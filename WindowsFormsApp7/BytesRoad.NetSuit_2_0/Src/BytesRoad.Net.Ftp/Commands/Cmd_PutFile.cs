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
	/// Summary description for Cmd_PutFile.
	/// </summary>
	internal class Cmd_PutFile : AsyncBase, IDisposable
	{
		#region AsyncResult class
		class PutFile_SO : AsyncResultBase
		{
			Stream _userStream = null;

			internal PutFile_SO(Stream userStream, 
				AsyncCallback cb, 
				object state) : base(cb, state)
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

		internal Cmd_PutFile(FtpClient ftp)
		{
			_client = ftp;
		}

		internal Type ARType
		{
			get { return typeof(PutFile_SO); }
		}

		void CreateDTP()
		{
			_currentDTP = null;
			lock(this)
			{
				if(!_disposed)
					_currentDTP = new Cmd_RunDTP(_client);
			}
			if(_disposed)
				throw new ObjectDisposedException("Cmd_PutFile");
		}

		internal void Execute(int timeout, 
			Stream userStream, 
			string file, 
			long length)
		{
			SetProgress(true);
			try
			{
				CreateDTP();
				string cmd = "STOR " + file;

				_currentDTP.Execute(timeout,
					cmd,
					_client.DataType,
					-1, 
					new DTPStreamCommon(userStream, DTPStreamType.ForReading, length));
			}
			finally
			{
				if(null != _currentDTP)
				{
					_currentDTP.Dispose();
					_currentDTP = null;
				}
				SetProgress(false);
			}
		}

		internal IAsyncResult BeginExecute(int timeout, 
			Stream userStream,
			string fileName,
			long length,
			AsyncCallback cb, 
			object state)
		{
			PutFile_SO stateObj = null;
			SetProgress(true);
			try
			{
				CreateDTP();
				stateObj = new PutFile_SO(userStream,
					cb,
					state);

				string cmd = "STOR " + fileName;
				_currentDTP.BeginExecute(timeout,
					cmd,
					_client.DataType,
					-1,
					new DTPStreamCommon(userStream, DTPStreamType.ForReading, length),
					new AsyncCallback(this.RunDTP_End),
					stateObj);
			}
			catch(Exception e)
			{
				if(null != _currentDTP)
				{
					_currentDTP.Dispose();
					_currentDTP = null;
				}
				SetProgress(false);
				throw e;
			}
			return stateObj;
		}

		void RunDTP_End(IAsyncResult ar)
		{
			PutFile_SO stateObj = (PutFile_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				_currentDTP.EndExecute(ar);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			finally
			{
				_currentDTP.Dispose();
				_currentDTP = null;
				stateObj.SetCompleted();
			}
		}

		internal void EndExecute(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(PutFile_SO));
			HandleAsyncEnd(ar, true);
		}

		#region Disposable pattern
		~Cmd_PutFile()
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

				try
				{
					IDisposable curDtp = _currentDTP;
					if(null != curDtp)
						curDtp.Dispose();
				}
				catch(Exception)
				{
				}
			}
		}
		#endregion
	}
}
