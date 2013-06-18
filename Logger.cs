using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace FileLog
{
    public class Logger : IDisposable
    {
        public Logger(string filename)
        {
            LogBaseName = filename;
            LogDirectory = @"Logs\";
        }

        public Logger(string filename, string directory)
        {
            LogBaseName = filename;

            LogDirectory = directory;

            if (LogDirectory[LogDirectory.Length - 1] != '\\')
            {
                LogDirectory += @"\";
            }
        }

        private void CreateNewWriter(int? filenum)
        {
            CurrentStreamDay = DateTime.Now;
            if (filenum.HasValue)
            {
                LogFileName = string.Format("{0}{1}({2})_{3:yyyy-M-d}.txt", LogDirectory, LogBaseName, filenum,
                                            CurrentStreamDay);
            }
            else
            {
                LogFileName = string.Format( "{0}{1}_{2:yyyy-M-d}.txt", LogDirectory, LogBaseName, CurrentStreamDay );
                filenum = 0; //just in case we can't open this file
            }
            

            if ( !Directory.Exists( LogDirectory ) )
            {
                Directory.CreateDirectory( LogDirectory );
            }

            try
            {
                _filewriter = new StreamWriter(LogFileName, true, Encoding.UTF8);
            }
            catch (UnauthorizedAccessException)
            {
                CreateNewWriter(++filenum);
            }
            catch (IOException)
            {
                CreateNewWriter( ++filenum );
            }
            catch (Exception)
            {
                _filewriter = null;
            }
            
        }

        private void CloseWriter()
        {
            if (_filewriter != null)
            {
                _filewriter.Close();
                _filewriter = null;
            }
        }

        public void Dispose()
        {
            CloseWriter();
        }

        public bool IsOpen
        {
            get { return _filewriter != null; }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Log(string logline)
        {
            DateTime logTime = DateTime.Now;

            if (logTime.DayOfYear != CurrentStreamDay.DayOfYear || !IsOpen)
            {
                CloseWriter();
                CreateNewWriter(null);
            }

            if (IsOpen)
            {
                string line = string.Format("{0:G}:\t{1}", logTime, logline);
                _filewriter.WriteLine(line);
                _filewriter.Flush(); //flush the writer before the next write
            }
        }

        private StreamWriter _filewriter;
        private string LogFileName { get; set; }
        private string LogBaseName { get; set; }
        private DateTime CurrentStreamDay { get; set; }
        private string LogDirectory { get; set; }
    }
}
