using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp7
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
        public struct FTPparams
        {
            //клиент
        public static FtpClient client = new FtpClient();
        public static int TimeoutFTP = 5000; //Таймаут.
        public static string FTPFullPath = @"/";
        public static string Path = "";
        public static string FTPSERVER;
        public static string FTPPORT;
        public static string FTP_PASSWORD;
        public static string FTP_USER;
    }

    public struct LocalPath
    {
        public static string FullPath = "";
        public static string Path = "";
        public static int Size;
    }
}
}
