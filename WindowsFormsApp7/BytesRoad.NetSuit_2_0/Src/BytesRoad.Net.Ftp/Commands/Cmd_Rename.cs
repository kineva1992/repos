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

using BytesRoad.Diag;
using BytesRoad.Net.Ftp;
using BytesRoad.Net.Ftp.Advanced;

namespace BytesRoad.Net.Ftp.Commands
{
	/// <summary>
	/// Summary description for Cmd_Rename.
	/// </summary>
	internal class Cmd_Rename : AsyncBase, IDisposable
	{
		#region AsyncResult class
		class Rename_SO : AsyncResultBase
		{
			string _dstFile = null;
			int _timeout = 0;

			internal Rename_SO(int timeout,
				string dstFile,
				AsyncCallback cb, 
				object state) : base(cb, state)
			{
				_timeout = timeout;
				_dstFile = dstFile;
			}

			internal int Timeout
			{
				get { return _timeout; }
			}

			internal string DstFile
			{
				get { return _dstFile; }
			}
		}
		#endregion

		FtpClient _client = null;
		FtpControlConnection _cc = null;

		internal Cmd_Rename(FtpClient ftp)
		{
			_client = ftp;
			_cc = ftp.ControlConnection;
		}

		internal Type ARType
		{
			get { return typeof(Rename_SO); }
		}

		internal void Execute(int timeout, 
			string srcFile, 
			string dstFile)
		{
			SetProgress(true);
			FtpResponse response = null;
			try
			{
				string cmd = "RNFR " + srcFile;
				response = _cc.SendCommandEx(timeout, cmd);
				FtpClient.CheckIntermediateResponse(response);

				cmd = "RNTO " + dstFile;
				response = _cc.SendCommandEx(timeout, cmd);
				FtpClient.CheckCompletionResponse(response);
			}
			finally
			{
				SetProgress(false);
			}
		}

		internal IAsyncResult BeginExecute(int timeout, 
			string srcFile, 
			string dstFile,
			AsyncCallback callback,
			object state)
		{
			Rename_SO stateObj = null;
			
			SetProgress(true);
			try
			{
				stateObj = new Rename_SO(timeout, dstFile, callback, state);

				string cmd = "RNFR " + srcFile;
				_cc.BeginSendCommandEx(timeout,
					cmd,
					new AsyncCallback(RNFRCmd_End),
					stateObj);
			}
			catch(Exception e)
			{
				SetProgress(false);
				throw e;
			}
			return stateObj;
		}

		void RNFRCmd_End(IAsyncResult ar)
		{
			Rename_SO stateObj = (Rename_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				FtpResponse response = _cc.EndSendCommandEx(ar);
				FtpClient.CheckIntermediateResponse(response);

				string cmd = "RNTO " + stateObj.DstFile;
				_cc.BeginSendCommandEx(stateObj.Timeout,
					cmd,
					new AsyncCallback(RNTOCmd_End),
					stateObj);

			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void RNTOCmd_End(IAsyncResult ar)
		{
			Rename_SO stateObj = (Rename_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				FtpResponse response = _cc.EndSendCommandEx(ar);
				FtpClient.CheckCompletionResponse(response);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			stateObj.SetCompleted();
		}

		internal void EndExecute(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(Rename_SO));
			HandleAsyncEnd(ar, true);
		}

		#region Disposable pattern
		~Cmd_Rename()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			lock(this)
			{
				if(disposing)
				{
				}

				try
				{
				}
				catch(Exception)
				{
				}
			}
		}
		#endregion
	}
}
