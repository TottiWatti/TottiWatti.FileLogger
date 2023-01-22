# FileLogger
Simple single file text logger that basically does more or less the same as Serilog + Serilog.Sinks.Async + Serilog.Sinks.RollingFile

## Features

* Writing a log to file is done in background task so it does not block your app
* Produces either a single log file or file per day for n days automatically deleting older ones
* Has log categories 'Information', 'Debug', 'Fatal', 'Verbose', 'Warning' and 'Error'. For 'Error' category you can provide exception with string to log. Full exception details will be written to log

### Installation

Add TottiWatti.FileLogger package reference from nuget or just copy file TW.FileLogger.cs to your C# application

## Usage

Creating logger instance 
```C#
// Create Logger instance which stores logs to your applications running directory one log file for each day with name {your-application-name}_{yyyy-MM-dd}.txt retaining default last 31 days logs and automatically deleting older
static FileLogger = new TottiWatti.FileLogger();

// Create Logger instance which stores logs to your applications running directory one log file for each day with name {your-application-name}_{yyyy-MM-dd}.txt retaining last 10 days logs and automatically deleting older
static FileLogger = new TottiWatti.FileLogger(Path: null,  KeepDays: 10);

// Create Logger instance which stores logs to your applications running directory's subdirectory 'Logs' one log file for each day with name MyLog_{yyyy-MM-dd}.txt retaining last 10 days logs and automatically deleting older
static FileLogger = new TottiWatti.FileLogger(Path: "Logs\\Mylog.txt",  KeepDays: 10);

// Create Logger instance which stores a log to your applications running directory one log only with name {your-application-name}.txt replacing old log with same name each time your application is run
static FileLogger = new TottiWatti.FileLogger(Path: null,  KeepDays: 0);

// Create Logger instance which stores a log to your applications running directory's subdirectory 'Logs' one log only with name Mylog.txt replacing old log with same name each time your application is run
static FileLogger = new TottiWatti.FileLogger(Path: "Logs\\Mylog.txt",  KeepDays: 0);
```

Using logger instance 
```C#
// store log message with information category to your log file
FileLogger.Information("A log message");

// store log message with error category and exception to your log file
try
{
    int i = 0;
    int j = 1 / i;
}
catch (Exception ex)
{                
    FileLogger.Error("Oh no, I divided with zero", ex);
}
```

Disposing logger instance 
```C#
// Always remember to dispose FileLogger instance when not needed or when exiting application. Dispose will force remaining log entries to be written from ram buffer to text file before releasing resources
FileLogger.Dispose();
```

## Testing

Download full source code and run it. A test program is ran with test for a small log file, a large single thread log file test and a large multi-thread log file test. If you want to inspect results of large log files please use Notepad++ or similar, file will be very slow to open with default Notepad.



