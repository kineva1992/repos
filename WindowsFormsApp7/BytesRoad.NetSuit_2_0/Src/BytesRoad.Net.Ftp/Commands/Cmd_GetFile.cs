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
using System.IO;

using BytesRoad.Diag;
using BytesRoad.Net.Ftp;
using BytesRoad.Net.Ftp.Advanced;


namespace BytesRoad.Net.Ftp.Commands
{
	/// <summary>
	/// Summary description for Cmd_GetFile.
	/// </summary>
	internal class Cmd_GetFile : AsyncBase, IDisposable
	{
		#region AsyncResult class
		class GetFile_SO : AsyncResultBase
		{
			Stream _userStream = null;

			internal GetFile_SO(Stream userStream, AsyncCallback cb, object state) : base(cb, state)
			{
				_userStream = userStream;
			}

			internal Stream UserStream
			{
				get { return _userStream; }
			}
		}
		#endregion

		FtpClient _client = null;
		Cmd_RunDTP _currentDTP = null;
		bool _disposed = false;


		internal Cmd_GetFile(FtpClient ftp)
		{
			_client = ftp;
		}

		internal Type ARType
		{
			get { return typeof(GetFile_SO); }
		}

		#region Helpers

		Exception GetDisposedException()
		{
			return new ObjectDisposedException(this.GetType().FullName, "Object was disposed.");
		}

		void CheckDisposed()
		{
			if(_disposed)
				throw GetDisposedException();
		}

		void CreateDTP()
		{
			_currentDTP = null;
			lock(this)
			{
				if(!_disposed)
					_currentDTP = new Cmd_RunDTP(_client);
			}
			CheckDisposed();
		}

		#endregion

		internal void Execute(int timeout, 
			Stream userStream, 
			string file, 
			long offset, 
			long length)
		{
			SetProgress(true);
			try
			{
				CreateDTP();
				string cmd = "RETR " + file;

				_currentDTP.Execute(timeout,
					cmd,
					_client.DataType,
					offset, 
					new DTPStreamCommon(userStream, DTPStreamType.ForWriting, length));
			}
			finally
			{
				SetProgress(false);
				CheckDisposed();
			}
		}

		internal IAsyncResult BeginExecute(int timeout, 
			Stream userStream,
			string file,
			long offset,
			long length,
			AsyncCallback cb, 
			object state)
		{
			GetFile_SO stateObj = null;
			SetProgress(true);
			try
			{
				CreateDTP();
				stateObj = new GetFile_SO(userStream, cb, state);

				string cmd = "RETR " + file;
				_currentDTP.BeginExecute(timeout,
					cmd,
					_client.DataType,
					offset,
					new DTPStreamCommon(userStream, DTPStreamType.ForWriting, length),
					new AsyncCallback(this.RunDTP_End),
					stateObj);
			}
			catch
			{
				SetProgress(false);
				CheckDisposed();
				throw;
			}
			return stateObj;
		}

		void RunDTP_End(IAsyncResult ar)
		{
			GetFile_SO stateObj = (GetFile_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				_currentDTP.EndExecute(ar);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			catch
			{
				NSTrace.WriteLineError("Non-CLS exception at: " + Environment.StackTrace);
				throw;
			}
			stateObj.SetCompleted();
		}

		internal void EndExecute(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(GetFile_SO));
			HandleAsyncEnd(ar, true);
		}

		#region Disposable pattern
		~Cmd_GetFile()
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
				_disposed = true;

				if(disposing)
				{
				}

				IDisposable curDtp = _currentDTP;
				if(null != curDtp)
					curDtp.Dispose();
			}
		}
		#endregion
	}
}
