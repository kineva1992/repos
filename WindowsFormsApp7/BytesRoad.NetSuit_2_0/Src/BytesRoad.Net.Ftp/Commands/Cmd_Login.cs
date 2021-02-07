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
	/// Summary description for Cmd_Login.
	/// </summary>
	internal class Cmd_Login : AsyncBase, IDisposable
	{
		#region AsyncResult class
		class Login_SO : AsyncResultBase
		{
			int _timeout;
			string _password;
			string _account;

			internal Login_SO(int timeout, 
				string password, 
				string account, 
				AsyncCallback cb,
				object state) : base(cb, state)
			{
				_timeout = timeout;
				_password = password;
				_account = account;
			}

			internal int Timeout
			{
				get { return _timeout; }
			}

			internal string Password
			{
				get { return _password; }
			}

			internal string Account
			{
				get { return _account; }
			}
		}
		#endregion

		FtpClient _client = null;
		FtpControlConnection _cc = null;

		internal Cmd_Login(FtpClient ftp)
		{
			_client = ftp;
			_cc = ftp.ControlConnection;
		}

		internal Type ARType
		{
			get { return typeof(Login_SO); }
		}

		Exception GetLoginFailedException(FtpResponse response)
		{
			return new FtpErrorException("Unable to login.", response);
		}

		bool RunLoginCmd(int timeout, string cmd, ref FtpResponse response)
		{
			response = _cc.SendCommandEx(timeout, cmd);
			if(!response.IsCompletionReply && 
				!response.IsIntermediateReply)
			{
				throw GetLoginFailedException(response);
			}

			if(response.IsCompletionReply)
				return false; //login successed

			return true; //need more
		}

		internal void Execute(int timeout,
			string user,
			string password,
			string account)
		{
			SetProgress(true);
			try
			{
				FtpResponse response = null;
				if(RunLoginCmd(timeout, "USER " + user, ref response))
					if(RunLoginCmd(timeout, "PASS " + password, ref response))
					{
						if(null == account)
							throw new FtpAccountRequiredException("Unable to login, account is required.", response);

						if(RunLoginCmd(timeout, "ACCT " + account, ref response))
							throw GetLoginFailedException(response);
					}
			}
			finally
			{
				SetProgress(false);
			}
		}

		internal IAsyncResult BeginExecute(int timeout,
			string user,
			string password,
			string account,
			AsyncCallback callback,
			object state)
		{
			Login_SO stateObj = null;
			SetProgress(true);
			try
			{
				stateObj = new Login_SO(timeout,
					password,
					account,
					callback,
					state);

				_cc.BeginSendCommandEx(timeout,
					"USER " + user,
					new AsyncCallback(UserCmd_End),
					stateObj);
			}
			catch(Exception e)
			{
				SetProgress(false);
				throw e;
			}
			return stateObj;
		}

		void UserCmd_End(IAsyncResult ar)
		{
			Login_SO stateObj = (Login_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				FtpResponse response = _cc.EndSendCommandEx(ar);
				if(!response.IsCompletionReply && 
					!response.IsIntermediateReply)
				{
					throw GetLoginFailedException(response);
				}

				if(response.IsCompletionReply)
				{
					stateObj.SetCompleted();
				}
				else
				{
					_cc.BeginSendCommandEx(stateObj.Timeout,
						"PASS " + stateObj.Password,
						new AsyncCallback(PasswordCmd_End),
						stateObj);
				}
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void PasswordCmd_End(IAsyncResult ar)
		{
			Login_SO stateObj = (Login_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				FtpResponse response = _cc.EndSendCommandEx(ar);
				if(!response.IsCompletionReply && 
					!response.IsIntermediateReply)
				{
					throw GetLoginFailedException(response);
				}

				if(response.IsCompletionReply)
				{
					stateObj.SetCompleted();
				}
				else
				{
					if(null == stateObj.Account)
						throw new FtpAccountRequiredException("Unable to login, account is required.", response);

					_cc.BeginSendCommandEx(stateObj.Timeout,
						"ACCT " + stateObj.Account,
						new AsyncCallback(AccountCmd_End),
						stateObj);
				}
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
				stateObj.SetCompleted();
			}
		}

		void AccountCmd_End(IAsyncResult ar)
		{
			Login_SO stateObj = (Login_SO)ar.AsyncState;
			try
			{
				stateObj.UpdateContext();
				FtpResponse response = _cc.EndSendCommandEx(ar);
				if(!response.IsCompletionReply)
				{
					throw GetLoginFailedException(response);
				}
			}
			catch(Exception e)
			{
				stateObj.Exception = e;
			}
			stateObj.SetCompleted();
		}


		internal void EndExecute(IAsyncResult ar)
		{
			AsyncBase.VerifyAsyncResult(ar, typeof(Login_SO));
			HandleAsyncEnd(ar, true);
		}

		#region Disposable pattern
		~Cmd_Login()
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
