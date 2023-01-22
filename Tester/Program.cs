



using System.Reflection;
using System.Threading.Tasks;

namespace Tester
{
    internal class Program
    {
        // create logger with default file name and location (\{your_app_path}\Logs\{your_app_name}.txt). Create just one log file always overwriting last one
        static TottiWatti.FileLogger FileLogger = new TottiWatti.FileLogger(Path: null,  KeepDays: 0);       

        static void Main(string[] args)
        {
            var StopWatch = new System.Diagnostics.Stopwatch();           

            Console.WriteLine($"File logger tester to default file path {FileLogger.FilePath} starts");

            StopWatch.Start();

            // write test entries to log file
            FileLogger.Debug("Test debug message");
            FileLogger.Fatal("Test fatal message");
            FileLogger.Information("Test information message");
            FileLogger.Warning("Test warning message");
            Program.GenerateException();    // Error test entry with exception

            Console.WriteLine($"Simple test entries with one exception queued to FileLogger in {StopWatch.ElapsedMilliseconds} ms.");

            // dispose file logger when your application exits, if forces remaining entries to be written to log
            FileLogger.Dispose();

            Console.WriteLine($"Simple test entries written to log text file in {StopWatch.ElapsedMilliseconds} ms.");

            Console.WriteLine();

            // create new logger with another file name for bigger test
            FileLogger = new TottiWatti.FileLogger(Path: "Logs\\SingleThreadTest.txt", KeepDays: 0);

            Console.WriteLine($"File logger single thread large log file tester to defined path {FileLogger.FilePath} starts");

            StopWatch.Restart();

            // write multiple entries to log but no exceptions, raising them takes most of time spent
                 
            for (int k = 0; k < 100000; k++)
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                FileLogger.Information($"Message {k} from thread {threadId}: Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.");
            }

            Console.WriteLine($"100000 test entries queued to FileLogger in {StopWatch.ElapsedMilliseconds} ms.");

            // dispose logger and force remaining entries to be written to log
            FileLogger.Dispose();

            Console.WriteLine($"100000 test entries written to log text file in {StopWatch.ElapsedMilliseconds} ms.");

            Console.WriteLine();            

            // create new logger with another file name for bigger test
            FileLogger = new TottiWatti.FileLogger(Path: "Logs\\MultiThreadTest.txt", KeepDays: 0);

            Console.WriteLine($"File logger multi thread large log file tester to defined path {FileLogger.FilePath} starts");

            // multithread action
            Action<object> action = (object obj) =>
            {
                for (int k = 0; k < 10000; k++)
                {
                    int threadId = Thread.CurrentThread.ManagedThreadId;
                    FileLogger.Information($"Message {k} from thread {threadId}: Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.");
                }
            };            

            StopWatch.Start();

            List<Task> tasks = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                Task task = new Task(action, "");
                task.Start();
                tasks.Add(task);               
            }

            Task t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception ex)
            {
                Exception eex = ex;
            }

            Console.WriteLine($"100000 test entries queued to FileLogger in {StopWatch.ElapsedMilliseconds} ms.");

            FileLogger.Dispose();

            Console.WriteLine($"100000 multithread test entries written to log text file in {StopWatch.ElapsedMilliseconds} ms.");

            Console.ReadKey();
        }

        static void GenerateException()
        {
            try
            {
                int i = 0;
                int j = 1 / i;
            }
            catch (Exception ex)
            {                
                FileLogger.Error("Program.GenerateException() catched an exception", ex);
            }
        }

    }
}
