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
	/// Summary description for Cmd_RunDTP.
	/// </summary>
	internal class Cmd_RunDTP : AsyncBase, IDisposable
	{
		#region AsyncResult class
		class RunDTP_SO : AsyncResultBase
		{
			Cmd_GetDataConnection _getDC_Cmd = null;
			int _timeout = -1;
			string _command = null;
			long _restart = -1;
			bool _manuallyClosed = false;
			DTPStream _userStream = null;
			bool _ccLocked = false;
			FtpDataType _dataType;
			FtpAbortedException _abortEx = null;
			int _prelRespCount = 0;

			internal RunDTP_SO(int timeout, 
				string cmd, 
				FtpDataType dataType,
				long restart, 
				DTPStream userStream, 
				AsyncCallback cb, 
				object state) : base(cb, state)
			{
				_timeout = timeout;
				_command = cmd;
				_userStream = userStream;
				_restart = restart;
				_dataType = dataType;
			}

			internal int PrelRespCount
			{
				get { return _prelRespCount; }
				set { _prelRespCount = value; }
			}

			internal FtpAbortedException AbortEx
			{
				get { return _abortEx; }
				set { _abortEx = value; }
			}

			internal FtpDataType DataType
			{
				get { return _dataType; }
			}

			internal bool CCLocked
			{
				get { return _ccLocked; }
				set { _ccLocked = value; }
			}

			internal bool ManuallyClosed
			{
				get { return _manuallyClosed; }
				set { _manuallyClosed = value; }
			}
			internal long Restart
			{
				get { return _restart; }
			}

			internal string Command
			{
				get { return _command; }
			}

			internal Cmd_GetDataConnection GetDC_Cmd
			{
				get { return _getDC_Cmd; }
				set { _getDC_Cmd = value; }
			}

			internal int Timeout
			{
				get { return _timeout; }
			}

			internal DTPStream UserStream
			{
				get { return _userStream; }
				set { _userStream = value; }
			}
		}
		#endregion

		FtpClient _client = null;
		
		FtpControlConnection _cc = null;
		FtpDataConnection _currentDC = null;

		bool _aborted = false;
		bool _quited = false;
		bool _disposed = false;

		static string _errMsg = "Transfering failed.";
		static string _errMsgNonCls = "Transfering failed (non cls exception catched).";

		internal Cmd_RunDTP(FtpClient ftp)
		{
			_client = ftp;
			_cc = ftp.ControlConnection;
		}

		internal Type ARType
		{
			get { return typeof(RunDTP_SO); }
		}

		internal void Quit()
		{
			_quited = true;
		}

		internal void Abort()
		{
			//----------------------------------------
			//We need to hold this flag, because its
			//determine the behaviour at the last stage
			//(during reading responses after DTP).
			_aborted = true;
			
			//----------------------------------------
			//Abort data-connection manually...
			//We need to do it because not everybody
			//follow RFC. (RFC: server should close
			//data-connection in case of ABORT cmd)
			FtpDataConnection curDC = _currentDC;
			if(null != curDC)
				curDC.Abort();
		}

		//----------------------------------------
		//simply reset data connection
		internal void Reset()
		{
			FtpDataConnection curDC = _currentDC;
			if(null != curDC)
				curDC.Abort();
		}

		#region Events
		void OnDataTransfered(object sender, DataTransferedEventArgs args)
		{
			if(null != DataTransfered)
				DataTransfered(this, args);
		}

		void OnCompleted(object sender, EventArgs args)
		{
			if(null != Completed)
				Completed(this, args);
		}

		internal delegate void DataTransferedEventHandler(object sender, DataTransferedEventArgs e);
		internal event DataTransferedEventHandler DataTransfered;

		internal delegate void CompletedEventHandler(object sender, EventArgs e);
		internal event CompletedEventHandler Completed;
		#endregion

		#region Helpers
		Exception GetRestartNotSuppException(FtpResponse response)
		{
			return new FtpRestartNotSupportedException("Restart command is not supported by the FTP server.", response);
		}

		Exception GetTooManyRespException()
		{
			return new FtpProtocolException("FTP server returns too many preliminary responses.", null, -1, -1);
		}

		string GetTypeCommand(FtpDataType dataType)
		{
			string typeCmd = "TYPE ";
			if(FtpDataType.Ascii == dataType)
				typeCmd += "A";
			else if(FtpDataType.Binary == dataType)
				typeCmd += "I";
			else
			{
				string msg = string.Format("Data type is unsupported ({0}).", dataType.ToString());
				NSTrace.WriteLineError(msg);
				throw new NotSupportedException(msg);
			}
			return typeCmd;
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


		internal void Execute(int timeout, 
			string cmd,
			FtpDataType dataType,
			long restart, 
			DTPStream userStream)
		{
			bool ccLocked = false;

			//protected component against simultaneuous usage
			SetProgress(true);
			try
			{
				//----------------------------------------
				//Lock Control connection to prevent 
				//run the command like abort, quit, stat
				//during configuring of data connection
				ccLocked = _cc.Lock(Timeout.Infinite);

				FtpResponse response = null;
				//----------------------------------------
				//send transfser type command
				if((false == _client.IsDataTypeWasCached) ||
					((true == _client.IsDataTypeWasCached) && (_client.CachedDataType != dataType)))
				{
					string typeCmd = GetTypeCommand(dataType);
					response = _cc.SendCommandEx(timeout, typeCmd);
					FtpClient.CheckCompletionResponse(response);
					
					_client.IsDataTypeWasCached = true;
					_client.CachedDataType = dataType;
				}

				//----------------------------------------
				//Initialize data connection
				_currentDC = null;
				Cmd_GetDataConnection getDC = new Cmd_GetDataConnection(_client);
				lock(this)
				{
					if(!_disposed)
						_currentDC = getDC.Execute(timeout);
				}
				CheckDisposed();

				//----------------------------------------
				//send restart position, if needed
				if(0 < restart)
				{
					string restCmd = "REST " + restart.ToString();
					response = _cc.SendCommandEx(timeout, restCmd);
					if(false == response.IsIntermediateReply)
						throw GetRestartNotSuppException(response);
					CheckDisposed();

				}

				//----------------------------------------
				//establish connection now, if it is outbound
				if(FtpDataConnectionType.Outbound == _currentDC.Type)
				{
					_currentDC.Establish(timeout);
					CheckDisposed();
				}

				//----------------------------------------
				//send transfer request to the server
				response = _cc.SendCommandEx(timeout, cmd);
				CheckDisposed();

				//----------------------------------------
				//first respone should be one 
				//of the 1** - let's check it
				if(response.IsCompletionReply)  //should not happen
				{
					NSTrace.WriteLineError("Executing DTP: receive completion as first reply.");
					//_currentDC.AbortAsyncEstablish();
				}
				else
				{
					FtpClient.CheckPreliminaryResponse(response);

					//----------------------------------------
					//establish connection now, if it is inbound
					if(FtpDataConnectionType.Inbound == _currentDC.Type)
					{
						_currentDC.Establish(timeout);
						CheckDisposed();
					}

					FtpAbortedException abortEx = null;
					try
					{
						//----------------------------------------
						//subscribe to data events
						_currentDC.DataTransfered += new FtpDataConnection.DataTransferedEventHandler(OnDataTransfered);
						_currentDC.Completed +=new FtpDataConnection.CompletedEventHandler(OnCompleted);

						//----------------------------------------
						//prepare for abortion
						_aborted = false;
						_client.CurrentDTP = this;

						//----------------------------------------
						//Unlock control connection, now the command
						//like abort, stat, quit could be issued
						ccLocked = false;
						_cc.Unlock();

						//----------------------------------------
						//start DTP
						_currentDC.RunDTPStream(timeout, userStream);
					}
					catch(FtpAbortedException ex)
					{
						abortEx = ex;
					}
					finally
					{
						_currentDC.DataTransfered -= new FtpDataConnection.DataTransferedEventHandler(OnDataTransfered);
						_currentDC.Completed -= new FtpDataConnection.CompletedEventHandler(OnCompleted);
					}

					//----------------------------------------
					//Lock control connection again - reading
					//responses
					ccLocked = _cc.Lock(Timeout.Infinite);
		
					//----------------------------------------
					//Skip preliminary responses
					//
					for(int i=0;i<10;i++)
					{
						response = _cc.ReadResponse(timeout);
						CheckDisposed();
						if(response.IsPreliminaryReply)
							continue;
						break;
					}

					//----------------------------------------
					//If still receiving priliminary responses
					//then it looks suspecious?
					//
					if(response.IsPreliminaryReply)
						throw GetTooManyRespException();

					//----------------------------------------
					// Dealing with Abort or Reset
					//
					if(!_aborted && (null == abortEx))
					{
						if(!_currentDC.ManuallyClosed)
						{
							FtpClient.CheckCompletionResponse(response);
						}
						else
						{
							//----------------------------------------
							//we DID close the data connection,
							//so, in general, the response should be
							//errorneous - therefore skip checking of it
						}
					}
					else
					{
						if(null != abortEx) //&& !response.IsCompletionReply)
							abortEx.SetResponse(response);

						//----------------------------------------
						//If "ABOR" command was sent we need to
						//one more response...
						//
						if(_aborted)
						{
							response = _cc.ReadResponse(timeout); 
						}
					}

					//------------------------------------------
					//If "QUIT" was sent during data transfer
					//then here we need read one more response
					if(_quited)
					{
						response = _cc.ReadResponse(timeout); 
					}

					if(null != abortEx)
						throw abortEx;
				}
			}
			catch(Exception e)
			{
				CheckDisposed();
				NSTrace.WriteLineError(_errMsg + e.ToString());
				throw;
			}
			catch
			{
				CheckDisposed();
				NSTrace.WriteLineError(_errMsgNonCls + Environment.StackTrace.ToString());
				throw;
			}
			finally
			{
				SetProgress(false);
				_client.CurrentDTP = null;
				
				if(true == ccLocked)
					_cc.Unlock();
			}
		}

		void UnlockCC(RunDTP_SO stateObj)
		{
			if(true == stateObj.CCLocked)
			{
				_cc.Unlock();
				stateObj.CCLocked = false;
			}
		}

		void HandleCatch(Exception e, 
			RunDTP_SO stateObj)
		{
			//----------------------------------------
			//Clear context before unlocking
			_client.CurrentDTP = null;

			//----------------------------------------
			//Unlock control connection, now the command
			//like abort, stat, quit could be issued
			try
			{
				UnlockCC(stateObj);
			}
			catch(Exception ex)
			{
				NSTrace.WriteLineError("UnlockCC exception: " + ex.ToString());
			}
			catch
			{
				NSTrace.WriteLineError("UnlockCC non-cls exception: " + Environment.StackTrace);
			}

			if(_disposed)
			{
				stateObj.Exception = GetDisposedException();
			}
			else if(null != e)
			{
				NSTrace.WriteLineError("DTP exception: " + e.ToString());
				stateObj.Exception = e;
			}
			else
			{
				NSTrace.WriteLineError("DTP non cls exception: " + Environment.StackTrace);
			}

			//Do not set completed in case we
			//have non-cls exception
			if(null != stateObj.Exception)
				stateObj.SetCompleted();
		}

		internal IAsyncResult BeginExecute(int timeout, 
			string command,
			FtpDataType dataType,
			long restart,
			DTPStream userStream,
 			AsyncCallback cb, 
			object state)
		{
			RunDTP_SO stateObj = new RunDTP_SO(timeout, 
				command,
				dataType,
				restart, 
				userStream, 
				cb, 
				state);

			SetProgress(true);
			_currentDC = null;
			try
			{
				//----------------------------------------
				//Lock Control connection to prevent 
				//run the command like abort, quit, stat
				//during configuring of data connection
				_cc.BeginLock(Timeout.Infinite,
					new WaitOrTimerCallback(LockFirst_End),
					stateObj);
			}
			catch(Exception e)
			{
				SetProgress(false);
				CheckDisposed();

				NSTrace.WriteLineError(_errMsg + e.ToString());
				throw;
			}
			catch
			{
				SetProgress(false);
				CheckDisposed();

				NSTrace.WriteLineError(_errMsgNonCls + Environment.StackTrace.ToString());
				throw;
			}
			return stateObj;
		}

		void LockFirst_End(object state, bool timedout)
		{
			RunDTP_SO stateObj = (RunDTP_SO)state;
			try
			{
				stateObj.UpdateContext();
				//----------------------------------------
				//Indicate that we lock CC
				stateObj.CCLocked = true;

				//----------------------------------------
				//send transfser type command
				if((false == _client.IsDataTypeWasCached) ||
					((true == _client.IsDataTypeWasCached) && (_client.CachedDataType != stateObj.DataType)))
				{
					string typeCmd = GetTypeCommand(stateObj.DataType);
					_cc.BeginSendCommandEx(stateObj.Timeout, 
						typeCmd, 
						new AsyncCallback(TypeCmd_End),
						stateObj);
				}
				else
				{
					stateObj.GetDC_Cmd = new Cmd_GetDataConnection(_client);

					//Initialize data connection
					stateObj.GetDC_Cmd.BeginExecute(stateObj.Timeout,
						new AsyncCallback(GetDC_End),
						stateObj);
				}
			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		void TypeCmd_End(IAsyncResult ar)
		{
			RunDTP_SO stateObj = (RunDTP_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				FtpResponse response = _cc.EndSendCommandEx(ar);
				FtpClient.CheckCompletionResponse(response);

				_client.IsDataTypeWasCached = true;
				_client.CachedDataType = stateObj.DataType;
				
				stateObj.GetDC_Cmd = new Cmd_GetDataConnection(_client);

				//Initialize data connection
				stateObj.GetDC_Cmd.BeginExecute(stateObj.Timeout,
					new AsyncCallback(GetDC_End),
					stateObj);

			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		void GetDC_End(IAsyncResult ar)
		{
			RunDTP_SO stateObj = (RunDTP_SO)ar.AsyncState;
			try
			{	
				stateObj.UpdateContext();
				lock(this)
				{
					if(!_disposed)
						_currentDC = stateObj.GetDC_Cmd.EndExecute(ar);
				}
				CheckDisposed();

				//send restart position, if needed
				if(stateObj.Restart > 0)
				{
					string restCmd = "REST " + stateObj.Restart.ToString();
					_cc.BeginSendCommandEx(stateObj.Timeout, 
						restCmd,
						new AsyncCallback(this.Restart_End),
						stateObj);
				}
				else
				{
					//establish connection now, if it is outbound
					if(FtpDataConnectionType.Outbound == _currentDC.Type)
					{
						_currentDC.BeginEstablish(stateObj.Timeout, 
							new AsyncCallback(this.OutboundEstablish_End),
							stateObj);
					}
					else
					{
						_cc.BeginSendCommandEx(stateObj.Timeout, 
							stateObj.Command, 
							new AsyncCallback(this.SendCommandEx_End),
							stateObj);
					}
				}
			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		void Restart_End(IAsyncResult ar)
		{
			RunDTP_SO stateObj = (RunDTP_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				FtpResponse res = _cc.EndSendCommandEx(ar);
				if(false == res.IsIntermediateReply)
					throw GetRestartNotSuppException(res);

				//establish connection now, if it is outbound
				if(FtpDataConnectionType.Outbound == _currentDC.Type)
				{
					_currentDC.BeginEstablish(stateObj.Timeout, 
						new AsyncCallback(this.OutboundEstablish_End),
						stateObj);
				}
				else
				{
					_cc.BeginSendCommandEx(stateObj.Timeout, 
						stateObj.Command, 
						new AsyncCallback(this.SendCommandEx_End),
						stateObj);
				}
			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		void OutboundEstablish_End(IAsyncResult ar)
		{
			RunDTP_SO stateObj = (RunDTP_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				_currentDC.EndEstablish(ar);

				_cc.BeginSendCommandEx(stateObj.Timeout, 
					stateObj.Command, 
					new AsyncCallback(this.SendCommandEx_End),
					stateObj);
			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		void SendCommandEx_End(IAsyncResult ar)
		{
			RunDTP_SO stateObj = (RunDTP_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				FtpResponse response = _cc.EndSendCommandEx(ar);
				if(response.IsCompletionReply)
				{
					NSTrace.WriteLineWarning("Executing DTP: receive completion as first reply.");
					UnlockCC(stateObj);
					stateObj.SetCompleted();
				}
				else
				{
					FtpClient.CheckPreliminaryResponse(response);

					//establish connection now, if it is inbound
					if(FtpDataConnectionType.Inbound == _currentDC.Type)
					{
						_currentDC.BeginEstablish(stateObj.Timeout, 
							new AsyncCallback(this.InboundEstablish_End),
							stateObj);
					}
					else
					{
						DoRunDTP(stateObj);
					}
				}
			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);			
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		void InboundEstablish_End(IAsyncResult ar)
		{
			RunDTP_SO stateObj = (RunDTP_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				_currentDC.EndEstablish(ar);

				DoRunDTP(stateObj);
			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		void DoRunDTP(RunDTP_SO stateObj)
		{
			//now, run the DTP 
			try
			{
				stateObj.UpdateContext();
				//----------------------------------------
				//prepare for abortion
				_aborted = false;
				_client.CurrentDTP = this;

				//----------------------------------------
				//Unlock control connection, now the command
				//like abort, stat, quit could be issued
				UnlockCC(stateObj);

				_currentDC.DataTransfered += new FtpDataConnection.DataTransferedEventHandler(OnDataTransfered);
				_currentDC.Completed += new FtpDataConnection.CompletedEventHandler(OnCompleted);

				_currentDC.BeginRunDTPStream(stateObj.Timeout, 
					stateObj.UserStream, 
					new AsyncCallback(RunDTP_End),
					stateObj);
			}
			catch
			{
				_currentDC.DataTransfered -= new FtpDataConnection.DataTransferedEventHandler(OnDataTransfered);
				_currentDC.Completed -= new FtpDataConnection.CompletedEventHandler(OnCompleted);
				throw;
			}
		}

		void RunDTP_End(IAsyncResult ar)
		{
			RunDTP_SO stateObj = (RunDTP_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				_currentDC.DataTransfered -= new FtpDataConnection.DataTransferedEventHandler(OnDataTransfered);
				_currentDC.Completed -= new FtpDataConnection.CompletedEventHandler(OnCompleted);

				try
				{
					//finish DTP...
					_currentDC.EndRunDTPStream(ar);
					stateObj.ManuallyClosed = _currentDC.ManuallyClosed;
				}
				catch(FtpAbortedException ex)
				{
					stateObj.AbortEx = ex;
				}
				finally
				{
					_currentDC.Dispose();
					_currentDC = null;
				}

				//----------------------------------------
				//Lock control connection again - reading
				//responses
				_cc.BeginLock(Timeout.Infinite,
					new WaitOrTimerCallback(LockLast_End),
					stateObj);
			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		void LockLast_End(object state, bool timedout)
		{
			RunDTP_SO stateObj = (RunDTP_SO)state;
			try
			{
				stateObj.UpdateContext();
				//----------------------------------------
				//CC is locked again
				stateObj.CCLocked = true;
				
				//read all preliminary results, till completion 
				_cc.BeginReadResponse(stateObj.Timeout, 
					new AsyncCallback(this.ReadLastResponse_End),
					stateObj);
			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		void ReadLastResponse_End(IAsyncResult ar)
		{
			RunDTP_SO stateObj = (RunDTP_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				bool needMore = true;
				AsyncCallback callBack = null;

				FtpResponse response = _cc.EndReadResponse(ar);
				if(!response.IsPreliminaryReply)
				{
					needMore = false;

					//----------------------------------------
					// Dealing with Abort or Reset
					//
					if(!_aborted && (null == stateObj.AbortEx))
					{
						if(!stateObj.ManuallyClosed)
						{
							FtpClient.CheckCompletionResponse(response);
						}
						else
						{
							//----------------------------------------
							//we DID close the data connection,
							//so, in general, the response should be
							//errorneous - therefore skip checking of it
						}
					}
					else
					{
						if(null != stateObj.AbortEx)// && 
							//!response.IsCompletionReply)
						{
							stateObj.AbortEx.SetResponse(response);
						}

						stateObj.Exception = stateObj.AbortEx;

						//----------------------------------------
						//If "ABOR" command was sent we need to
						//read one more response ...
						//
						if(_aborted)
						{
							callBack = new AsyncCallback(this.ReadAbortResponse_End);
							needMore = true;
						}
					}

					if(!_aborted && _quited)
					{
						callBack = new AsyncCallback(this.ReadQuitResponse_End);
						needMore = true;
					}
				}
				else
				{
					stateObj.PrelRespCount++;
					if(stateObj.PrelRespCount > 10)
						throw GetTooManyRespException();
				}

				if(true == needMore)
				{
					if(null == callBack)
						callBack = new AsyncCallback(this.ReadLastResponse_End);

					//----------------------------------------
					//read all preliminary results, till enough
					_cc.BeginReadResponse(stateObj.Timeout, 
						callBack,
						stateObj);
				}
				else
				{
					ClearAtEnd(stateObj);
				}
			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		internal void ReadAbortResponse_End(IAsyncResult ar)
		{
			RunDTP_SO stateObj = (RunDTP_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				FtpResponse response = _cc.EndReadResponse(ar);
				if(_quited)
				{
					//----------------------------------------
					// If quited read one more response
					_cc.BeginReadResponse(stateObj.Timeout, 
						new AsyncCallback(ReadQuitResponse_End),
						stateObj);
				}
				else
				{
					ClearAtEnd(stateObj);
				}
			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		internal void ReadQuitResponse_End(IAsyncResult ar)
		{
			RunDTP_SO stateObj = (RunDTP_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				FtpResponse response = _cc.EndReadResponse(ar);
				ClearAtEnd(stateObj);
			}
			catch(Exception e)
			{
				HandleCatch(e, stateObj);
			}
			catch
			{
				HandleCatch(null, stateObj);
			}
		}

		void ClearAtEnd(RunDTP_SO stateObj)
		{
			stateObj.SetCompleted();

			//----------------------------------------
			//Clear context
			_client.CurrentDTP = null;
			_currentDC = null;

			//----------------------------------------
			//Unlock control connection, now the command
			//like abort, stat, quit could be issued
			UnlockCC(stateObj);
		}

		internal void EndExecute(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(RunDTP_SO));
			HandleAsyncEnd(ar, true);
		}

		#region Disposable pattern
		~Cmd_RunDTP()
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
					FtpDataConnection dc = _currentDC;
					if(null != dc)
						dc.Dispose();
				}
				catch(Exception e)
				{
					NSTrace.WriteLineError("DTP.Dispose() ex: " + e.ToString());
				}
				catch
				{
					NSTrace.WriteLineError("DTP.Dispose() non cls ex: " + Environment.StackTrace);
				}
			}
		}

		#endregion
	}
}
