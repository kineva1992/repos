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

using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using BytesRoad.Diag;
using BytesRoad.Net.Ftp;
using BytesRoad.Net.Ftp.Advanced;


namespace BytesRoad.Net.Ftp.Commands
{
	/// <summary>
	/// Summary description for Cmd_GetDataConnection.
	/// </summary>
	internal class Cmd_GetDataConnection : AsyncBase
	{
		#region AsyncResult class
		class GetDC_SO : AsyncResultBase
		{
			FtpDataConnection _dc = null;
			string _cmd = null;
			int _timeout = -1;

			internal GetDC_SO(int timeout, AsyncCallback cb, object state) : base(cb, state)
			{
				_timeout = timeout;
			}

			internal int Timeout
			{
				get { return _timeout; }
			}

			internal FtpDataConnection DC
			{
				get { return _dc; }
				set { _dc = value; }
			}

			internal string Cmd
			{
				get { return _cmd; }
				set { _cmd = value; }
			}
		}

		#endregion

		bool _passiveMode = false;
		FtpControlConnection _cc = null;
		Encoding _encoding = null;
		FtpClient _client = null;
		Random _rand = new Random(unchecked((int)DateTime.Now.Ticks)); 

		internal Cmd_GetDataConnection(FtpClient ftp)
		{
			_cc = ftp.ControlConnection;
			_passiveMode = ftp.PassiveMode;
			_encoding = ftp.UsedEncoding;
			_client = ftp;
		}

		internal Type ARType
		{
			get { return typeof(GetDC_SO); }
		}

		#region Helpers
		IPEndPoint GetPasvEndPoint(FtpResponse response)
		{
			string respStr = _encoding.GetString(response.RawBytes);
			Regex re = new Regex(@"^227.*[^\d](?<a1>\d{1,3}) *, *(?<a2>\d{1,3}) *, *(?<a3>\d{1,3}) *, *(?<a4>\d{1,3}) *, *(?<p1>\d{1,3}) *, *(?<p2>\d{1,3}).*");
			Match m = re.Match(respStr);

			if(7 != m.Groups.Count)
				throw new Exception(); // some error in reply

				
			string ip = m.Groups["a1"] + "." + m.Groups["a2"] + "." + 
				m.Groups["a3"] + "." + m.Groups["a4"];
			IPAddress address = IPAddress.Parse(ip);

			int port = (int.Parse(m.Groups["p1"].Value)<<8)|int.Parse(m.Groups["p2"].Value);

			return new IPEndPoint(address, port);
		}

		IPAddress GetRandomLocalIP()
		{
			IPHostEntry host = _client.LocalHostEntry;
			int addressNo = 0;
			if(host.AddressList.Length > 1)
				addressNo = _rand.Next(host.AddressList.Length - 1);
			
			return host.AddressList[addressNo];
		}

		IPEndPoint GetLocalEndPoint()
		{
			if(IPAddress.Any.Equals(_client.LocalIP))
				return new IPEndPoint(GetRandomLocalIP(), 0);
			return new IPEndPoint(_client.LocalIP, 0);
		}

		string GetPortCmd(IPEndPoint ep)
		{
			byte[] addr = new Byte[4];
			long ipAddr = ep.Address.Address;
			addr[3] = (byte)((ipAddr&0xFF000000)>>24);
			addr[2] = (byte)((ipAddr&0x00FF0000)>>16);
			addr[1] = (byte)((ipAddr&0x0000FF00)>>8);
			addr[0] = (byte)((ipAddr&0x000000FF));

			byte p1 = (byte)((ep.Port&0xFF00)>>8);
			byte p2 = (byte)(ep.Port&0xFF);
			return string.Format("port {0},{1},{2},{3},{4},{5}", addr[0], addr[1], addr[2], addr[3], p1, p2);
		}
		#endregion

		internal FtpDataConnection Execute(int timeout)
		{
			if(_passiveMode)
			{
				FtpResponse response = _cc.SendCommandEx(timeout, "PASV");
				if(false == response.IsCompletionReply)
				{
					NSTrace.WriteLineError("PASV: " + response.ToString());
					throw new FtpErrorException("Error while configuring data connection.", response);
				}

				IPEndPoint ep = GetPasvEndPoint(response);
				return new FtpDataConnectionOutbound(_client, ep);
			}
			else
			{
				IPEndPoint ep = GetLocalEndPoint();
				FtpDataConnectionInbound dconn = new FtpDataConnectionInbound(_client, ep);
				
				//prepare data connection
				dconn.Prepare(timeout, _cc.UsedSocket);
				
				//get the inbound data connection attributes
				string cmd = GetPortCmd(dconn.LocalEndPoint);

				//send tranfser parameters to the server
				FtpResponse response = _cc.SendCommandEx(timeout, cmd);
				if(false == response.IsCompletionReply)
				{
					dconn.Dispose();
					NSTrace.WriteLineError(cmd + ": " + response.ToString());
					throw new FtpErrorException("Error while configuring data connection.", response);
				}
				return dconn;
			}
		}

		internal IAsyncResult BeginExecute(int timeout, AsyncCallback cb, object state)
		{
			SetProgress(true);
			GetDC_SO stateObj = null;
			try
			{
				stateObj = new GetDC_SO(timeout, cb, state);
				if(_passiveMode)
				{
					_cc.BeginSendCommandEx(timeout,
						"PASV",
						new AsyncCallback(this.GetDC_EndCmd),
						stateObj);
				}
				else
				{
					IPEndPoint ep = GetLocalEndPoint();
					stateObj.DC = new FtpDataConnectionInbound(_client, ep);
					stateObj.DC.BeginPrepare(timeout, 
						_cc.UsedSocket, 
						new AsyncCallback(Prepare_End),
						stateObj);
				}
			}
			finally
			{
				SetProgress(false);
			}
			return stateObj;
		}

		void Prepare_End(IAsyncResult ar)
		{
			GetDC_SO stateObj = (GetDC_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				stateObj.DC.EndPreapre(ar);

				stateObj.Cmd = GetPortCmd(stateObj.DC.LocalEndPoint);

				_cc.BeginSendCommandEx(stateObj.Timeout,
					stateObj.Cmd,
					new AsyncCallback(this.GetDC_EndCmd),
					stateObj);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void GetDC_EndCmd(IAsyncResult ar)
		{
			GetDC_SO stateObj = (GetDC_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				FtpResponse response = _cc.EndSendCommandEx(ar);
				if(_passiveMode)
				{
					if(false == response.IsCompletionReply)
					{
						NSTrace.WriteLineError("PASV: " + response.ToString());
						stateObj.Exception = new FtpErrorException("Error while configuring data connection.", response);
					}
					else
					{
						IPEndPoint ep = GetPasvEndPoint(response);
						stateObj.DC = new FtpDataConnectionOutbound(_client, ep);
					}
				}
				else
				{
					if(false == response.IsCompletionReply)
					{
						stateObj.DC.Dispose();
						stateObj.DC = null;
						NSTrace.WriteLineError(stateObj.Cmd + ": " + response.ToString());
						stateObj.Exception = new FtpErrorException("Error while configure data connection.", response);
					}
				}
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			stateObj.SetCompleted();
		}

		internal FtpDataConnection EndExecute(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(GetDC_SO));
			HandleAsyncEnd(ar, true);
			GetDC_SO stateObj = (GetDC_SO)ar;
			return stateObj.DC;
		}
	}
}
