namespace MiniScreenRecorder
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;

    static class ScreenRecorder
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
              Application.ThreadException += delegate (object sender, System.Threading.ThreadExceptionEventArgs e)
              {
                  ErrorLog(e.Exception, "ThreadException");
              };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MiniScreenRecorder());
        }

        public static void ErrorLog(Exception ex, string Method)
        {
            Thread th = new Thread(delegate ()
            {
                DateTime time = DateTime.Now;
                string ErrorMsg = "";
                ErrorMsg += "<Row>" + Environment.NewLine;
                ErrorMsg += "<DateTime>" + time.ToString() + "</DateTime>" + Environment.NewLine;
                ErrorMsg += "<Message>" + ex.Message + "</Message>" + Environment.NewLine;
                ErrorMsg += "<StackTrace>" + ex.StackTrace + "</StackTrace>" + Environment.NewLine;
                ErrorMsg += "<Method>" + Method + "</Method>" + Environment.NewLine;
                ErrorMsg += "</Row>" + Environment.NewLine;
                string path = AppDomain.CurrentDomain.BaseDirectory + "ErrorLog" + time.ToString("yyyyMMdd") + ".txt";
                File.AppendAllText(path, ErrorMsg, System.Text.Encoding.UTF8);
            });
            th.Start();
        }
    }
}
