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
using System.Collections;
using System.Threading;

using BytesRoad.Diag;
using BytesRoad.Net.Ftp;
using BytesRoad.Net.Ftp.Advanced;

namespace BytesRoad.Net.Ftp.Commands
{
	/// <summary>
	/// Summary description for Cmd_Quit.
	/// </summary>
	internal class Cmd_Quit : AsyncBase
	{
		#region AsyncResult class
		class Quit_SO : AsyncResultBase
		{
			int _timeout = 0;
			bool _ccLocked = false;

			internal Quit_SO(int timeout,
				AsyncCallback cb, 
				object state) : base(cb, state)
			{
				_timeout = timeout;
			}

			internal int Timeout
			{
				get { return _timeout; }
			}

			internal bool CCLocked
			{
				get { return _ccLocked; }
				set { _ccLocked = value; }
			}
		}
		#endregion

		FtpClient _client = null;
		FtpControlConnection _cc = null;

		internal Cmd_Quit(FtpClient ftp)
		{
			_client = ftp;
			_cc = ftp.ControlConnection;
		}

		internal Type ARType
		{
			get { return typeof(Quit_SO); }
		}

		internal void Execute(int timeout)
		{
			bool ccLocked = false;
			try
			{
				ccLocked = _cc.Lock(Timeout.Infinite);
				if(null != _client.CurrentDTP)
				{
					//----------------------------------------
					//In case when we have active DTP we delegate
					//handling responses to it and here only send
					//quit command
					_cc.SendCommand(timeout, "QUIT");

					//----------------------------------------
					//Notify DTP that it should handle quit
					//issue
					_client.CurrentDTP.Quit();
				}
				else
				{
					//----------------------------------------
					//In case there is no DTP, we should
					//dealing with responses here ...
					FtpResponse response = _cc.SendCommandEx(timeout, "QUIT");
					FtpClient.CheckCompletionResponse(response);
				}
			}
			finally
			{
				if(true == ccLocked)
					_cc.Unlock();
			}
		}

		internal IAsyncResult BeginExecute(int timeout, AsyncCallback cb, object state)
		{

			Quit_SO stateObj = new Quit_SO(timeout, cb, state);

			//----------------------------------------
			//Lock Control connection.
			//
			_cc.BeginLock(Timeout.Infinite,
				new WaitOrTimerCallback(Lock_End),
				stateObj);

			return stateObj;
		}

		void Lock_End(object state, bool timedout)
		{
			Quit_SO stateObj = (Quit_SO)state;
			try
			{
				stateObj.UpdateContext();
				//----------------------------------------
				//Indicate that we lock CC
				stateObj.CCLocked = true;

				if(null != _client.CurrentDTP)
				{
					//----------------------------------------
					//In case when we have active DTP we delegate
					//handling responses to it and here only send
					//quit command
					_cc.BeginSendCommand(stateObj.Timeout, 
						"QUIT",
						new AsyncCallback(SendCmd_End),
						stateObj);
				}
				else
				{
					//----------------------------------------
					//In case there is no DTP, we should
					//dealing with responses here ...
					_cc.BeginSendCommandEx(stateObj.Timeout, 
						"QUIT",
						new AsyncCallback(SendCmdEx_End),
						stateObj);
				}
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void SendCmd_End(IAsyncResult ar)
		{
			Quit_SO stateObj = (Quit_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				//----------------------------------------
				//finish sending command
				_cc.EndSendCommand(ar);

				//----------------------------------------
				//Notify DTP that it should handle quit
				//issue
				_client.CurrentDTP.Quit();
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			finally
			{
				//----------------------------------------
				//Unlock control connection
				_cc.Unlock();

				stateObj.SetCompleted();
			}
		}

		void SendCmdEx_End(IAsyncResult ar)
		{
			Quit_SO stateObj = (Quit_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				//----------------------------------------
				//finish sending command
				FtpResponse response = _cc.EndSendCommandEx(ar);

				//----------------------------------------
				//Check response
				FtpClient.CheckCompletionResponse(response);

			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			finally
			{
				//----------------------------------------
				//Unlock control connection
				_cc.Unlock();

				stateObj.SetCompleted();
			}
		}

		internal void EndExecute(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(Quit_SO));
			HandleAsyncEnd(ar, false);
		}
	}
}
