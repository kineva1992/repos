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
using System.IO;

using BytesRoad.Net.Ftp;
using BytesRoad.Net.Sockets;
using BytesRoad.Diag;

namespace BytesRoad.Net.Ftp.Advanced
{
	enum FtpDataConnectionType
	{
		Inbound,
		Outbound
	}

	/// <summary>
	/// Summary description for FtpDataConnection.
	/// </summary>
	internal abstract class FtpDataConnection : AsyncBase, IDisposable
	{
		#region AsyncResultStream classes
		class TransStateObjectStream : AsyncResultBase
		{
			DTPStream _stream = null;
			int _transfered = 0;

			internal TransStateObjectStream(DTPStream stream, AsyncCallback cb, object callerState) : base(cb, callerState)
			{
				_stream = stream;
			}

			internal DTPStream UserStream
			{
				get { return _stream; }
			}

			internal int Transfered
			{
				get { return _transfered; }
				set { _transfered = value; }
			}
		}

		class RunDTPStateObjectStream : AsyncResultBase
		{
			bool _downloading = false;
			internal RunDTPStateObjectStream(bool downloading, AsyncCallback cb, object state) : base(cb, state)
			{
				_downloading = downloading;
			}

			internal bool IsDownloading
			{
				get { return _downloading; }
			}
		}
		
		#endregion

		protected int _bufferSize = 32*1024;
		byte[] _workBuffer = null;
		bool _manuallyClosed = false;
		bool _aborted = false;
		int _wholeTransfered = 0;
		protected bool _disposed = false;

		internal FtpDataConnection()
		{
		}

		internal FtpDataConnection(int bufferSize)
		{
			_bufferSize = bufferSize;
		}

		void FinishTransferingStream(Stream userStream)
		{
			userStream.Flush();
			Shutdown();
		}

		#region Attributes
		internal abstract IPEndPoint LocalEndPoint { get; }
		internal abstract IPEndPoint RemoteEndPoint { get; }

		internal bool ManuallyClosed
		{
			get { return _manuallyClosed; }
		}
		#endregion

		#region Events
		void OnAborted()
		{
			try
			{
				if(null != Aborted)
					Aborted(this, EventArgs.Empty);
			}
			catch(Exception e)
			{
				NSTrace.WriteLineError("Aborted event ex: " + e.ToString());
				throw;
			}
			catch
			{
				NSTrace.WriteLineError("Aborted event non cls ex." + Environment.StackTrace.ToString());
				throw;
			}
		}

		void OnDataTransfered(byte[] data, int size)
		{
			_wholeTransfered += size;
			try
			{
				if(null != DataTransfered)
					DataTransfered(this, 
						new DataTransferedEventArgs(data, size, _wholeTransfered));
			}
			catch(Exception e)
			{
				NSTrace.WriteLineError("DataTransfered event ex: " + e.ToString());
				throw;
			}
			catch
			{
				NSTrace.WriteLineError("DataTransfered event non cls ex." + Environment.StackTrace.ToString());
				throw;
			}
		}

		void OnCompleted()
		{
			try
			{
				if(null != Completed)
					Completed(this, EventArgs.Empty);
			}
			catch(Exception e)
			{
				NSTrace.WriteLineError("Completed event ex: " + e.ToString());
				throw;
			}
			catch
			{
				NSTrace.WriteLineError("Completed event non cls ex." + Environment.StackTrace.ToString());
				throw;
			}
		}


		internal delegate void DataTransferedEventHandler(object sender, DataTransferedEventArgs e);
		internal event DataTransferedEventHandler DataTransfered;

		internal delegate void CompletedEventHandler(object sender, EventArgs e);
		internal event CompletedEventHandler Completed;

		internal delegate void AbortEventHandler(object sender, EventArgs e);
		internal event AbortEventHandler Aborted;
		#endregion

		#region Helpers
		Exception GetTimeoutException(Exception e)
		{
			return new FtpTimeoutException("Transfering was timeouts.", e);
		}

		protected void CheckTimeoutException(SocketException e)
		{
			if(e.ErrorCode == SockErrors.WSAETIMEDOUT)
				throw GetTimeoutException(e);
		}

		Exception GetDisposedException()
		{
			return new ObjectDisposedException(this.GetType().FullName, "Object was disposed.");
		}

		protected void CheckDisposed()
		{
			if(_disposed)
				throw GetDisposedException();
		}
		#endregion

		#region Establish functions
		abstract internal void Establish(int timeout);
		abstract internal IAsyncResult BeginEstablish(int timeout, AsyncCallback cb, object state);
		abstract internal void EndEstablish(IAsyncResult ar);
		#endregion

		#region Prepare functions
		abstract internal void Prepare(int timeout, SocketEx ccSocket);
		abstract internal IAsyncResult BeginPrepare(int timeout, 
			SocketEx ccSocket,
			AsyncCallback callback,
			object state);
		abstract internal void EndPreapre(IAsyncResult ar);
		#endregion

		#region RunDTPStream functions
		
		DTPStreamType ValidateInputStream(DTPStream stream)
		{
			//------------------------------------------
			//Validate input
			if(null == stream)
				throw new ArgumentNullException("steam", "The value cannot be null.");

			DTPStreamType dtpType = stream.Type;
			if((DTPStreamType.ForWriting != dtpType) &&
				(DTPStreamType.ForReading != dtpType))
			{
				string msg =  string.Format("Unknown DTP type ({0})", stream.Type.ToString());
				NSTrace.WriteLineError(msg);
				throw new ArgumentException(msg, "type");
			}
			return dtpType;
		}
		
		internal void RunDTPStream(int timeout, DTPStream stream)
		{
			CheckDisposed();

			//------------------------------------------
			//Validate input
			DTPStreamType dtpType = ValidateInputStream(stream);

			//-------------------------------------------
			//Allocate data buffer 
			_wholeTransfered = 0;
			if(null == _workBuffer)
				_workBuffer = new byte[_bufferSize];

			//--------------------------------------------
			//Prevent simultaneous usage
			SetProgress(true);
			_manuallyClosed = false;
			try
			{
				SetTimeout(timeout);
				if(DTPStreamType.ForWriting == dtpType)
				{
					RunDownloadingStream(stream);
				}
				else //if(DTPStreamType.ForReading == dtpType)
				{
					RunUploadingStream(stream);
				}
			}
			catch(SocketException e)
			{
				if(_aborted)
					throw new FtpAbortedException();
				CheckDisposed();
				CheckTimeoutException(e);
				throw;
			}
			catch
			{
				if(_aborted)
					throw new FtpAbortedException();
				CheckDisposed();
				throw;
				//string msg = "Data transfering was unexpectedly interrupted.";
				//throw new FtpIOException(msg, e);
			}
			finally
			{
				Dispose();
				SetProgress(false);
			}
		}

		internal IAsyncResult BeginRunDTPStream(int timeout, DTPStream stream, AsyncCallback cb, object state)
		{
			CheckDisposed();

			//------------------------------------------
			//Validate input
			DTPStreamType dtpType = ValidateInputStream(stream);

			//-------------------------------------------
			//Allocate data buffer 
			_wholeTransfered = 0;
			if(null == _workBuffer)
				_workBuffer = new byte[_bufferSize];

			//--------------------------------------------
			//Prevent simultaneous usage
			SetProgress(true);

			RunDTPStateObjectStream stateObj = null;
			_manuallyClosed = false;

			try
			{
				SetTimeout(timeout);
				if(DTPStreamType.ForWriting == dtpType)
				{
					stateObj = new RunDTPStateObjectStream(true, cb, state);
					BeginRunDownloadingStream(stream, 
						new AsyncCallback(this.DTPFinishedStream),
						stateObj);
				}
				else //if(DTPStreamType.ForReading == stream.Type)
				{
					stateObj = new RunDTPStateObjectStream(false, cb, state);
					BeginRunUploadingStream(stream,
						new AsyncCallback(this.DTPFinishedStream),
						stateObj);
				}
			}
			catch(SocketException e)
			{
				SetProgress(false);
				if(_aborted)
					throw new FtpAbortedException();
				CheckDisposed();
				CheckTimeoutException(e);
				throw;
			}
			catch
			{
				SetProgress(false);
				if(_aborted)
					throw new FtpAbortedException();
				CheckDisposed();
				throw;
			}
			return stateObj;
		}

		void DTPFinishedStream(IAsyncResult ar)
		{
			RunDTPStateObjectStream stateObj = (RunDTPStateObjectStream)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				if(true == stateObj.IsDownloading)
				{
					EndRunDownloadingStream(ar);
				}
				else
				{
					EndRunUploadingStream(ar);
				}
			}
			catch(SocketException e)
			{
				if(_aborted)
				{
					stateObj.Exception = new FtpAbortedException();
				}
				else if(_disposed)
				{
					stateObj.Exception = GetDisposedException();
				}
				else if(e.ErrorCode == SockErrors.WSAETIMEDOUT)
				{
					stateObj.Exception = GetTimeoutException(e);
				}
				else
				{
					stateObj.Exception = e;
				}
			}
			catch(Exception e)
			{
				if(_aborted)
				{
					stateObj.Exception = new FtpAbortedException();
				}
				else if(_disposed)
				{
					stateObj.Exception = GetDisposedException();
				}
				else
				{
					stateObj.Exception = e;
				}
			}
			catch
			{
				if(_aborted)
				{
					stateObj.Exception = new FtpAbortedException();
				}
				else if(_disposed)
				{
					stateObj.Exception = GetDisposedException();
				}
				else
				{
					NSTrace.WriteLineError("Non-CLS exception at: " + Environment.StackTrace);
					throw;
				}
			}
			stateObj.SetCompleted();
		}

		internal void EndRunDTPStream(IAsyncResult ar)
		{
			try
			{
				AsyncBase.VerifyAsyncResult(ar, typeof(RunDTPStateObjectStream));
				HandleAsyncEnd(ar, true);
			}
			finally
			{
				Dispose();
			}
		}

		
		#endregion

		#region DownloadingStream functions
		void RunDownloadingStream(DTPStream userStream)
		{
			bool needMoreData = true;
			while(true)
			{
				int num = DataStream.Read(_workBuffer, 0, _workBuffer.Length);
				if(num > 0)
				{
					long require = userStream.AvailableSpace;
					if(num > require)
					{
						num = (int)require;
						needMoreData = false;
					}

					OnDataTransfered(_workBuffer, num);
					userStream.Write(_workBuffer, 0, num);
				}

				if((0 == num) || (false == needMoreData))
				{
					//Determine wether we had read enough information.
					//We need to know it, because usually server
					//should close connection by him self,
					//and if we will do this instead, server will
					//return errorneous response later
					if(num > 0)
						_manuallyClosed = true; 

					userStream.Flush();
					OnCompleted();
					break;
				}
				else if(_aborted)
				{
					throw new FtpAbortedException();
				}
			}
		}

		IAsyncResult BeginRunDownloadingStream(DTPStream userStream, AsyncCallback cb, object state)
		{
			TransStateObjectStream stateObj = 
					new TransStateObjectStream(userStream, cb, state);

			DataStream.BeginRead(_workBuffer, 
				0, 
				_workBuffer.Length,
				new AsyncCallback(this.DataStreamRead_End), 
				stateObj);

			return stateObj;
		}

		void DataStreamRead_End(IAsyncResult ar)
		{
			TransStateObjectStream stateObj = (TransStateObjectStream)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				int num = DataStream.EndRead(ar);

				DTPStream userStream = (DTPStream)stateObj.UserStream;
				if(num > 0)
				{
					long require = userStream.AvailableSpace;
					if(num > require)
						num = (int)require;

					stateObj.Transfered += num;
					OnDataTransfered(_workBuffer, num);

					//write received data to user stream
					userStream.BeginWrite(_workBuffer,
						0,
						num,
						new AsyncCallback(this.UserStreamWrite_End),
						stateObj);
				}
				else
				{
					userStream.Flush();
					OnCompleted();
					stateObj.SetCompleted();
				}
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

		void UserStreamWrite_End(IAsyncResult ar)
		{
			TransStateObjectStream stateObj = (TransStateObjectStream)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				DTPStream userStream = (DTPStream)stateObj.UserStream;
				userStream.EndWrite(ar);

				if(_aborted)
				{
					throw new FtpAbortedException();
				}

				if(userStream.AvailableSpace > 0)
				{
					DataStream.BeginRead(_workBuffer, 
						0, 
						_workBuffer.Length,
						new AsyncCallback(this.DataStreamRead_End), 
						stateObj);
				}
				else
				{
					_manuallyClosed = true;

					userStream.Flush();
					OnCompleted();
					stateObj.SetCompleted();
				}
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

		void EndRunDownloadingStream(IAsyncResult ar)
		{
			VerifyAsyncResult(ar, typeof(TransStateObjectStream));
			HandleAsyncEnd(ar, false);
		}
		#endregion

		#region UploadingStream functions
		void RunUploadingStream(DTPStream userStream)
		{
			try
			{
				while(true)
				{
					if(_aborted)
						throw new FtpAbortedException();

					int count = userStream.Read(_workBuffer, 0, _workBuffer.Length);
					if(0 == count)
					{
						OnDataTransfered(null, 0);
						OnCompleted();
						break;
					}

					DataStream.Write(_workBuffer, 0, count);
					OnDataTransfered(null, count);
					if(userStream.AvailableSpace == 0)
					{
						OnCompleted();
						break;
					}
				}
				FinishTransferingStream(DataStream);
			}
			finally
			{
				//we need to dispose here to signal server
				//about the end of data
				Dispose(); 
			}
		}

		IAsyncResult BeginRunUploadingStream(DTPStream userStream, AsyncCallback cb, object state)
		{
			TransStateObjectStream stateObj = new TransStateObjectStream(userStream, cb, state);
			
			userStream.BeginRead(_workBuffer,
				0,
				_workBuffer.Length,
				new AsyncCallback(this.UserStreamRead_End),
				stateObj);

			return stateObj;
		}

		void UserStreamRead_End(IAsyncResult ar)
		{
			TransStateObjectStream stateObj = (TransStateObjectStream)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				int num = stateObj.UserStream.EndRead(ar);
				if(0 == num) //for example coping zero-length file
				{
					FinishTransferingStream(DataStream);
					OnDataTransfered(null, 0);
					OnCompleted();
					stateObj.SetCompleted();
				}
				else
				{
					DataStream.BeginWrite(_workBuffer, 
						0,
						num,
						new AsyncCallback(this.WriteDataStream_End),
						stateObj);
				}
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

		void WriteDataStream_End(IAsyncResult ar)
		{
			TransStateObjectStream stateObj = (TransStateObjectStream)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				DataStream.EndWrite(ar);

				if(_aborted)
				{
					throw new FtpAbortedException();
				}

				if(stateObj.UserStream.AvailableSpace > 0)
				{
					//read and send more data
					stateObj.UserStream.BeginRead(_workBuffer,
						0,
						_workBuffer.Length,
						new AsyncCallback(this.UserStreamRead_End),
						stateObj);
				}
				else
				{
					FinishTransferingStream(DataStream);
					OnCompleted();
					stateObj.SetCompleted();
				}
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

		void EndRunUploadingStream(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(TransStateObjectStream));
			HandleAsyncEnd(ar, false);
		}

		#endregion

		abstract internal FtpDataConnectionType Type { get; }
		abstract protected NetworkStreamEx DataStream {get;}

		abstract protected void Shutdown();
		abstract protected void SetTimeout(int timeout);
		
		virtual internal void Abort()
		{
			OnAborted();

			_aborted = true;
			Dispose();
		}

		#region Disposable pattern
		~FtpDataConnection()
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
		}
		#endregion
	}

	#region Inbound implementation
	internal class FtpDataConnectionInbound : FtpDataConnection
	{
		SocketEx _listenSocket = null;
		SocketEx _socket = null;
		NetworkStreamEx _stream = null;

		internal FtpDataConnectionInbound(FtpClient ftp, IPEndPoint localEP)
		{
			_listenSocket  = ftp.GetSocket();
		}


		#region Prepare functions
		override internal void Prepare(int timeout, SocketEx ccSocket)
		{
			CheckDisposed();
			try
			{
				_listenSocket.ConnectTimeout = timeout; //Bind used ConnectTimeout value
				_listenSocket.Bind(ccSocket);
				_listenSocket.Listen(1);
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
		}

		override internal IAsyncResult BeginPrepare(int timeout,
			SocketEx ccSocket,
			AsyncCallback callback,
			object state)
		{
			CheckDisposed();
			try
			{
				_listenSocket.ConnectTimeout = timeout; //Bind used ConnectTimeout value
				return _listenSocket.BeginBind(ccSocket, callback, state);
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
		}

		override internal void EndPreapre(IAsyncResult ar)
		{
			try
			{
				_listenSocket.EndBind(ar);
				_listenSocket.Listen(1);
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
		}
		#endregion

		#region Attributes
		internal override FtpDataConnectionType Type
		{
			get { return FtpDataConnectionType.Inbound; }
		}

		internal override IPEndPoint LocalEndPoint 
		{ 
			get
			{
				CheckDisposed();
				try
				{
					if(null != _listenSocket)
						return (IPEndPoint)_listenSocket.LocalEndPoint;
					return null;
				}
				catch
				{
					CheckDisposed();
					throw;
				}
			}
		}

		internal override IPEndPoint RemoteEndPoint 
		{ 
			get
			{
				CheckDisposed();
				try
				{
					if(null != _socket)
						return (IPEndPoint)_socket.RemoteEndPoint;
					return null;
				}
				catch
				{
					CheckDisposed();
					throw;
				}
			}
		}

		protected override NetworkStreamEx DataStream 
		{
			get { return _stream; }
		}
		#endregion

		#region Establish functions
		internal override void Establish(int timeout)
		{
			CheckDisposed();
			try
			{
				_listenSocket.AcceptTimeout = timeout;
				_socket = _listenSocket.Accept();
				_stream = new NetworkStreamEx(_socket);
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
		}

		internal override IAsyncResult BeginEstablish(int timeout, AsyncCallback cb, object state)
		{
			CheckDisposed();
			try
			{
				_listenSocket.AcceptTimeout = timeout;
				return _listenSocket.BeginAccept(cb, state);
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
		}

		internal override void EndEstablish(IAsyncResult ar)
		{
			try
			{
				_socket = _listenSocket.EndAccept(ar);
				_stream = new NetworkStreamEx(_socket);
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
		}
		#endregion

		protected override void SetTimeout(int timeout)
		{
			_socket.SendTimeout = timeout;
			_socket.ReceiveTimeout = timeout;
		}

		protected override void Shutdown()
		{
			_socket.Shutdown(SocketShutdown.Both);
		}

		#region Disposable pattern

		protected override void Dispose(bool disposing)
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
						if(null != _listenSocket)
						{
							_listenSocket.Close();
						}

						if(_stream != null)
						{
							_stream.Close();
						}

						if(null != _socket)
						{
							_socket.Close();
						}
					}
					catch
					{
					}
				}
			}
		}

		#endregion
	}

	#endregion

	#region Outbound implementation
	internal class FtpDataConnectionOutbound : FtpDataConnection
	{
		#region Async classes
		class Prepare_SO : AsyncResultBase
		{
			internal Prepare_SO(AsyncCallback cb, object state) : base(cb, state)
			{
			}
		}

		#endregion

		SocketEx _socket = null;
		IPEndPoint _remoteEP = null;
		NetworkStreamEx _stream = null;

		internal FtpDataConnectionOutbound(FtpClient ftp, IPEndPoint remoteEP)
		{
			_remoteEP = remoteEP;
			_socket = ftp.GetSocket();
		}


		#region Attributes
		internal override FtpDataConnectionType Type
		{
			get { return FtpDataConnectionType.Outbound; }
		}

		internal override IPEndPoint LocalEndPoint 
		{ 
			get
			{
				CheckDisposed();
				try
				{
					if(null != _socket)
						return (IPEndPoint)_socket.LocalEndPoint;
					return null;
				}
				catch
				{
					CheckDisposed();
					throw;
				}
			}
		}

		internal override IPEndPoint RemoteEndPoint 
		{ 
			get
			{
				CheckDisposed();
				try
				{
					if(null != _socket)
						return (IPEndPoint)_socket.RemoteEndPoint;
					return null;
				}
				catch
				{
					CheckDisposed();
					throw;
				}
			}
		}

		protected override NetworkStreamEx DataStream 
		{
			get { return _stream; }
		}
		#endregion

		#region Prepare functions
		override internal void Prepare(int timeout, SocketEx ccSocket)
		{
			CheckDisposed();
		}

		override internal IAsyncResult BeginPrepare(int timeout, SocketEx ccSocket,
			AsyncCallback callback,
			object state)
		{
			CheckDisposed();
			try
			{
				Prepare_SO stateObj = new Prepare_SO(callback, state);
				stateObj.SetCompleted();
				return stateObj;
			}
			catch
			{
				CheckDisposed();
				throw;
			}
		}

		override internal void EndPreapre(IAsyncResult ar)
		{
			try
			{
				AsyncBase.VerifyAsyncResult(ar, typeof(Prepare_SO));
				HandleAsyncEnd(ar, false);
			}
			catch
			{
				CheckDisposed();
				throw;
			}
		}
		#endregion

		#region Establish functions
		internal override void Establish(int timeout)
		{
			CheckDisposed();
			try
			{
				_socket.ConnectTimeout = timeout;
				_socket.Connect(_remoteEP);
				_stream = new NetworkStreamEx(_socket);
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
		}

		internal override IAsyncResult BeginEstablish(int timeout, AsyncCallback cb, object state)
		{
			CheckDisposed();
			try
			{
				_socket.ConnectTimeout = timeout;
				return _socket.BeginConnect(_remoteEP, cb, state);
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
		}

		internal override void EndEstablish(IAsyncResult ar)
		{
			try
			{
				_socket.EndConnect(ar);
				_stream = new NetworkStreamEx(_socket);
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
		}
		#endregion

		protected override void Shutdown()
		{
			_socket.Shutdown(SocketShutdown.Both);
		}

		protected override void SetTimeout(int timeout)
		{
			_socket.SendTimeout = timeout;
			_socket.ReceiveTimeout = timeout;
		}


		#region Disposable pattern
		protected override void Dispose(bool disposing)
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
						if(null != _stream)
						{
							_stream.Close();
						}

						if(null != _socket)
						{
							_socket.Close();
						}
					}
					catch
					{
					}
				}
			}
		}
		#endregion
	}
	#endregion
}
