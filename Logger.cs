using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace FileLog
{
    public sealed class Logger : IDisposable
    {
        public Logger(string filename)
        {
            Init(filename, @"Logs\", true, true);
        }

        public Logger(string filename, bool? usedate)
        {
            Init(filename, @"Logs\", usedate, true);
        }

        public Logger(string filename, string directory)
        {
            Init(filename, directory, true, true);
        }

        public Logger(string filename, string directory, bool? usedate, bool? showLinePrefix)
        {
            Init(filename, directory, usedate, showLinePrefix);
        }

        private void Init(string filename, string directory, bool? usedate, bool? showLinePrefix)
        {
            _logQueue = new BlockingQueue<string>();

            LogBaseName = filename;

            LogDirectory = directory;

            if (LogDirectory[LogDirectory.Length - 1] != '\\')
            {
                LogDirectory += @"\";
            }

            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            if (usedate.HasValue)
            {
                _usedate = usedate.Value;
            }

            if (showLinePrefix.HasValue)
            {
                _usePrefix = showLinePrefix.Value;
            }
        }

        public void Close()
        {
            CloseWriter();
        }

        public void Log(string logline)
        {
            if (_usePrefix)
            {
                DateTime logTime = DateTime.Now;
                WriteLine(string.Format("{0:G}:\t{1}", logTime, logline));
            }
            {
                WriteLine(logline);
            }
        }

        public void QueueLog(string logline)
        {
            if (_usePrefix)
            {
                DateTime logTime = DateTime.Now;
                _logQueue.Enqueue(string.Format("Queue {0:G}:\t{1}", logTime, logline));
            }
            else
            {
                _logQueue.Enqueue(logline);
            }
            ProcessQueue();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void WriteLine(string line)
        {
            DateTime logTime = DateTime.Now;

            if (_usedate)
            {
                if (logTime.DayOfYear != CurrentStreamDay.DayOfYear || !IsOpen)
                {
                    CloseWriter();
                    CreateNewWriter(null);
                }
            }
            else if (!IsOpen)
            {
                CreateNewWriter(null);
            }

            if (IsOpen)
            {
                _filewriter.WriteLine(line);
            }
        }

        public void Log(Exception ex)
        {
            Log(ExceptionLogLine(ex));

            if (ex.InnerException != null)
            {
                Log("Inner Exception", ex.InnerException);
            }
        }

        public void Log(string line, Exception ex)
        {
            Log(line + " " + ExceptionLogLine(ex));

            if (ex.InnerException != null)
            {
                Log("Inner Exception", ex.InnerException);
            }
        }

        public void QueueLog(Exception ex)
        {
            QueueLog(ExceptionLogLine(ex));

            if (ex.InnerException != null)
            {
                QueueLog("Inner Exception", ex.InnerException);
            }
        }

        public void QueueLog(string line, Exception ex)
        {
            QueueLog(line + " " + ExceptionLogLine(ex));

            if (ex.InnerException != null)
            {
                QueueLog("Inner Exception", ex.InnerException);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ProcessQueue()
        {
            if (_processQueueThread == null)
            {
                _processThreadExited = new ManualResetEvent(false);
                _processQueueThread = new Thread(
                    () =>
                    {
                        try
                        {
                            while (!_disposed)
                            {
                                WriteLine(_logQueue.Dequeue());
                            }
                        }
                        catch (ThreadAbortException)
                        {
                            _processQueueThread = null;
                        }
                        finally
                        {
                            _processThreadExited.Set();
                        }
                    });
                _processQueueThread.IsBackground = true;
                _processQueueThread.Name = string.Format("{0} QueueLogger thread", this.LogBaseName);
            }

            if (_processQueueThread.IsAlive)
                return;

            _processQueueThread.Start();
        }

        private void CreateNewWriter(int? filenum)
        {
            if (_usedate)
            {
                DeleteOldFiles();
                CurrentStreamDay = DateTime.Now;
                if (filenum.HasValue)
                {
                    LogFileName = string.Format("{0}{1}({2})_{3:yyyy-M-d}.txt", LogDirectory, LogBaseName, filenum,
                                                CurrentStreamDay);
                }
                else
                {
                    LogFileName = string.Format("{0}{1}_{2:yyyy-M-d}.txt", LogDirectory, LogBaseName, CurrentStreamDay);
                    filenum = 0; //just in case we can't open this file
                }
            }
            else
            {
                if (filenum.HasValue)
                {
                    LogFileName = string.Format("{0}{1}({2}).txt", LogDirectory, LogBaseName, filenum);
                }
                else
                {
                    LogFileName = string.Format("{0}{1}.txt", LogDirectory, LogBaseName);
                    filenum = 0; //just in case we can't open this file
                }
            }

            try
            {
                //use date parameter will determine if we should append or start new
                _filewriter = new StreamWriter(LogFileName, _usedate, Encoding.UTF8);
                _filewriter.AutoFlush = true;
            }
            catch (UnauthorizedAccessException)
            {
                CreateNewWriter(++filenum);
            }
            catch (IOException)
            {
                CreateNewWriter(++filenum);
            }
            catch (Exception)
            {
                _filewriter = null;
            }
        }

        private void DeleteOldFiles()
        {
            var deleteBefore = DateTime.Today.AddDays(-7);

            var files = Directory.GetFiles(LogDirectory).Where(file => file.Contains(LogBaseName)).ToList();
            foreach (var file in files)
            {
                var f = new FileInfo(file);
                if (f.LastWriteTime < deleteBefore)
                {
                    f.Delete();
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void CloseWriter()
        {
            if (_filewriter != null && IsOpen)
            {
                _filewriter.WriteLine("\r\n\r\n"); //add some blank lines to the end

                _filewriter.Close();
                _filewriter = null;
            }
        }

        private bool _disposed = false;
        public void Dispose()
        {
            _disposed = true;
            if (_processQueueThread != null && _processQueueThread.IsAlive)
            {
                _logQueue.Clear();
                _logQueue.Enqueue("<end queue>"); //one last message to exit the process thread
                //_processQueueThread.Abort();
                //_processQueueThread = null;

                if (_processThreadExited.WaitOne(TimeSpan.FromSeconds(1)))
                {
                    //thread finished running
                    _processQueueThread.Join();
                }
                else
                {
                    //thread failed to exit
                    _processQueueThread.Abort();
                }
                _processQueueThread = null;
            }

            CloseWriter();

            GC.SuppressFinalize(this);
        }

        public bool IsOpen
        {
            get { return _filewriter != null; }
        }

        private string ExceptionLogLine(Exception ex)
        {
            return
                string.Format(
                    "Exception:\t{0}\r\n\tException Message:\t{1}\r\n\tSource:\t{2}\r\n\tStack:\t{3}\r\n",
                    ex.GetType(), ex.Message, ex.Source, ex.StackTrace);
        }

        private StreamWriter _filewriter;
        private bool _usedate;
        private bool _usePrefix;
        private BlockingQueue<string> _logQueue;
        private Thread _processQueueThread;
        private ManualResetEvent _processThreadExited;

        private string LogFileName { get; set; }

        private string LogBaseName { get; set; }

        private DateTime CurrentStreamDay { get; set; }

        private string LogDirectory { get; set; }
    }
}
