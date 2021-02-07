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
	/// Summary description for Cmd_Disconnect.
	/// </summary>
	internal class Cmd_Disconnect : AsyncBase
	{
		#region AsyncResult class
		class Disconnect_SO : AsyncResultBase
		{
			int _timeout = 0;

			internal Disconnect_SO(int timeout,
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

		Cmd_Reset _cmdReset = null;
		Cmd_Quit _cmdQuit = null;
		IAsyncResult _asyncResult;

		internal Cmd_Disconnect(FtpClient ftp)
		{
			_cmdReset = new Cmd_Reset(ftp);
			_cmdQuit = new Cmd_Quit(ftp);
		}

		internal Type ARType
		{
			get { return typeof(Disconnect_SO); }
		}

		internal IAsyncResult AsyncResult
		{
			get { return _asyncResult; }
		}

		internal void Execute(int timeout)
		{
			_cmdReset.Execute(timeout);

			_cmdQuit.Execute(timeout);
		}

		internal IAsyncResult BeginExecute(int timeout, AsyncCallback cb, object state)
		{
			Disconnect_SO stateObj = new Disconnect_SO(timeout, cb, state);
			_asyncResult = stateObj;
			try
			{
				_cmdReset.BeginExecute(timeout,
					new AsyncCallback(Reset_End),
					stateObj);
			}
			catch
			{
				stateObj.SetCompleted();
				throw;
			}
			return stateObj;
		}

		void Reset_End(IAsyncResult ar)
		{
			Disconnect_SO stateObj = (Disconnect_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				_cmdReset.EndExecute(ar);
				_cmdQuit.BeginExecute(stateObj.Timeout,
					new AsyncCallback(Quit_End),
					stateObj);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void Quit_End(IAsyncResult ar)
		{
			Disconnect_SO stateObj = (Disconnect_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				_cmdQuit.EndExecute(ar);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			stateObj.SetCompleted();
		}

		internal void EndExecute(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(Disconnect_SO));
			HandleAsyncEnd(ar, false);
		}
	}
}
