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
	/// Summary description for Cmd_Single.
	/// </summary>
	internal class Cmd_Single : AsyncBase, IDisposable
	{
		#region AsyncResult class
		class Single_SO : AsyncResultBase
		{
			internal Single_SO(AsyncCallback cb, object state) : base(cb, state)
			{
			}
		}
		#endregion

		FtpClient _client = null;
		FtpControlConnection _cc = null;
		Type _arType = null;

		internal Cmd_Single(FtpClient ftp)
		{
			_client = ftp;
			_cc = ftp.ControlConnection;
		}

		internal Type ARType
		{
			get { return _arType; }
		}

		internal FtpResponse Execute(int timeout, string command)
		{
			SetProgress(true);
			FtpResponse response = null;
			try
			{
				response = _cc.SendCommandEx(timeout, command);
			}
			finally
			{
				SetProgress(false);
			}
			return response;
		}

		internal IAsyncResult BeginExecute(int timeout, string command, AsyncCallback cb, object state)
		{
			_arType = null;
			IAsyncResult ar = _cc.BeginSendCommandEx(timeout, command, cb, state);
			if(null != ar)
				_arType = ar.GetType();
			return ar;
		}

		internal FtpResponse EndExecute(IAsyncResult ar)
		{
			return _cc.EndSendCommandEx(ar);
		}

		#region Disposable pattern
		~Cmd_Single()
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
