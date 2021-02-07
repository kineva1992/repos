using System.IO;
using System.Threading;
using BytesRoad.Net.Ftp;

namespace BytesRoad.NetSuit_2_0.Tests
{
  class Program
  {
    static void Main(string[] args)
    {
      string file = @"C:\Temp\tmp.txt";
      //File.WriteAllText(file, "Hello Wold");
      UploadFile(Timeout.Infinite, "coad.net", "coadnet0000", "kleenex", "/coadnet0000/temp", file);
    }

    private static void UploadFile(int Timeout, string FtpServer, string Username, string Password, string RemotePath, string LocalFile)
    {
      // get instance of the FtpClient
      FtpClient client = new FtpClient();

      // connect to the specified FTP server
      client.PassiveMode = true;
      client.Connect(Timeout, FtpServer, 21);
      client.Login(Timeout, Username, Password);

      // build the target file path
      string target = Path.Combine(RemotePath, Path.GetFileName(LocalFile)).Replace("\\", "/");
      
      // synchronously upload the file
      client.PutFile(Timeout, target, LocalFile);
    }

    private static byte[] DownloadFile(int timeout, string ftpServer, string filePath)
    {
      //get instance of the FtpClient
      FtpClient client = new FtpClient();

      //connect to the specified FTP server
      client.Connect(timeout, ftpServer, 21);

      //synchronously download the file
      return client.GetFile(timeout, filePath);
    }
  }
}
