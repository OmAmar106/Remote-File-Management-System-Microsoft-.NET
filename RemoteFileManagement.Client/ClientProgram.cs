using System;
using System.Windows.Forms;

namespace RemoteFileManagement.Client
{
    static class ClientProgram
    {
        /// <summary>
        /// The main entry point for the Remote File Management client application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
