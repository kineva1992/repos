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
	/// Summary description for Cmd_Reset.
	/// </summary>
	internal class Cmd_Reset : AsyncBase
	{
		#region AsyncResult class
		class Reset_SO : AsyncResultBase
		{
			int _timeout = 0;

			internal Reset_SO(int timeout,
				AsyncCallback cb, 
				object state) : base(cb, state)
			{
				_timeout = timeout;
			}

			internal int Timeout
			{
				get { return _timeout; }
			}
		}
		#endregion

		FtpClient _client = null;
		FtpControlConnection _cc = null;

		internal Cmd_Reset(FtpClient ftp)
		{
			_client = ftp;
			_cc = ftp.ControlConnection;
		}

		internal Type ARType
		{
			get { return typeof(Reset_SO); }
		}

		#region Helpers
		Exception GetTimeoutException(int timeout)
		{
			string msg = string.Format("Unable to reset data connection within {0} milliseconds.", timeout);
			return new FtpTimeoutException(msg);
		}
		#endregion

		internal void Execute(int timeout)
		{
			bool finished = false;

			//---------------------------------
			//Lock CC
			if(_cc.Lock(timeout))
			{
				//------------------------------------------
				//Reset DTP
				try
				{
					Cmd_RunDTP curDTP = _client.CurrentDTP;
					if(null != curDTP)
						curDTP.Reset();
				}
				finally
				{
					_cc.Unlock();
				}

				//------------------------------------------
				//Wait till DTP exits
				finished = _client.WaitForDTPFinish(timeout);
				if(!finished)
				{
					string msg = string.Format("DTP is not finished withing {0} time: ", timeout);
					NSTrace.WriteLineError(msg + Environment.StackTrace);
					throw GetTimeoutException(timeout);
				}
			}
			else
			{
				string msg = string.Format("Unable to lock CC within {0} time: ", timeout);
				NSTrace.WriteLineError(msg + Environment.StackTrace);
				throw GetTimeoutException(timeout);
			}
		}

		internal IAsyncResult BeginExecute(int timeout, 
			AsyncCallback cb, 
			object state)
		{
			Reset_SO stateObj = new Reset_SO(timeout, cb, state);

			//----------------------------------------
			//Lock Control connection.
			_cc.BeginLock(timeout,
				new WaitOrTimerCallback(Lock_End),
				stateObj);

			return stateObj;
		}

		void Lock_End(object state, bool timedout)
		{
			Reset_SO stateObj = (Reset_SO)state;
			try
			{
				stateObj.UpdateContext();
				if(timedout)
				{
					string msg = string.Format("Unable to lock CC within {0} time: ", stateObj.Timeout);
					NSTrace.WriteLineError(msg + Environment.StackTrace);
					stateObj.Exception = GetTimeoutException(stateObj.Timeout);
					stateObj.SetCompleted();
				}
				else
				{
					try
					{
						Cmd_RunDTP curDTP = _client.CurrentDTP;
						if(null != curDTP)
							curDTP.Reset();
					}
					finally
					{
						_cc.Unlock();
					}

					//----------------------------------------
					//Wait till DTP finishes
					_client.BeginWaitForDTPFinished(stateObj.Timeout,
						new WaitOrTimerCallback(DTPFinishedWait_End),
						stateObj);
				}
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void DTPFinishedWait_End(object state, bool timedout)
		{
			Reset_SO stateObj = (Reset_SO)state;
			try
			{
				stateObj.UpdateContext();
				if(timedout)
				{
					string msg = string.Format("DTP is not finished withing {0} time: ", stateObj.Timeout);
					NSTrace.WriteLineError(msg + Environment.StackTrace);
					stateObj.Exception = GetTimeoutException(stateObj.Timeout);
				}
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			stateObj.SetCompleted();
		}

		internal void EndExecute(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(Reset_SO));
			HandleAsyncEnd(ar, false);
		}
	}
}
