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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;


using BytesRoad.Net.Sockets;
using BytesRoad.Diag;


namespace BytesRoad.Net.Ftp.Advanced
{
	/// <summary>
	/// Summary description for FtpControlConnection.
	/// </summary>
	internal class FtpControlConnection : AsyncBase, IDisposable
	{
		#region AsyncResult classes
		class Connect_SO : AsyncResultBase
		{
			int _timeout = -1;
			internal Connect_SO(int timeout, 
							AsyncCallback cb, 
							object callerState) : base(cb, callerState)
			{
				_timeout = timeout;
			}

			internal int Timeout
			{
				get { return _timeout; }
			}
		}

		class CCSendStateObject : AsyncResultBase
		{
			byte[] _cmd = null;
			int _sent = 0;
			int _timeout;
			internal CCSendStateObject(int timeout,
				byte[] cmd, 
				AsyncCallback cb, 
				object callerState) : base(cb, callerState)
			{
				_cmd = cmd;
				_timeout = timeout;
			}

			internal byte[] Command
			{
				get { return _cmd; }
			}

			internal int Sent
			{
				get { return _sent; }
				set { _sent = value; }
			}
			internal int Timeout
			{
				get { return _timeout; }
			}
		}
		
		class CCReadStateObject : AsyncResultBase
		{
			internal CCReadStateObject(AsyncCallback cb, object callerState) : base(cb, callerState)
			{
			}
		}
		#endregion
		
		SocketEx _socket = null;
		Random _rand = new Random(unchecked((int)DateTime.Now.Ticks)); 
		FtpResponseReader _reader = null;
		FtpResponse _response = null;
		Encoding _encoding = Encoding.Default;
		AutoResetEvent _lock = new AutoResetEvent(true);
		FtpClient _client = null;
		bool _disposed = false;
		static char[] crlfChars = new char[]{'\r', '\n'};

		internal FtpControlConnection(FtpClient ftp, Encoding en, int maxLineLength)
		{
			if(null == en)
				throw new ArgumentNullException("en", "FtpControlConnection requires non null encoding.");

			_encoding = en;
			_socket = ftp.GetSocket();
			_reader = new FtpResponseReader(_socket, _encoding, maxLineLength);
			_client = ftp;
		}


		#region Events
		void OnCommandSent(string cmd)
		{
			try
			{
				if(null != CommandSent)
				{
					cmd = cmd.TrimEnd(crlfChars);
					CommandSent(this, new CommandSentEventArgs(cmd));
				}
			}
			catch(Exception e)
			{
				NSTrace.WriteLineError("OnCommandSent event ex: " + e.ToString());
				throw;
			}
			catch
			{
				NSTrace.WriteLineError("OnCommandSent event non cls ex.");
				throw;
			}
		}

		void OnCommandSent(byte[] cmd)
		{
			try
			{
				if(null != CommandSent)
				{
					string scmd = _encoding.GetString(cmd);
					scmd = scmd.TrimEnd(crlfChars);
					CommandSent(this, new CommandSentEventArgs(scmd));
				}
			}
			catch(Exception e)
			{
				NSTrace.WriteLineError("OnCommandSent event ex: " + e.ToString());
				throw;
			}
			catch
			{
				NSTrace.WriteLineError("OnCommandSent event non cls ex.");
				throw;
			}
		}

		void OnResponseReceived()
		{
			try
			{
				if(null != ResponseReceived)
					ResponseReceived(this, new ResponseReceivedEventArgs(_response));
			}
			catch(Exception e)
			{
				NSTrace.WriteLineError("OnResponseReceived event ex: " + e.ToString());
				throw;
			}
			catch
			{
				NSTrace.WriteLineError("OnResponseReceived event non cls ex.");
				throw;
			}
		}

		internal delegate void CommandSentEventHandler(object sender, CommandSentEventArgs e);
		internal event CommandSentEventHandler CommandSent;

		internal delegate void ResponseReceivedEventHandler(object sender, ResponseReceivedEventArgs e);
		internal event ResponseReceivedEventHandler ResponseReceived;
		#endregion

		#region Attributes
		internal bool IsConnected
		{
			get 
			{ 
				try
				{
					return _socket.Connected; 
				}
				catch
				{
					CheckDisposed();
					throw;
				}
			}
		}

		internal SocketEx UsedSocket
		{
			get { return _socket; }
		}

		internal Encoding Encoding
		{
			set 
			{ 
				_encoding = value; 
				if(null != _reader)
					_reader.Encoding = value;
			}
		}
		#endregion

		#region Helper functions
		Exception GetTimeoutException(Exception e)
		{
			return new FtpTimeoutException("Timeout occurs.", e);
		}

		protected void CheckTimeoutException(SocketException e)
		{
			if(e.ErrorCode == SockErrors.WSAETIMEDOUT)
				throw GetTimeoutException(e);
		}

		IPEndPoint ConstructEndPoint(IPHostEntry host, int port)
		{
			if(0 >= host.AddressList.Length)
			{
				string msg = "Provided host structure do not contains addresses.";
				NSTrace.WriteLineError(msg);
				throw new ArgumentException(msg);
			}

			int addressNo = 0;
			if(1 < host.AddressList.Length)
				addressNo = _rand.Next(host.AddressList.Length - 1);
			
			return new IPEndPoint(host.AddressList[addressNo], port);
		}

		string AppendCRLF(string cmd)
		{
			if(false == cmd.EndsWith("\r\n"))
				return cmd + "\r\n";
			return cmd;
		}

		Exception GetDisposedException()
		{
			return new ObjectDisposedException(this.GetType().FullName, "Object was disposed.");
		}

		void CheckDisposed()
		{
			if(_disposed)
				throw GetDisposedException();
		}
		#endregion

		#region Locking functions
		class AsyncLocker
		{
			object _state;
			WaitOrTimerCallback _callback;

			internal AsyncLocker(WaitOrTimerCallback callback, 
				object state)
			{
				_state = state;
				_callback = callback;
			}

			internal object State
			{
				get { return _state; }
			}

			internal WaitOrTimerCallback Callback
			{
				get { return _callback; }
			}
		}

		internal bool Lock(int timeout)
		{
			CheckDisposed();
			try
			{
				return _lock.WaitOne(timeout, true);
			}
			catch
			{
				CheckDisposed();
				throw;
			}
		}

		internal void Unlock()
		{
			CheckDisposed();
			try
			{
				_lock.Set();
			}
			catch
			{
				CheckDisposed();
				throw;
			}
		}

		internal void BeginLock(int timeout, 
			WaitOrTimerCallback callback,
			object state)
		{
			CheckDisposed();
			try
			{
				ThreadPool.RegisterWaitForSingleObject(_lock,
					new WaitOrTimerCallback(OnLocked),
					new AsyncLocker(callback, state),
					timeout,
					true);
			}
			catch
			{
				CheckDisposed();
				throw;
			}
		}

		void OnLocked(object state, bool timedout)
		{
			AsyncLocker al = (AsyncLocker)state;
			al.Callback(al.State, timedout);
		}

		#endregion

		#region OnResponse function
		//this function used by routines - Connect, SendCommandEx, ReadResponse
		void OnResponse(IAsyncResult ar)
		{
			AsyncResultBase stateObj = (AsyncResultBase)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				_response = _reader.EndReadResponse(ar);
				OnResponseReceived();
			}
			catch(SocketException e)
			{
				if(_disposed)
					stateObj.Exception = GetDisposedException();
				else if(e.ErrorCode == SockErrors.WSAETIMEDOUT)
					stateObj.Exception = GetTimeoutException(e);
				else
					stateObj.Exception = e;
			}
			catch(Exception e)
			{
				if(_disposed)
					stateObj.Exception = GetDisposedException();
				else
					stateObj.Exception = e;
			}
			catch
			{
				if(_disposed)
					stateObj.Exception = GetDisposedException();
				else
				{
					NSTrace.WriteLineError("Non-CLS exception at: " + Environment.StackTrace);
					throw;
				}
			}
			_response = _reader.Response;
			stateObj.SetCompleted();
		}
		#endregion

		#region Connect functions
		internal FtpResponse Connect(int timeout, 
			string server, 
			int port)
		{
			CheckDisposed();

			NSTrace.WriteLineVerbose("CC: -> Connect");
			if(null == server)
				throw new ArgumentNullException("server");

			if(port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
				throw new ArgumentOutOfRangeException("port", "Value, specified for the port, is out of valid range."); 

			SetProgress(true);
			try
			{
				_socket.ConnectTimeout = timeout;
				
				string msg = string.Format("CC: Connecting (timeout: {0}, srv: {1}:{2})...", timeout, server, port);
				NSTrace.WriteLineInfo(msg);

				_socket.Connect(server, port);

				msg = string.Format("CC: Reading response...");
				NSTrace.WriteLineInfo(msg);

				_response = _reader.ReadResponse(timeout);

				NSTrace.WriteLineVerbose("CC: <- Connect");
			}
			catch(SocketException e)
			{
				CheckDisposed();
				CheckTimeoutException(e);
				throw;
			}
			catch(Exception)
			{
				CheckDisposed();
				throw;
			}
			catch
			{
				CheckDisposed();
				throw;
			}
			finally
			{
				SetProgress(false);
			}
			OnResponseReceived();
			return _response;
		}

		internal IAsyncResult BeginConnect(int timeout, 
			string server, 
			int port, 
			AsyncCallback cb, 
			object state)
		{
			CheckDisposed();

			Connect_SO stateObj = null;
			if(null == server)
				throw new ArgumentNullException("server");

			if(port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
				throw new ArgumentOutOfRangeException("port", "Value, specified for the port is out of valid range."); 

			SetProgress(true);
			try
			{
				stateObj = new Connect_SO(timeout, cb, state);

				_socket.ConnectTimeout = timeout;
				_socket.BeginConnect(server, port, 
					new AsyncCallback(this.OnEndConnectCB), 
					stateObj);
			}
			catch(SocketException e)
			{
				SetProgress(false);
				CheckDisposed();
				CheckTimeoutException(e);
				throw;
			}
			catch(Exception)
			{
				SetProgress(false);
				CheckDisposed();
				throw;
			}
			catch
			{
				SetProgress(false);
				CheckDisposed();
				throw;
			}
			return stateObj;
		}

		void OnEndConnectCB(IAsyncResult ar)
		{
			Connect_SO stateObj = (Connect_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				_socket.EndConnect(ar);
				_reader.BeginReadResponse(stateObj.Timeout, 
										new AsyncCallback(this.OnResponse),
										stateObj);
			}
			catch(SocketException e)
			{
				if(_disposed)
					stateObj.Exception = GetDisposedException();
				else if(e.ErrorCode == SockErrors.WSAETIMEDOUT)
					stateObj.Exception = GetTimeoutException(e);
				else
					stateObj.Exception = e;
				stateObj.SetCompleted();
			}
			catch(Exception e)
			{
				if(_disposed)
					stateObj.Exception = GetDisposedException();
				else
					stateObj.Exception = e;
				stateObj.SetCompleted();
			}
			catch
			{
				if(_disposed)
				{
					stateObj.Exception = GetDisposedException();
					stateObj.SetCompleted();
				}
				else
				{
					NSTrace.WriteLineError("Non-CLS exception at: " + Environment.StackTrace);
					throw;
				}
			}
		}

		internal FtpResponse EndConnect(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(Connect_SO), "EndConnect");
			HandleAsyncEnd(ar, true);
			return _response;
		}
		#endregion

		#region SendCommand functions
		internal void SendCommand(int timeout, string command)
		{
			CheckDisposed();

			if(null == command)
				throw new ArgumentNullException("command", "Value cannot be null.");

			command = AppendCRLF(command);

			SetProgress(true);
			_response = null;
			try
			{
				_socket.SendTimeout = timeout;
				byte[] cmdBytes = _encoding.GetBytes(command);
				int startPos = 0;
				while(startPos < cmdBytes.Length)
					startPos += _socket.Send(cmdBytes, startPos, cmdBytes.Length - startPos);

				OnCommandSent(command);
			}
			catch(SocketException e)
			{
				CheckDisposed();
				CheckTimeoutException(e);
				throw;
			}
			catch(Exception)
			{
				CheckDisposed();
				throw;
			}
			catch
			{
				CheckDisposed();
				throw;
			}
			finally
			{
				SetProgress(false);
			}
		}

		internal IAsyncResult BeginSendCommand(int timeout, string command, AsyncCallback cb, object state)
		{
			CheckDisposed();

			if(null == command)
				throw new ArgumentNullException("command", "'command' argument cannot be null");

			command = AppendCRLF(command);

			SetProgress(true);
			_response = null;
			try
			{
				_socket.SendTimeout = timeout;
				byte[] cmdBytes = _encoding.GetBytes(command);

				CCSendStateObject stateObj = 
					new CCSendStateObject(timeout, cmdBytes, cb, state);
				_socket.BeginSend(cmdBytes, 
					0, 
					cmdBytes.Length, 
					new AsyncCallback(this.OnEndSend), 
					stateObj);
				return stateObj;
			}
			catch(SocketException e)
			{
				SetProgress(false);
				CheckDisposed();
				CheckTimeoutException(e);
				throw;
			}
			catch
			{
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}

		void OnEndSend(IAsyncResult ar)
		{
			CCSendStateObject stateObj = (CCSendStateObject)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				stateObj.Sent += _socket.EndSend(ar);

				int cmdLen = stateObj.Command.Length;
				if(stateObj.Sent < cmdLen) //all data was sent?
				{
					_socket.BeginSend(stateObj.Command, 
						stateObj.Sent, 
						cmdLen - stateObj.Sent,
						new AsyncCallback(this.OnEndSend),
						stateObj);
				}
				else //all data was sent, set completed
				{
					OnCommandSent(((CCSendStateObject)ar.AsyncState).Command);
					stateObj.SetCompleted();
				}
			}
			catch(SocketException e)
			{
				if(_disposed)
					stateObj.Exception = GetDisposedException();
				else if(e.ErrorCode == SockErrors.WSAETIMEDOUT)
					stateObj.Exception = GetTimeoutException(e);
				else
					stateObj.Exception = e;

				stateObj.SetCompleted();
			}
			catch(Exception e)
			{
				if(_disposed)
					stateObj.Exception = GetDisposedException();
				else
					stateObj.Exception = e;
				stateObj.SetCompleted();
			}
			catch
			{
				if(_disposed)
				{
					stateObj.Exception = GetDisposedException();
					stateObj.SetCompleted();
				}
				else
				{
					NSTrace.WriteLineError("Non-CLS exception at: " + Environment.StackTrace);
					throw;
				}
			}
		}

		internal void EndSendCommand(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(CCSendStateObject));
			HandleAsyncEnd(ar, true);
		}
		#endregion
	
		#region SendCommandEx functions
		internal FtpResponse SendCommandEx(int timeout, string command)
		{
			CheckDisposed();

			if(null == command)
				throw new ArgumentNullException("command", "'command' argument cannot be null");

			command = AppendCRLF(command);
			SetProgress(true);
			_response = null;
			try
			{
				_socket.SendTimeout = timeout;
				byte[] cmdBytes = _encoding.GetBytes(command);
				int startPos = 0;
				while(startPos < cmdBytes.Length)
					startPos += _socket.Send(cmdBytes, startPos, cmdBytes.Length - startPos);

				OnCommandSent(command);
				try
				{
					_response = _reader.ReadResponse(timeout);
				}
				catch
				{
					_response = _reader.Response;
					throw;
				}
			}
			catch(SocketException e)
			{
				CheckDisposed();
				CheckTimeoutException(e);
				throw;
			}
			catch
			{
				CheckDisposed();
				throw;
			}
			finally
			{
				SetProgress(false);
			}
			OnResponseReceived();
			return _response;
		}

		internal IAsyncResult BeginSendCommandEx(int timeout, string command, AsyncCallback cb, object state)
		{
			CheckDisposed();

			if(null == command)
				throw new ArgumentNullException("command", "'command' argument cannot be null");

			command = AppendCRLF(command);
			SetProgress(true);
			_response = null;
			try
			{
				_socket.SendTimeout = timeout;
				byte[] cmdBytes = _encoding.GetBytes(command);

				CCSendStateObject stateObj = 
					new CCSendStateObject(timeout, cmdBytes, cb, state);
				_socket.BeginSend(cmdBytes, 
					0, 
					cmdBytes.Length, 
					new AsyncCallback(this.OnEndSendEx), 
					stateObj);
				return stateObj;
			}
			catch(SocketException e)
			{
				SetProgress(false);
				CheckDisposed();
				CheckTimeoutException(e);
				throw;
			}
			catch
			{
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}

		void OnEndSendEx(IAsyncResult ar)
		{
			CCSendStateObject stateObj = (CCSendStateObject)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				stateObj.Sent += _socket.EndSend(ar);

				int cmdLen = stateObj.Command.Length;
				if(stateObj.Sent < cmdLen) //all data was sent?
				{
					_socket.BeginSend(stateObj.Command, 
						stateObj.Sent, 
						cmdLen - stateObj.Sent,
						new AsyncCallback(this.OnEndSend),
						stateObj);
				}
				else //all data was sent, read response
				{
					OnCommandSent(stateObj.Command);
					try
					{
						_reader.BeginReadResponse(stateObj.Timeout, new AsyncCallback(this.OnResponse), stateObj);
					}
					catch(Exception e)
					{
						_response = _reader.Response;
						throw e;
					}
				}
			}
			catch(SocketException e)
			{
				if(_disposed)
					stateObj.Exception = GetDisposedException();
				else if(e.ErrorCode == SockErrors.WSAETIMEDOUT)
					stateObj.Exception = GetTimeoutException(e);
				else
					stateObj.Exception = e;
				stateObj.SetCompleted();
			}
			catch(Exception e)
			{
				if(_disposed)
					stateObj.Exception = GetDisposedException();
				else
					stateObj.Exception = e;
				stateObj.SetCompleted();
			}
			catch
			{
				if(_disposed)
				{
					stateObj.Exception = GetDisposedException();
					stateObj.SetCompleted();
				}
				else
				{
					NSTrace.WriteLineError("Non-CLS exception at: " + Environment.StackTrace);
					throw;
				}
			}
		}

		internal FtpResponse EndSendCommandEx(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(CCSendStateObject));
			HandleAsyncEnd(ar, true);
			return _response;
		}

		#endregion	

		#region ReadResponse functions
		internal FtpResponse ReadResponse(int timeout)
		{
			CheckDisposed();

			SetProgress(true);
			_response = null;
			try
			{
				_response = _reader.ReadResponse(timeout);
			}
			catch(SocketException e)
			{
				CheckDisposed();
				CheckTimeoutException(e);
				throw;
			}
			catch
			{
				CheckDisposed();
				throw;
			}
			finally
			{
				_response = _reader.Response;
				SetProgress(false);
			}
			OnResponseReceived();
			return _response;
		}

		internal IAsyncResult BeginReadResponse(int timeout, 
			AsyncCallback cb, 
			object state)
		{
			CheckDisposed();

			CCReadStateObject stateObj = null;
			SetProgress(true);
			_response = null;
			try
			{
				stateObj = new CCReadStateObject(cb, state);
				_reader.BeginReadResponse(timeout, 
					new AsyncCallback(this.OnResponse), 
					stateObj);
			}
			catch(SocketException e)
			{
				SetProgress(false);
				CheckDisposed();
				CheckTimeoutException(e);
				throw;
			}
			catch
			{
				SetProgress(false);
				CheckDisposed();
				throw;
			}
			finally
			{
				_response = _reader.Response;
			}
			return stateObj;
		}
		
		internal FtpResponse EndReadResponse(IAsyncResult ar)
		{
			try
			{
				AsyncBase.VerifyAsyncResult(ar, typeof(CCReadStateObject));
				HandleAsyncEnd(ar, true);
			}
			catch(SocketException e)
			{
				CheckTimeoutException(e);
				throw;
			}
			return _response;
		}
		#endregion

		#region Disposable pattern
		~FtpControlConnection()
		{
			Dispose(false);
		}

		internal void Close()
		{
			Dispose();
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
				if(!_disposed)
				{
					_disposed = true;
					if(disposing)
					{
					}

					try
					{
						if(null != _socket)
						{
							_socket.Close();
						}

						if(null != _lock)
							_lock.Close();
					}
					catch(Exception)
					{
					}
				}
			}
		}
		#endregion
	}
}
