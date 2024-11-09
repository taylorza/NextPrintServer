namespace NextPrintServer
{
    internal static class Program
    {
        static readonly Mutex _mutex = new(true, "{72E0739D-9639-4023-8381-CF1C2D16FA04}");

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (_mutex.WaitOne(TimeSpan.Zero, true))
            {
                try
                {
                    // To customize application configuration such as set high DPI settings or default font,
                    // see https://aka.ms/applicationconfiguration.
                    ApplicationConfiguration.Initialize();
                    Application.Run(new MainForm());
                }
                finally
                {
                    _mutex.ReleaseMutex();
                }
            }
            else
            {
                MessageBox.Show(Resources.errServerAlreadyRunning, Resources.strTitle, MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
    }
}