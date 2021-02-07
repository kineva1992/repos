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
	/// Summary description for Cmd_GetDirectoryList.
	/// </summary>
	internal class Cmd_GetDirectoryList : AsyncBase, IDisposable
	{
		#region AsyncResult class
		class List_SO : AsyncResultBase
		{
			DTPStream _userStream = null;
			
			internal List_SO(DTPStream userStream, AsyncCallback cb, object state) : base(cb, state)
			{
				_userStream = userStream;
			}


			internal DTPStream UserStream
			{
				get { return _userStream; }
			}
		}

		#endregion

		FtpClient _client = null;
		Cmd_RunDTP _currentDTP = null;
		bool _disposed = false;

		LinesBuilder _linesBuilder = null;
		ArrayList _items = new ArrayList();
		static char[] crlfChars = new char[]{'\r', '\n'};

		internal Cmd_GetDirectoryList(FtpClient ftp)
		{
			_client = ftp;
			_linesBuilder = new LinesBuilder(ftp.MaxLineLength);
		}

		internal Type ARType
		{
			get { return typeof(List_SO); }
		}

		#region Events
		void OnNewFtpItem(FtpItem item)
		{
			if(null != NewFtpItem)
				NewFtpItem(this, new NewFtpItemEventArgs(item));
		}

		internal delegate void NewFtpItemEventHandler(object sender, NewFtpItemEventArgs e);
		internal event NewFtpItemEventHandler NewFtpItem;
		#endregion

		private void List_OnNewLineEvent(object sender, NewLineEventArgs e)
		{
			string line = _client.UsedEncoding.GetString(e.Line.Content.Data, 0, e.Line.Content.Size);
			line = line.TrimEnd(crlfChars);

			FtpItem item = _client.FtpItemResolver.Resolve(line);
			_items.Add(item);
			OnNewFtpItem(item);
		}

		private void List_OnDataTransfered(object sender, DataTransferedEventArgs e)
		{
			_linesBuilder.PutData(e.Data, e.LastTransfered);
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
				throw new ObjectDisposedException("Cmd_GetDirectoryList");
		}

		internal FtpItem[] Execute(int timeout, string dir)
		{
			FtpItem[] items = null;
			SetProgress(true);
			try
			{
				CreateDTP();
				_linesBuilder.NewLineEvent +=new LinesBuilder.NewLineEventHandler(List_OnNewLineEvent);
				_items.Clear();
				_linesBuilder.Reset();

				string cmd = "LIST";
				if(null != dir)
					cmd += " " + dir;

				_currentDTP.DataTransfered += new Cmd_RunDTP.DataTransferedEventHandler(List_OnDataTransfered);
				try
				{
					_currentDTP.Execute(timeout, 
						cmd, 
						FtpDataType.Ascii,
						-1,
						new DTPStreamCommon(Stream.Null, DTPStreamType.ForWriting));
				}
				finally
				{
					_currentDTP.DataTransfered -= new Cmd_RunDTP.DataTransferedEventHandler(List_OnDataTransfered);
				}

				items = new FtpItem[_items.Count];
				_items.CopyTo(items);
			}
			finally
			{
				if(null != _currentDTP)
				{
					_currentDTP.Dispose();
					_currentDTP = null;
				}
				_linesBuilder.NewLineEvent -= new LinesBuilder.NewLineEventHandler(List_OnNewLineEvent);
				SetProgress(false);
			}
			return items;
		}

		internal IAsyncResult BeginExecute(int timeout, string path, AsyncCallback cb, object state)
		{
			List_SO stateObj = null;
			SetProgress(true);
			try
			{
				CreateDTP();
				stateObj = new List_SO(new DTPStreamCommon(Stream.Null, DTPStreamType.ForWriting), 
					cb,
					state);

				_linesBuilder.NewLineEvent +=new LinesBuilder.NewLineEventHandler(List_OnNewLineEvent);
				_items.Clear();
				_linesBuilder.Reset();

				string cmd = "LIST";
				if(null != path)
					cmd += " " + path;

				_currentDTP.DataTransfered += new Cmd_RunDTP.DataTransferedEventHandler(List_OnDataTransfered);
				try
				{
					_currentDTP.BeginExecute(timeout,
						cmd,
						FtpDataType.Ascii,
						-1,
						stateObj.UserStream,
						new AsyncCallback(RunCmd_End),
						stateObj);
				}
				catch(Exception e)
				{
					_currentDTP.DataTransfered -= new Cmd_RunDTP.DataTransferedEventHandler(List_OnDataTransfered);
					throw e;
				}
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

		void RunCmd_End(IAsyncResult ar)
		{
			List_SO stateObj = (List_SO)ar.AsyncState;
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
				_currentDTP.DataTransfered -= new Cmd_RunDTP.DataTransferedEventHandler(List_OnDataTransfered);
				_currentDTP.Dispose();
				_currentDTP = null;
				_linesBuilder.NewLineEvent -=new LinesBuilder.NewLineEventHandler(List_OnNewLineEvent);
				stateObj.SetCompleted();
			}
		}

		internal FtpItem[] EndExecute(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(List_SO));
			HandleAsyncEnd(ar, true);
			
			FtpItem[] ret = new FtpItem[_items.Count];
			_items.CopyTo(ret);
			return ret;
		}

		#region Disposable pattern
		~Cmd_GetDirectoryList()
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
