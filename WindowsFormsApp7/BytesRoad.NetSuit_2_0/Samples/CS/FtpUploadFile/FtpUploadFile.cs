// =============================================================
// BytesRoad.NetSuit : A free network library for .NET platform 
// =============================================================
//
// Copyright (C) 2004-2005 BytesRoad Software
// 
// Project Info: http://www.bytesroad.com/NetSuit/
// 
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//========================================================================== 
//  FTP Sample
//
//  File:		FtpUploadFile.cs
//  Summary:	Demonstrates how to upload file to FTP server
//				by using BytesRoad.NetSuit Library.
//
//========================================================================== 

using System;
using System.Diagnostics;
using System.IO;

using BytesRoad.Diag;
using BytesRoad.Net.Ftp;


namespace FtpUploadFile
{
	class Class1
	{
		static void ShowGreeting()
		{
			Console.WriteLine("(C) Copyright 2004-2005 BytesRoad Software. All rights reserved.");
			Console.WriteLine("-----------------------------------------------------------");
			Console.WriteLine("Description:");
			Console.WriteLine("     FtpUploadFile sample upload specified file to the");
			Console.WriteLine("     FTP server.");
			Console.WriteLine("-----------------------------------------------------------");
		}


		static void Main(string[] args)
		{
			ShowGreeting();

			FtpClient ftp = null;

			// setup tracing
			try
			{
				SetupTraceListener();
			}
			catch(Exception e)
			{
				Console.WriteLine("Warning: Unable to setup tracing ({0}).", e.Message);
			}
		
			//set timeout to 10 seconds
			int timeout = 10000; 
			try
			{
				//ask for the server name, localhost is default
				Console.Write("Server name (press ENTER for 'localhost'): ");
				string server = Console.ReadLine();
				if((null == server) || (0 == server.Length))
					server = "localhost";

				//ask for the port, 21 is default
				int port = 21;
				Console.Write("Server port (press ENTER for 21): ");
				string strPort = Console.ReadLine();
				if((null != strPort) && (0 != strPort.Length))
					port = Convert.ToInt32(strPort, 10);

				// get default source file path
				string srcFilePath = ConstructFileName("test.txt");

				//ask for the source file path
				Console.Write("Source file path (press ENTER for '{0}'): ", srcFilePath);
				string path = Console.ReadLine();
				if((null != path) && (0 != path.Length))
					srcFilePath = path;

				string dstFilePath = DefDestFilePath(srcFilePath);

				//ask for the destination file path
				Console.Write("Destination file path (press ENTER for '{0}'): ", dstFilePath);
				path = Console.ReadLine();
				if((null != path) && (0 != path.Length))
					dstFilePath = path;

				//create an instance
				ftp = new FtpClient();

				//connect to the ftp server
				FtpResponse res = ftp.Connect(timeout, server, port);
				Console.WriteLine(res.RawString + Environment.NewLine);

				//login 
				ftp.Login(timeout, "anonymous", "a@a.com");

				//upload the file
				ftp.PutFile(timeout, dstFilePath, srcFilePath);

				Console.WriteLine("Uploading is completed.");

				//disconnect from the FTP server
				ftp.Disconnect(timeout);
			}
			catch(FtpErrorException e)
			{
				//non fatal error occurs...
				Console.WriteLine("ERROR: {0} ({1}).", 
					e.Message, 
					e.Response.RawString);

				//let's close the connection gracefully
				if(ftp.IsConnected)
					ftp.Disconnect(timeout);
			}
			catch(Exception e)
			{
				Console.WriteLine("FATAL ERROR: {0}", e.Message);
			}

			//release all resources
			if(null != ftp)
				ftp.Dispose();

			Console.WriteLine("Press ENTER to exit...");
			Console.ReadLine();
		}


		// Setup tracing options - redirect all tracing
		// into the 'FtpTrace_uCS.txt' file located
		// in the same directory as UploadFileCS.exe
		static void SetupTraceListener()
		{
			string traceFileName = ConstructFileName("NSTrace_uCS.txt");
			NSTraceOptions.Level = TraceLevel.Error;
			TextWriterTraceListener listener = new TextWriterTraceListener(traceFileName);
			NSTraceOptions.Listeners.Add(listener);
			NSTraceOptions.AutoFlush = true;
		}

		static string ConstructFileName(string fileName)
		{
			string ret = @".\"  + fileName;
			try
			{
				Process oLocal = Process.GetCurrentProcess();
				ProcessModule oMain = oLocal.MainModule;
				string curDir = Path.GetDirectoryName(oMain.FileName);
				if(!curDir.EndsWith("\\") && !curDir.EndsWith("/"))
					curDir += "\\";

				ret = curDir + fileName;
			}
			catch(Exception e)
			{
				Console.WriteLine("Warning: Can't construct the file path: {0}", e.ToString());
			}
			return ret;
		}

		static string DefDestFilePath(string srcFilePath)
		{
			string fileName = null;
			int idx = srcFilePath.LastIndexOfAny(new char[]{'\\', '/'});
			if(idx == -1)
				fileName = srcFilePath;
			else
				fileName = srcFilePath.Substring(idx+1);

			return "/" + fileName;
		}
	}
}
