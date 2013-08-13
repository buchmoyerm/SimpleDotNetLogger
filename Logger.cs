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
            Init(filename, @"Logs\" , true);
        }

        public Logger(string filename, bool? usedate)
        {
            Init(filename, @"Logs\", usedate);
        }

        public Logger(string filename, string directory)
        {
            Init(filename, directory, true);
        }

        public Logger(string filename, string directory, bool? usedate)
        {
            Init(filename, directory, usedate);
        }

        private void Init(string filename, string directory, bool? usedate)
        {
            LogBaseName = filename;

            LogDirectory = directory;

            if (LogDirectory[LogDirectory.Length - 1] != '\\')
            {
                LogDirectory += @"\";
            }

            if (usedate.HasValue)
            {
                _usedate = usedate.Value;
            }
        }

        public void Close()
        {
            CloseWriter();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Log(string logline)
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

            if ( IsOpen )
            {
                string line = string.Format( "{0:G}:\t{1}", logTime, logline );
                _filewriter.WriteLine( line );
                _filewriter.Flush(); //flush the writer before the next write
            }
        }

        public void Log( Exception ex )
        {
            Log(ExceptionLogLine(ex));
        }

        public void Log( string line, Exception ex )
        {
            Log(line + " " + ExceptionLogLine(ex));
        }

        private void CreateNewWriter(int? filenum)
        {
            if (_usedate)
            {
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

        [MethodImpl( MethodImplOptions.Synchronized )]
        private void CloseWriter()
        {
            if (_filewriter != null && IsOpen)
            {
                _filewriter.WriteLine( "\r\n\r\n" ); //add some blank lines to the end
                _filewriter.Flush(); //flush the writer before the next write

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

        private string ExceptionLogLine(Exception ex)
        {
            return
                string.Format(
                    "Exception:\t{0}\r\n\tException Message:\t{1}\r\n\tSource:\t{2}\r\n\tStack:\t{3}\r\n",
                    ex.GetType(), ex.Message, ex.Source, ex.StackTrace);
        }

        private StreamWriter _filewriter;
        private bool _usedate;
        private string LogFileName { get; set; }
        private string LogBaseName { get; set; }
        private DateTime CurrentStreamDay { get; set; }
        private string LogDirectory { get; set; }
    }
}
