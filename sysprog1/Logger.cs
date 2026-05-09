namespace sysprog1
{
    public class Logger
    {
        private static readonly object _logLock = new();
        public static void Log(string message)
        {
            lock (_logLock)
            {
                Console.WriteLine(message);
            }
        }
    }
}
