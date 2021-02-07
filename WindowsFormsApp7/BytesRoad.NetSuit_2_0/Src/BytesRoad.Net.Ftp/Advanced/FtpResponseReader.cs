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
using System.Net.Sockets;
using System.Threading;
using System.Text;

using BytesRoad.Net.Sockets;
using BytesRoad.Diag;

namespace BytesRoad.Net.Ftp.Advanced
{
	/// <summary>
	/// Summary description for FtpResponseReader.
	/// </summary>
	internal class FtpResponseReader : AsyncBase
	{
		#region Async class
		class ReaderStateObject : AsyncResultBase
		{
			internal ReaderStateObject(AsyncCallback cb, 
									object callerState) : base(cb, callerState)
			{
			}
		}
		#endregion

		byte[] _recvBuffer = null;
		SocketEx _socket = null;
		FtpResponse _response = null;
		LinesBuilder _linesBuilder = null;
		Encoding _encoding = null;

		internal FtpResponseReader(SocketEx s, Encoding en, int maxLineLength)
		{
			if(null == s)
				throw new ArgumentNullException("s", "FtpResponseReader requires non null socket.");

			if(null == en)
				throw new ArgumentNullException("en", "FtpResponseReader requires non null encoding.");

			//maxLineLength = 10;
					
			_socket = s;
			_recvBuffer = new byte[maxLineLength*2];
			_linesBuilder = new LinesBuilder(maxLineLength);

			//---------------------------------------
			// single line mode is enabled
			// because sometimes few responses (150, 200)
			// may came in the single packet
			// and we should read only first one
			_linesBuilder.SingleLineMode = true;
			_encoding = en;
		}

		#region Events
		internal delegate void NewLineEventHandler(FtpResponseReader sender, FtpResponseLine line);
		internal event NewLineEventHandler NewLineEvent;
		void OnNewLine(FtpResponse sender, FtpResponseLine line)
		{
			if(null != NewLineEvent)
				NewLineEvent(this, line);
		}
		#endregion	

		#region Attributes
		internal FtpResponse Response
		{
			get { return _response; }
		}

		internal Encoding Encoding
		{
			set { _encoding = value; }
		}
		#endregion


		Exception GetClosedException()
		{
			return new FtpProtocolException("FTP server has unexpectedly terminated the connection.", null, -1, -1);
		}

		private void LinesBuilder_NewLineEvent(object sender, NewLineEventArgs e)
		{
			_response.PutLine(e.Line);
		}

		void ParseExistentData()
		{
			try
			{
				_linesBuilder.ParseExistentData();
			}
			catch(LineFormatException ex)
			{
				throw new FtpProtocolException(ex.Message,
					_response,
					ex.Line,
					ex.Position);
			}
			catch(LineLengthExceededException e)
			{
				string msg = string.Format("Response line exceed maximum length ({0} bytes).", _linesBuilder.MaxLineLength);
				throw new FtpProtocolException(msg,
					_response,
					e.Line,
					-1);
			}
		}

		internal FtpResponse ReadResponse(int timeout)
		{
			SetProgress(true);
			try
			{
				_response = new FtpResponse(_encoding);
				_response.NewLineEvent += new FtpResponse.NewLineEventHandler(this.OnNewLine);
				_linesBuilder.NewLineEvent += new BytesRoad.Net.Ftp.Advanced.LinesBuilder.NewLineEventHandler(LinesBuilder_NewLineEvent);
				_socket.ReceiveTimeout = timeout;

				while(true)
				{
					ParseExistentData();

					if(_response.IsCompleted)
						break;

					if(_linesBuilder.Available > 0)
						continue;

					int readNum = _socket.Receive(_recvBuffer);
					if(0 == readNum)
						throw GetClosedException(); 

					_linesBuilder.PutData(_recvBuffer, readNum, false);
				}
			}
			finally
			{
				SetProgress(false);
				_linesBuilder.NewLineEvent -= new BytesRoad.Net.Ftp.Advanced.LinesBuilder.NewLineEventHandler(LinesBuilder_NewLineEvent);
				_response.NewLineEvent -= new FtpResponse.NewLineEventHandler(this.OnNewLine);

				_linesBuilder.ClearCompleted();
			}
			return _response;
		}
		
		void BuildResponse(ReaderStateObject stateObj)
		{
			bool needMoreData = true;
			stateObj.UpdateContext();
			do 
			{
				ParseExistentData();
				if(_response.IsCompleted)
				{
					stateObj.SetCompleted();
					needMoreData = false;
					break;
				}
			} while(_linesBuilder.Available > 0);

			if(needMoreData)
			{
				//start reading response
				_socket.BeginReceive(_recvBuffer,
					0,
					_recvBuffer.Length, 
					new AsyncCallback(this.OnRecieved),
					stateObj);
			}
		}

		internal IAsyncResult BeginReadResponse(int timeout, 
			AsyncCallback cb, 
			object state)
		{
			SetProgress(true);
			ReaderStateObject stateObj = null;
			try
			{
				_response = new FtpResponse(_encoding);
				stateObj = new ReaderStateObject(cb, state);
				_response.NewLineEvent += new FtpResponse.NewLineEventHandler(this.OnNewLine);
				_linesBuilder.NewLineEvent += new BytesRoad.Net.Ftp.Advanced.LinesBuilder.NewLineEventHandler(LinesBuilder_NewLineEvent);
				_socket.ReceiveTimeout = timeout;

				BuildResponse(stateObj);
			}
			catch
			{
				SetProgress(false);
				_response.NewLineEvent -= new FtpResponse.NewLineEventHandler(this.OnNewLine);
				_linesBuilder.NewLineEvent -= new BytesRoad.Net.Ftp.Advanced.LinesBuilder.NewLineEventHandler(LinesBuilder_NewLineEvent);
				throw;
			}
			return stateObj;
		}

		private void OnRecieved(IAsyncResult ar)
		{
			ReaderStateObject stateObj = (ReaderStateObject)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();

				int readNum = _socket.EndReceive(ar);
				if(0 == readNum)
					throw GetClosedException();

				_linesBuilder.PutData(_recvBuffer, readNum, false);
				BuildResponse(stateObj);
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
			catch
			{
				NSTrace.WriteLineError("Non-CLS exception at: " + Environment.StackTrace);
				throw;
			}
		}

		internal FtpResponse EndReadResponse(IAsyncResult ar)
		{
			try
			{
				AsyncBase.VerifyAsyncResult(ar, typeof(ReaderStateObject));
				HandleAsyncEnd(ar, true);
			}
			finally
			{
				_response.NewLineEvent -= new FtpResponse.NewLineEventHandler(this.OnNewLine);
				_linesBuilder.NewLineEvent -= new BytesRoad.Net.Ftp.Advanced.LinesBuilder.NewLineEventHandler(LinesBuilder_NewLineEvent);
			}
			return _response;
		}
	}
}
