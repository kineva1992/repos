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
//  File:		FtpDirectoryList.cs
//  Summary:	Demonstrates how to retrieve FTP's directory 
//				content	by using BytesRoad.NetSuit Library.
//========================================================================== 

using System;
using System.Diagnostics;
using System.IO;

using BytesRoad.Net.Ftp;
using BytesRoad.Diag;

namespace FtpDirectoryList
{
	class Class1
	{
		// output greetings to the console
		static void ShowGreeting()
		{
			Console.WriteLine("(C) Copyright 2004-2005 BytesRoad Software. All rights reserved.");
			Console.WriteLine("-----------------------------------------------------------");
			Console.WriteLine("Description:");
			Console.WriteLine("     FtpDirectoryList sample read the content of the default");
			Console.WriteLine("     directory at the ftp.microsoft.com FTP server.");
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
				string server = "ftp.microsoft.com";
				ftp = new FtpClient();
				
				//connect to the ftp server
				Console.Write("Connecting to '{0}'... ", server);
				FtpResponse res = ftp.Connect(timeout, server, 21);
				Console.WriteLine("done");
				Console.WriteLine(res.RawString + Environment.NewLine);

				//login 
				ftp.Login(timeout, "anonymous", "a@a.com");

				//switch between MS-DOS and Unix directory
				//listing styles
				Console.WriteLine("Send 'SITE DIRSTYLE'");
				res = ftp.SendCommand(timeout, "SITE DIRSTYLE");
				Console.WriteLine(res.RawString + Environment.NewLine);

				//get default directory content
				FtpItem[] items = ftp.GetDirectoryList(timeout, null);
				Console.WriteLine("Default directory for '{0}': {1}",
									server, Environment.NewLine);

				Console.WriteLine("    Type            Name                          Size     Time");
				Console.WriteLine("------------------------------------------------------------------------------");
				foreach(FtpItem item in items)
				{
					//If ftp item was succesfully resolved
					//then print the details; otherwise
					//print the raw string which is represent
					//ftp item.
					if(FtpItemType.Unresolved != item.ItemType)
					{
						Console.WriteLine("  {0,-10} {1,-25}     {2,10}     {3}", 
							item.ItemType.ToString(), 
							item.Name,
							item.Size,
							item.Date.ToString());
					}
					else
					{

						Console.WriteLine(item.RawString);
					}
				}
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

			Console.WriteLine("");
			Console.WriteLine("Press ENTER to exit...");
			Console.ReadLine();
		}

		// Setup tracing options - redirect all tracing
		// into the 'NSTrace_lCS.txt' file located
		// in the same directory as DirectoryListCS.exe
		static void SetupTraceListener()
		{
			string traceFileName = ConstructFileName("NSTrace_lCS.txt");
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
	}
}
