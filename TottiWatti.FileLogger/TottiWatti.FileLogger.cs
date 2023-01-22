using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection.Metadata.Ecma335;

namespace TottiWatti
{
    /// <summary>
    /// Simple single file logger that basically does more or less the same as Serilog + Serilog.Sinks.Async + Serilog.Sinks.RollingFile
    /// Each day will have own log file as long as KeepDays is set > 0. Default one month of logs are saved and older removed.
    /// Version: 1.00 22.1.2023
    /// License: MIT https://mit-license.org/
    /// Copyright (c) 2022 TottiWatti
    /// </summary>
    public class FileLogger : IDisposable
    {
        private string _FileName = "";       
        private string _Path = "";
        private string _FileExtension = "";
        private uint _KeepDays = 31;
        private ConcurrentQueue<FileLogMessage> _Entries = new ConcurrentQueue<FileLogMessage>();
        private CancellationTokenSource? _PersistCancellationTokenSource = null;
        private FileStream? _FileStream = null;
        private object _Lock = new object();
        private bool _Disposing = false;
        private Assembly? _Assembly = Assembly.GetEntryAssembly();
        private string _BaseDirectory = "";
        private StringBuilder _StringBuilder = new StringBuilder();
        private string _LastFile = "";

        /// <summary>
        /// Empty initializer using default values
        /// </summary>
        public FileLogger()
        {
            _Initialize();
        }

        /// <summary>
        /// Initialization with target folder path, file name and number of daily logs to keep
        /// </summary>
        /// <param name="Path">Target file with path and extension. If null or empty "Logs\\{your_application_name}.txt" is used</param>        
        /// <param name="KeepDays">Number of calendar day log files to keep, older deleted automatically. Default value is 30. Special case 0 -> only one log file is created and overwritten if allready exists</param>
        public FileLogger(string? Path, uint KeepDays)
        {
            try
            { 
                if (!string.IsNullOrEmpty(Path)) _Path = Path;   
                _KeepDays = KeepDays;                
                _Initialize();
            }
            catch (Exception ex)
            {
                Exception e = ex;
            }
        }

        public string FilePath
        { 
            get { return _Path; }
        }

        /// <summary>
        /// Initialize local variables
        /// </summary>
        private void _Initialize()
        {
            try
            {  
                string curdir = Directory.GetCurrentDirectory();

                if (string.IsNullOrEmpty(_Path))
                {
                    if (_KeepDays == 0) {
                        _Path = $"{_Assembly?.FullName?.Split(',')[0]}.txt";
                    }
                    else
                    {
                        _Path = $"Logs\\{_Assembly?.FullName?.Split(',')[0]}.txt";
                    }                    
                }

                string filePath = Path.Combine(curdir, _Path); 
                
                string? bdir = Path.GetDirectoryName(filePath); ;
                if (!string.IsNullOrEmpty(bdir)) _BaseDirectory = bdir;
                              
                _FileName = Path.GetFileNameWithoutExtension(filePath);
                string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex($"[{System.Text.RegularExpressions.Regex.Escape(regexSearch)}]");
                _FileName = r.Replace(_FileName, "_");

                _FileExtension = Path.GetExtension(filePath);
                
                if (!Directory.Exists(_BaseDirectory))
                    Directory.CreateDirectory(_BaseDirectory);
                _StringBuilder = new StringBuilder();
            }
            catch (Exception ex)
            {
                Exception eex = ex;
            }
        }

        /// <summary>
        /// Log entries write to target file task
        /// </summary>
        /// <param name="token"></param>
        private void _PersistLogEntries(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                _PersistEntry();
            }
        }

        private void _PersistEntry()
        {
            try
            {
                FileLogMessage msg;

                // try read entry from queueu
                if (_Entries.TryPeek(out msg))
                {
                    string file = _BaseDirectory + @"\" + _FileName + (this._KeepDays > 0 ? "_" + DateTime.Now.ToString("yyyy-MM-dd") : "") + _FileExtension;

                    // first write or file changed?
                    if (string.IsNullOrEmpty(_LastFile) || file != _LastFile)
                    {
                        // if file not exists, create it and remove logfiles older than KeepDays
                        if (this._KeepDays > 0 && !System.IO.File.Exists(file))
                        {
                            string[] oldlogs = Directory.GetFiles(_BaseDirectory, _FileName + "*.txt");
                            uint days = this._KeepDays;
                            uint maxDays = Debugger.IsAttached ? 7u : 365u; // shorten log on debug
                            if (days > maxDays) { days = maxDays; }

                            foreach (string oldlog in oldlogs)
                            {
                                FileInfo fi = new FileInfo(oldlog);
                                if (DateTime.Now.Subtract(fi.LastWriteTime).TotalDays >= days)
                                    System.IO.File.Delete(fi.FullName);
                            }
                        }
                    }                    

                    // target file changed? Close old stream
                    if (_FileStream != null && _FileStream.Name != file)
                    {
                        _FileStream.Close();
                        _FileStream.Dispose();
                        _FileStream = null;
                    }

                    if (_FileStream == null)
                        _FileStream = new FileStream(file, this._KeepDays > 0 ? FileMode.Append : FileMode.Create);
                    byte[] b = Encoding.UTF8.GetBytes(msg.ToString());
                    _FileStream.Write(b, 0, b.Length);
                    _FileStream.Flush();

                    _LastFile = file;

                    // remove ontry from queue
                    _Entries.TryDequeue(out msg);
                }
            }
            catch (Exception ex)
            {
                Exception eex = ex;
            }
        }

        /// <summary>
        /// Adds file log entry to write queueu
        /// </summary>
        /// <param name="category">Log entry category</param>
        /// <param name="message">Log entry message</param>
        private void _Log(string category, string message)
        {
            try
            {
                FileLogMessage msg = new FileLogMessage(DateTime.Now, category, message);
                _Entries.Enqueue(msg);

                // now there's at least one entry to persist, start persist task if not already running
                if (_PersistCancellationTokenSource == null)
                {
                    _PersistCancellationTokenSource = new CancellationTokenSource();
                    Task t = Task.Factory.StartNew(() => _PersistLogEntries(_PersistCancellationTokenSource.Token), _PersistCancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
                }
            }
            catch (Exception ex)
            {
                Exception e = ex;
            }
        }

        /// <summary>
        /// Stops listening for new log entries, writes all existing to file before actual dispose
        /// </summary>
        public void Dispose()
        {
            _Disposing = true;

            // cancel task
            if (_PersistCancellationTokenSource != null)
            {
                _PersistCancellationTokenSource.Cancel(true);
                _PersistCancellationTokenSource.Dispose();
            }                

            // synchronously write remaining entries before disposal of logger
            while (_Entries.Count > 0)
            {
                _PersistEntry();
            }

            // dispose file stream
            if (_FileStream != null)
            {
                _FileStream.Close();
                _FileStream.Dispose();
                _FileStream = null;
            }               

        }

        /// <summary>
        /// Add information log entry
        /// </summary>
        /// <param name="source">Log message</param>
        public void Information(string source)
        {
            if (!_Disposing)
            {
                lock (_Lock)
                {
                    _Log("Information", source);
                }
            }
        }

        /// <summary>
        /// Add debug log entry
        /// </summary>
        /// <param name="source">Log message</param>
        public void Debug(string source)
        {
            if (!_Disposing)
            {
                lock (_Lock)
                {
                    _Log("Debug", source);
                }
            }
        }

        /// <summary>
        /// Add fatal log entry
        /// </summary>
        /// <param name="source">Log message</param>
        public void Fatal(string source)
        {
            if (!_Disposing)
            {
                lock (_Lock)
                {
                    _Log("Fatal", source);
                }
            }
        }

        /// <summary>
        /// Add verbose log entry
        /// </summary>
        /// <param name="source">Log message</param>
        public void Verbose(string source)
        {
            if (!_Disposing)
            {
                lock (_Lock)
                {
                    _Log("Verbose", source);
                }
            }
        }

        /// <summary>
        /// Add warning log entry
        /// </summary>
        /// <param name="source">Log message</param>
        public void Warning(string source)
        {
            if (!_Disposing)
            {
                lock (_Lock)
                {
                    _Log("Warning", source);
                }
            }
        }

        /// <summary>
        /// Add error log entry
        /// </summary>
        /// <param name="source">Log message</param>
        /// <param name="ex">Exception to log</param>
        public void Error(string source, Exception? ex = null)
        {
            if (!_Disposing)
            {
                lock (_Lock)
                {
                    Exception? _ex = ex;
                    _StringBuilder.Append($"{source}");                 

                    if (_ex != null)
                    {
                        string exceptionString = _ex.ToString();
                        _StringBuilder.Append($"\nException: {exceptionString}");
                        if (!string.IsNullOrEmpty(_ex.StackTrace) && !exceptionString.Contains(_ex.StackTrace))
                            _StringBuilder.Append($"\nException stack trace: {_ex.StackTrace}");

                        while (_ex.InnerException != null)
                        {
                            exceptionString = _ex.InnerException.ToString();
                            _StringBuilder.Append($"\nInner exception: {exceptionString}");
                            if (!string.IsNullOrEmpty(_ex.InnerException.StackTrace) && !exceptionString.Contains(_ex.InnerException.StackTrace))
                                _StringBuilder.Append($"\nInner exception stack trace: {_ex.InnerException.StackTrace}");
                            _ex = _ex.InnerException;
                        }
                    }
                    _Log("Error", _StringBuilder.ToString());
                    _StringBuilder.Clear();
                }
            }
        }
        
        /// <summary>
        /// Log entry model
        /// </summary>
        private struct FileLogMessage
        {
            public FileLogMessage()
            {
            }

            public FileLogMessage(DateTime date, string Category, string Message)
            {
                this.Date = date;
                this.Category = Category;
                this.Message = Message;
            }

            public DateTime Date = DateTime.MinValue;

            public string Category = "";

            public string Message = "";

            public override string ToString()
            {
                return $"{Date.ToString("yyyy-MM-dd HH\\:mm\\:ss\\.fff")} [{Category}] {Message}\n";
            }

        }

    }


}




