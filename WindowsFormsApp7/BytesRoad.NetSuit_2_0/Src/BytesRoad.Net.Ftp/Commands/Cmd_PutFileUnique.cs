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
using System.Text;
using System.Text.RegularExpressions;


using BytesRoad.Diag;
using BytesRoad.Net.Ftp;
using BytesRoad.Net.Ftp.Advanced;


namespace BytesRoad.Net.Ftp.Commands
{
	/// <summary>
	/// Summary description for Cmd_PutFileUnique.
	/// </summary>
	internal class Cmd_PutFileUnique : AsyncBase, IDisposable
	{
		#region AsyncResult class
		class PutFileUnique_SO : AsyncResultBase
		{
			Stream _userStream = null;

			internal PutFileUnique_SO(Stream userStream, 
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
		FtpControlConnection _cc = null;
		string _uniqueFileName = null;
		ArrayList _nameRegEx = new ArrayList();
		bool _disposed = false;
		Cmd_RunDTP _currentDTP = null;

		internal Cmd_PutFileUnique(FtpClient ftp, Regex regEx)
		{
			_client = ftp;
			_cc = _client.ControlConnection;

			//---------------------------------
			//place user defined reg ex
			//at the beginning
			if(null != regEx)
				_nameRegEx.Add(regEx);

			_nameRegEx.Add(new Regex("^150[- ]FILE: *(?<name>.*)\r\n$", RegexOptions.IgnoreCase | RegexOptions.Compiled));
			_nameRegEx.Add(new Regex("^150[- ]Opening BINARY mode data connection for *(?<name>.*)\r\n$", RegexOptions.IgnoreCase | RegexOptions.Compiled));
		}

		internal Type ARType
		{
			get { return typeof(PutFileUnique_SO); }
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
				throw new ObjectDisposedException("Cmd_PutFileUnique");
		}

		internal string Execute(int timeout, 
			Stream userStream, 
			long length)
		{
			SetProgress(true);
			_uniqueFileName = null;
			try
			{
				CreateDTP();
				string cmd = "STOU";

				_cc.ResponseReceived += new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);
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
				_cc.ResponseReceived -= new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);
				SetProgress(false);
			}
			return _uniqueFileName;
		}

		internal IAsyncResult BeginExecute(int timeout, 
			Stream userStream,
			long length,
			AsyncCallback cb, 
			object state)
		{
			PutFileUnique_SO stateObj = null;
			SetProgress(true);
			_uniqueFileName = null;
			try
			{
				CreateDTP();
				stateObj = new PutFileUnique_SO(userStream,
					cb,
					state);

				_cc.ResponseReceived += new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);

				string cmd = "STOU";
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
				_cc.ResponseReceived -= new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);
				SetProgress(false);
				throw e;
			}
			return stateObj;
		}

		void RunDTP_End(IAsyncResult ar)
		{
			PutFileUnique_SO stateObj = (PutFileUnique_SO)ar.AsyncState;
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
				_cc.ResponseReceived -= new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);
				stateObj.SetCompleted();
			}
		}

		internal string EndExecute(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(PutFileUnique_SO));
			HandleAsyncEnd(ar, true);
			return _uniqueFileName;
		}

		private void CC_ResponseReceived(object sender, ResponseReceivedEventArgs e)
		{
			FtpResponse response = e.Response;
			int code = response.Code;
			if(code != 150)
				return;

			//FtpResponseLine line = response.Lines[0];
			//string resText = _client.UsedEncoding.GetString(line.Content);
			string resText = response.RawStrings[0]; //_client.UsedEncoding.GetString(line.Content);

			//------------------------------------
			//Here we need to extract the name 
			//from the response.

			for(int i=0;i<_nameRegEx.Count;i++)
			{
				Regex r = (Regex)_nameRegEx[i];
				Match m = r.Match(resText);
				if(null != m)
				{
					Group gr = m.Groups["name"];
					if(gr.Success)
					{
						_uniqueFileName = gr.Value.TrimEnd('.');
						break;
					}
				}
			}
		}

		#region Disposable pattern
		~Cmd_PutFileUnique()
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
