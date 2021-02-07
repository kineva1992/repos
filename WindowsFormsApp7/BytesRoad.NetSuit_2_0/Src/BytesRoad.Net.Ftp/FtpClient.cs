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
using System.Collections;
using System.IO;
using System.Threading;
using System.ComponentModel;

using System.Net.Sockets;

using System.Text;
using System.Text.RegularExpressions;

using BytesRoad.Net.Ftp.Advanced;
using BytesRoad.Net.Ftp.Commands;
using BytesRoad.Net.Sockets;

using BytesRoad.Diag;

namespace BytesRoad.Net.Ftp
{
	#region Event's arguments
	/// <summary>
	/// Provides data for the 
	/// <see cref="BytesRoad.Net.Ftp.FtpClient.NewFtpItem">NewFtpItem</see> event.
	/// </summary>
	public class NewFtpItemEventArgs : EventArgs
	{
		FtpItem _item;
		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="BytesRoad.Net.Ftp.NewFtpItemEventArgs">NewFtpItemEventArgs</see>
		/// class using the specified 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see>.
		/// </summary>
		/// 
		/// <param name="item">
		/// An instance that represent 
		/// the currently received ftp item.
		/// </param>
		public NewFtpItemEventArgs(FtpItem item)
		{
			_item = item;
		}

		/// <summary>
		/// Gets an <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see>
		/// that represents the currently received ftp item.
		/// </summary>
		/// <value>
		/// An instance of <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see>
		/// that represents the currently received ftp item.
		/// </value>
		public FtpItem Item
		{
			get { return _item; }
		}
	}


	/// <summary>
	/// Provides data for the 
	/// <see cref="BytesRoad.Net.Ftp.FtpClient.CommandSent">CommandSent</see> event.
	/// </summary>
	public class CommandSentEventArgs : EventArgs
	{
		string _cmd = null;

		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="BytesRoad.Net.Ftp.CommandSentEventArgs">CommandSentEventArgs</see>
		/// class using the specified command.
		/// </summary>
		/// 
		/// <param name="command">
		/// String which represents sent command.
		/// </param>
		public CommandSentEventArgs(string command)
		{
			_cmd = command;
		}

		/// <summary>
		/// Gets string which represents sent command.
		/// </summary>
		public string Command
		{
			get { return _cmd; }
		}
	}

	/// <summary>
	/// Provides data for the 
	/// <see cref="BytesRoad.Net.Ftp.FtpClient.ResponseReceived">ResponseReceived</see>
	/// event.
	/// </summary>
	public class ResponseReceivedEventArgs : EventArgs
	{
		FtpResponse _response = null;

		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="BytesRoad.Net.Ftp.ResponseReceivedEventArgs">ResponseReceivedEventArgs</see>
		/// class using the specified 
		/// <see cref="BytesRoad.Net.Ftp.FtpResponse">FtpResponse</see>.
		/// </summary>
		/// 
		/// <param name="response">
		/// An instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpResponse">FtpResponse</see>
		/// class which represents received response.
		/// </param>
		public ResponseReceivedEventArgs(FtpResponse response)
		{
			_response = response;
		}

		/// <summary>
		/// Gets the response from the FTP server.
		/// </summary>
		/// <value>
		/// An instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpResponse">FtpResponse</see>
		/// class which represents received response.
		/// </value>
		public FtpResponse Response
		{
			get { return _response; }
		}
	}

	/// <summary>
	/// Represent the state of current transfering
	/// operation. Contains the data for 
	/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataTransfered">
	/// DataTransfered</see> event.
	/// </summary>
	public class DataTransferedEventArgs
	{
		byte[] _data = null;
		int _lastTransfered = 0;
		int _wholeTransfered = 0;

		internal DataTransferedEventArgs(byte[] data, 
			int lastTransfered,
			int wholeTransfered)
		{
			_data = data;
			_lastTransfered = lastTransfered;
			_wholeTransfered = wholeTransfered;
		}

		internal byte[] Data
		{
			get { return _data; }
		}

		/// <summary>
		/// Gets number of bytes transfered since the last
		/// notification.
		/// </summary>
		public int LastTransfered
		{
			get { return _lastTransfered; }
		}

		/// <summary>
		/// Gets the number of bytes transfered 
		/// during current file transfering operation.
		/// </summary>
		public int WholeTransfered
		{
			get { return _wholeTransfered; }
		}
	}

	#endregion

	/// <summary>
	/// Specifies the data type an instance of the 
	/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
	/// class used for the file transfers.
	/// </summary>
	/// 
	public enum FtpDataType
	{
		/// <summary>
		/// ASCII type. It is intended primarily for transfer
		/// of text files.
		/// </summary>
		Ascii,

		/// <summary>
		/// Binary type (also known as IMAGE type).
		/// It is intended for the transfer the files
		/// with binary data.
		/// </summary>
		Binary
	}

	/// <summary>
	/// Provides the base interface for implementation
	/// of ftp item resolver.
	/// </summary>
	/// <remarks>
	/// <b>IFtpItemResolver</b> interface is used by
	/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
	/// to convert raw string which represents an ftp item
	/// (file, link, directory) into instance of the
	/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class.
	/// Default implementation of this interface 
	/// can be obtained via 
	/// <see cref="BytesRoad.Net.Ftp.FtpClient.DefaultFtpItemResolver">
	/// FtpClient.DefaultFtpItemResolver</see> property.
	/// </remarks>
	public interface IFtpItemResolver
	{
		/// <summary>
		/// Resolve raw string which represents an ftp item to the instance
		/// of the <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class.
		/// </summary>
		/// <param name="rawString">Raw string which represents an ftp item.</param>
		/// <returns>Instance of 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class.
		/// </returns>
		/// <remarks>
		/// In case the method unable to resolve the string, it has
		/// to return valid instance of the
		/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class,
		/// with 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem.RawString">FtpItem.RawString</see>
		/// property equals to the original raw string and
		/// <see cref="BytesRoad.Net.Ftp.FtpItem.ItemType">FtpItem.ItemType</see>
		/// property set to 
		/// <see cref="BytesRoad.Net.Ftp.FtpItemType.Unresolved">FtpItemType.Unresolved</see>.
		/// </remarks>
		FtpItem Resolve(string rawString);
	}

	/// <summary>
	/// <b>FtpClient</b> is a class that can be used to access
	/// and manipulate resources located at the FTP server. It supports
	/// direct network connection as well as the connection through the 
	/// various proxy servers.
	/// </summary>
	/// <remarks>
	/// The <b>FtpClient</b> class provides a rich set of methods and 
	/// properties for communication with FTP server. All methods
	/// which are involves network communication with FTP server have
	/// synchronous version as well as asynchronous. The <b>FtpClient</b>
	/// class follows the .NET Framework naming pattern for asynchronous
	/// methods. For example, to synchronously download the file from the 
	/// FTP server you may use
	/// <see cref="BytesRoad.Net.Ftp.FtpClient.GetFile">GetFile</see>
	/// method, this method corresponds to the asynchronous 
	/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginGetFile">BeginGetFile</see>
	/// and
	/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndGetFile">EndGetFile</see>
	/// methods.
	/// <para>
	/// By using 
	/// <see cref="BytesRoad.Net.Ftp.FtpClient.ProxyInfo">FtpClient.ProxyInfo</see>
	/// property you may specify proxy server to use for communication 
	/// with FTP server. Following proxy servers are supported:
	/// <list type="bullet">
	/// <item>
	/// <description>Socks4</description>
	/// </item>
	/// <item>
	/// <description>Socks4a</description>
	/// </item>
	/// <item>
	/// <description>Socks5, username/password authentication method supported</description>
	/// </item>
	/// <item>
	/// <description>Web proxy (HTTP CONNECT method), basic authentication method supported</description>
	/// </item>
	/// </list>
	/// </para>
	/// </remarks>
	public class FtpClient : IDisposable
	{
		const long _4GVal = 4294967296L;

		//to which NIC to bind
		IPAddress _localIP = IPAddress.Any;
		Random _rand = new Random(unchecked((int)DateTime.Now.Ticks)); 
		IPHostEntry _localHost = null;

		//server mode
		bool _pasvMode = false;

		//parameter for type command
		FtpDataType _dataType = FtpDataType.Binary;

		//used encoding for any byte to text conversion
		Encoding _encoding = Encoding.Default;

		//item resolver interfaces, resolve string to ftp item
		IFtpItemResolver _itemResolver = FtpItemResolverImpl.Instance;

		//maximum line length
		int _maxLineLength = 512;

		//state variables
		Cmd_RunDTP _currentDTP = null;
		FtpControlConnection _cc = null;
		bool _inProgress = false;

		//current command
		IDisposable _currentCmd = null;

		//flag indicating the the component was disposed
		bool _disposed = false;

		//delegate data transfering event
		bool _issueDataTransEvent = true;

		//proxy attributes
		FtpProxyInfo _proxyInfo = null;

		//DTP state event
		//signaled - there is no DTP
		//non-signaled - DTP is in progress
		ManualResetEvent _dtpEvent = new ManualResetEvent(true);

		/// <summary>
		/// Initializes an empty instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">
		/// FtpClient</see> class.
		/// </summary>
		public FtpClient()
		{
		}


		#region Helper functions
		void DisconnectInternal()
		{
			FtpControlConnection cc = _cc;
			if(null != cc)
			{
				cc.Dispose();
				_cc = null;
			}
		}

		void ThrowIfNegative(string argName, long val)
		{
			if(val < 0)
				throw GetNegativeException(argName);
		}

		void ThrowIfNegative(string argName, int val)
		{
			if(val < 0)
				throw GetNegativeException(argName);
		}

		Exception GetNegativeException(string argName)
		{
			return new ArgumentOutOfRangeException(argName, "The value cannot be negative.");
		}

		void ThrowIfNull(string argName, object val)
		{
			if(null == val)
				throw GetArgNullException(argName);
		}

		Exception GetArgNullException(string argName)
		{
			return new ArgumentNullException(argName, "The value cannot be null.");
		}

		int GetTimeoutValue(int val)
		{
			if(val < 0 && (Timeout.Infinite != val))
				throw new ArgumentOutOfRangeException("timeout", "Timeout value should not be less then zero (exception is only Timeout.Infinite)");

			if(0 == val)
				return Timeout.Infinite;
			else
				return val; 
		}

		void ValidateProxyInfo(FtpProxyInfo info)
		{
			if(null == info)
				return;

			if(null == info.Server)
				throw new ArgumentNullException("FtpProxyInfo.Server", "Value cannot be null.");

			if(info.Port < IPEndPoint.MinPort || 
				info.Port > IPEndPoint.MaxPort)
				throw new ArgumentOutOfRangeException("FtpProxyInfo.Port", "Value is out of valid range.");
		}

		internal SocketEx GetSocket()
		{
			byte[] user = null;
			byte[] password = null;
			string server = null;
			int port = -1;
			bool preAuth = true;

			ProxyType type = ProxyType.None;
			if(null != _proxyInfo)
			{
				user = _proxyInfo.UserBytes;
				password = _proxyInfo.PasswordBytes;

				if(FtpProxyType.Socks4 == _proxyInfo.Type)
					type = ProxyType.Socks4;
				else if(FtpProxyType.Socks4a == _proxyInfo.Type)
					type = ProxyType.Socks4a;
				else if(FtpProxyType.Socks5 == _proxyInfo.Type)
					type = ProxyType.Socks5;
				else if(FtpProxyType.HttpConnect == _proxyInfo.Type)
					type = ProxyType.HttpConnect;

				server = _proxyInfo.Server;
				port = _proxyInfo.Port;
				preAuth = _proxyInfo.PreAuthenticate;
			}

			SocketEx s = new SocketEx(type, 
				server, 
				port, 
				user, 
				password);

			s.PreAuthenticate = preAuth;
			return s;
		}

		Regex _dirNameRegEx = new Regex("^.*\"(?<name>.*)\"[^\"].*$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline);

		string GetNameFor257Response(FtpResponse response)
		{
			if(257 == response.Code)
			{
				//FtpResponseLine line = response.Lines[0];
				//string str = UsedEncoding.GetString(line.Content);
				string str = response.RawStrings[0];
				Match m = _dirNameRegEx.Match(str);
				return m.Groups["name"].Value;
			}
			return null;
		}

		void CheckReadyForCmd()
		{
			CheckDisposed();
			if(null == _cc)
				throw new InvalidOperationException("Connection with server is absent.");
		}

		void CheckDisposed()
		{
			if(true == _disposed)
				throw new ObjectDisposedException("FtpClient");
		}

		internal static void CheckResponse(FtpResponse response)
		{
			if(false == response.IsCompletionReply)
				throw new FtpErrorException("Operation failed.", response);
		}

		internal static void CheckCompletionResponse(FtpResponse response)
		{
			if(false == response.IsCompletionReply)
				throw new FtpErrorException("Operation failed.", response);
		}

		internal static void CheckIntermediateResponse(FtpResponse response)
		{
			if(false == response.IsIntermediateReply)
				throw new FtpErrorException("Operation failed.", response);
		}

		internal static void CheckPreliminaryResponse(FtpResponse response)
		{
			if(false == response.IsPreliminaryReply)
				throw new FtpErrorException("Operation failed.", response);
		}

		string GetResponseText(FtpResponse response)
		{
			//FtpResponseLine line = response.Lines[0];
			//string str = UsedEncoding.GetString(line.Content);
			string str = response.RawStrings[0];
			Regex re = new Regex(@"^\d{3}[- ](?<os>.*)\r\n$");
			Match m = re.Match(str);

			if(2 != m.Groups.Count)
				return "";
			return m.Groups["os"].Value;
		}

		void SetProgress(bool progress)
		{
			//prevent from nested calls
			lock(this)
			{
				if(progress)
				{
					if(true == _inProgress)
						throw new InvalidOperationException("Attempt to start operation while other operation is in progress.");
					_inProgress = true;
				}
				else
				{
					_inProgress = false;
				}
			}
		}
		#endregion

		#region WaitForDTPFinish functions
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


		internal bool WaitForDTPFinish(int timeout)
		{
			return _dtpEvent.WaitOne(timeout, true);
		}

		internal void BeginWaitForDTPFinished(int timeout, 
			WaitOrTimerCallback callback,
			object state)
		{
			ThreadPool.RegisterWaitForSingleObject(_dtpEvent,
				new WaitOrTimerCallback(OnDTPFinished),
				new AsyncLocker(callback, state),
				timeout,
				true);
		}

		void OnDTPFinished(object state, bool timedout)
		{
			AsyncLocker al = (AsyncLocker)state;
			al.Callback(al.State, timedout);
		}
		#endregion

		#region Events
		/// <summary>
		/// Represents the method that handles the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.NewFtpItem">NewFtpItem</see> 
		/// event of an 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>.
		/// </summary>
		public delegate void NewFtpItemEventHandler(object sender, NewFtpItemEventArgs e);

		/// <summary>
		/// Occurs when new ftp item received.
		/// </summary>
		public event NewFtpItemEventHandler NewFtpItem;

		/// <summary>
		/// Represents the method that handles the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.CommandSent">CommandSent</see> 
		/// event of an 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>.
		/// </summary>
		public delegate void CommandSentEventHandler(object sender, CommandSentEventArgs e);

		/// <summary>
		/// Occurs when 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// sends command to the FTP server.
		/// </summary>
		public event CommandSentEventHandler CommandSent;

		/// <summary>
		/// Represents the method that handles the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.ResponseReceived">ResponseReceived</see> 
		/// event of an 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>.
		/// </summary>
		public delegate void ResponseReceivedEventHandler(object sender, ResponseReceivedEventArgs e);

		/// <summary>
		/// Occurs when 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// receive response from the FTP server.
		/// </summary>
		public event ResponseReceivedEventHandler ResponseReceived;

		/// <summary>
		/// Represents the method that handles the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataTransfered">DataTransfered</see> 
		/// event of an 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>.
		/// </summary>
		public delegate void DataTransferedEventHandler(object sender, DataTransferedEventArgs e);

		/// <summary>
		/// Occurs when 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// transfered chunk of data via data channel to/from the FTP server.
		/// </summary>
		public event DataTransferedEventHandler DataTransfered;
		#endregion

		#region Attributes

		/// <summary>
		/// Gets a value indicating whether 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is connected to the remote FTP server.
		/// </summary>
		/// <value>
		/// <b>true</b> if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// was connected to the remote FTP server
		/// as of the most recent operation; otherwise <b>false</b>.
		/// </value>
		/// <remarks>
		/// The value represented by <b>IsConnected</b> property based
		/// on the 
		/// <see cref="System.Net.Sockets.Socket.Connected">Socket.Connected</see>
		/// property value.
		/// </remarks>
		public bool IsConnected
		{
			get 
			{ 
				try
				{
					FtpControlConnection cc = _cc;
					if(null == cc)
						return false;
					return cc.IsConnected;
				}
				catch
				{
					CheckDisposed();
					throw;
				}
			}
		}

		/// <summary>
		/// Gets or sets network proxy information.
		/// </summary>
		/// <value>
		/// An instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpProxyInfo">FtpProxyInfo</see>
		/// class that indicates the network proxy to use for
		/// communication with FTP server. By default proxy is not used
		/// (value of the property is null).
		/// </value>
		/// <remarks>
		/// For the 
		/// <see cref="BytesRoad.Net.Ftp.FtpProxyType.Socks5">Socks5</see>
		/// proxy servers the Username/Password authentication method is supported.
		/// For the 
		/// <see cref="BytesRoad.Net.Ftp.FtpProxyType.HttpConnect">HttpConnect</see>
		/// proxy servers the basic authentication method is supported.
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">
		/// An attempt was made to change the proxy information
		/// during alive connection.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <see cref="BytesRoad.Net.Ftp.FtpProxyInfo.Server">
		/// FtpProxyInfo.Server</see> property value
		/// is null reference (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <see cref="BytesRoad.Net.Ftp.FtpProxyInfo.Port">
		/// FtpProxyInfo.Port</see> property value is less then
		/// <see cref="System.Net.IPEndPoint.MinPort">IPEndPoint.MinPort</see>
		///  or greater then 
		///  <see cref="System.Net.IPEndPoint.MaxPort">IPEndPoint.MaxPort</see>.
		/// </exception>
		public FtpProxyInfo ProxyInfo 
		{
			get
			{
				return _proxyInfo;
			}

			set
			{
				CheckDisposed();
				if(null != _cc)
					throw new InvalidOperationException("Inpossible to change proxy settings during connection.");
				ValidateProxyInfo(value);

				_proxyInfo = value;
			}
		}

		/// <summary>
		/// Gets the default implementation 
		/// for <see cref="BytesRoad.Net.Ftp.IFtpItemResolver"/>
		/// interface.
		/// </summary>
		/// <value>
		/// An <see cref="BytesRoad.Net.Ftp.IFtpItemResolver">
		/// IFtpItemResolver</see> that may be used to resolve
		/// ftp items of commonly used directory styles -
		/// MS-DOS and UNIX styles.
		/// </value>
		static public IFtpItemResolver DefaultFtpItemResolver
		{
			get { return FtpItemResolverImpl.Instance; }
		}

		/// <summary>
		/// Gets or sets the encoding used by the client.
		/// </summary>
		/// <value>
		/// The <see cref="System.Text.Encoding">Encoding</see> instance
		/// used for converting string to bytes array and vice versa. 
		/// Default value is <see cref="System.Text.Encoding.Default">
		/// Encoding.Default</see>.
		/// </value>
		/// <exception cref="System.ArgumentNullException">
		/// UsedEncoding is set to null 
		/// reference (Nothing in Visual Basic).
		/// </exception>
		public Encoding UsedEncoding
		{
			get { return _encoding; }
			set 
			{ 
				if(null == value)
					throw new ArgumentNullException("UsedEncoding");
				_encoding = value;
				if(null != _cc)
					_cc.Encoding = value;
			}
		}

		/// <summary>
		/// Gets or sets mode for FTP server.
		/// </summary>
		/// <value>
		/// This property is of a bool type. Default value 
		/// is false which means that the server is in 
		/// active mode.
		/// </value>
		/// <remarks>
		/// This property affect the file tranfers between client
		/// and FTP server. In case passive mode is off
		/// (false value, default) the server is initiating 
		/// connection for file transfer.
		/// If the passive mode is on (true value), the client 
		/// is initiate the connection. Some firewalls may block
		/// file tranfser if the server is in active mode. By
		/// using this property you may solve such problem - 
		/// set value for the property to true.
		/// </remarks>
		public bool PassiveMode
		{
			get { return _pasvMode; }
			set { _pasvMode = value; }
		}

		/// <summary>
		/// Gets or sets local IP address which would be used
		/// for communication.
		/// </summary>
		/// <value>
		/// The <see cref="System.Net.IPAddress">IPAddress</see> instance
		/// to use for communication with FTP servers. Default value is
		/// <see cref="System.Net.IPAddress.Any">IPAddress.Any</see>.
		/// </value>
		/// <exception cref="System.ArgumentNullException">
		/// IPAddress is set to null 
		/// reference (Nothing in Visual Basic).
		/// </exception>
		/// <remarks>
		/// This property has meaning only in case host machine 
		/// has more then one local IP address.
		/// </remarks>
		public IPAddress LocalIP
		{
			get { return _localIP; }
			set 
			{ 
				if(null == value)
					throw new ArgumentNullException("IPAddress", "Value cannot be null.");
				_localIP = value; 
			}
		}

		/// <summary>
		/// Gets or sets the ftp item resolver object.
		/// </summary>
		/// <value>
		/// The <see cref="BytesRoad.Net.Ftp.IFtpItemResolver">
		/// IFtpItemResolver</see> instance to use for resolving
		/// ftp items. The default value is
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DefaultFtpItemResolver">
		/// FtpClient.DefaultFtpItemResolver</see>.
		/// </value>
		/// <exception cref="System.ArgumentNullException">
		/// FtpItemResolver is set to null 
		/// reference (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <example>
		/// Example below show how to use FtpItemResolver property.
		/// <code>
		/// [C#]
		/// class MyResolver : IFtpItemResolver
		/// {
		///		public MyResolver
		///		{}
		/// 
		///		public FtpItem Resolve(string rawString)
		///		{
		///			//simply use the default implementation
		///			return FtpClient.DefaultFtpItemResolver.Resolve(rawString);
		///		}
		/// }
		/// 
		/// FtpItem[] GetDirectoryList(FtpClient client, string path)
		/// {
		///		int timeout = 5000;
		///		client.FtpItemResolver = new MyResolver();
		///		return client.GetDirectoryList(timeout, path);
		/// }
		/// </code>
		/// </example>
		public IFtpItemResolver FtpItemResolver
		{
			get { return _itemResolver; }
			set 
			{
				if(null == value)
					throw new ArgumentNullException("FtpItemResolver");
				_itemResolver = value; 
			}
		}

		/// <summary>
		/// Gets or sets the data type used for file transfers.
		/// </summary>
		/// <value>One of the <see cref="BytesRoad.Net.Ftp.FtpDataType">FtpDataType</see> values.
		/// The default value is 
		/// <see cref="BytesRoad.Net.Ftp.FtpDataType.Binary">FtpDataType.Binary</see>.
		/// </value>
		public FtpDataType DataType
		{
			get { return _dataType; }
			set { _dataType = value; }
		}

		/// <summary>
		/// Gets or sets the maximum acceptable line length of the server response.
		/// </summary>
		/// <value>
		/// Default value for maximum line length is 512 bytes.
		/// Note that this value should not be lesser then 5 (RFC constraint).
		/// </value>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// Specified value is lesser then 5.
		/// </exception>
		public int MaxLineLength
		{
			get { return _maxLineLength; }
			set 
			{
				if(value < 5)
					throw new ArgumentOutOfRangeException("MaxLineLength", "Maximum line length should not be lesser then 5.");
				_maxLineLength = value;
			}
		}


		//------------------------------
		//This property is used for abort
		//purpose only!
		internal Cmd_RunDTP CurrentDTP
		{
			get { return _currentDTP; }
			set 
			{ 
				if(null != value)
				{
					value.DataTransfered += new Cmd_RunDTP.DataTransferedEventHandler(DTP_DataTransfered);
					_dtpEvent.Reset();
				}
				else if(null != _currentDTP)
					_currentDTP.DataTransfered -= new Cmd_RunDTP.DataTransferedEventHandler(DTP_DataTransfered);

				if(null == value)
					_dtpEvent.Set();

				_currentDTP = value;
			}
		}

		internal FtpControlConnection ControlConnection
		{
			get { return _cc; }
		}

		internal IPHostEntry LocalHostEntry
		{
			get 
			{
				if(null == _localHost)
				{
					string name = Dns.GetHostName();
					_localHost = Dns.GetHostByName(name);
				}
				return _localHost;
			}
		}

		FtpDataType _cachedDataType;
		bool	_dataTypeWasCached = false;

		internal bool IsDataTypeWasCached
		{
			get { return _dataTypeWasCached; }
			set { _dataTypeWasCached = value; }
		}

		internal FtpDataType CachedDataType
		{
			get { return _cachedDataType; }
			set { _cachedDataType = value; }
		}

		#endregion


		#region Connect functions

		/// <summary>
		/// Establish a connection to a remote FTP server.
		/// </summary>
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// <param name="server">
		/// A string containing the DNS name of the FTP server.
		/// </param>
		/// <param name="port">
		/// The port number for the FTP server. 
		/// Usually FTP server is located on port number 21.
		/// </param>
		/// <returns>
		/// An instance of <see cref="BytesRoad.Net.Ftp.FtpResponse">
		/// FtpResponse</see> class which represents the response from
		/// the FTP server after the connection is established.
		/// </returns>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>server</i> is null reference (<b>Nothing</b>
		/// in Visual Basic)
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <i>port</i> is less than <see cref="System.Net.IPEndPoint.MinPort">MinPort</see>. 
		///<para>-or-</para>
		///	<i>port</i> is greater than <see cref="System.Net.IPEndPoint.MaxPort">MaxPort</see>.
		///	<para>-or-</para>
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. See the 
		/// Remarks section for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <remarks>
		/// The <b>Connect</b> method synchronously establishes a 
		/// network connection with specified FTP server. It blocks
		/// until operation is completed or error occurs.
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		public FtpResponse Connect(int timeout, 
			string server, 
			int port)
		{
			CheckDisposed();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("server", server);
			if(port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
				throw new ArgumentOutOfRangeException("port", "Value, specified for the port, is out of valid range."); 

			FtpResponse response = null;
			SetProgress(true);
			try
			{
				FtpControlConnection cc = _cc;
				if(null != cc)
					cc.Close();

				_cc = new FtpControlConnection(this, UsedEncoding, MaxLineLength);
				_cc.CommandSent += new FtpControlConnection.CommandSentEventHandler(CC_CommandSent);
				_cc.ResponseReceived +=new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);

				response = _cc.Connect(timeout, server, port);
				CheckCompletionResponse(response);
			}
			catch
			{
				FtpControlConnection cc = _cc;
				if(null != cc)
				{
					cc.CommandSent -= new FtpControlConnection.CommandSentEventHandler(CC_CommandSent);
					cc.ResponseReceived -= new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);
					cc.Close();
					_cc = null;
				}

				CheckDisposed();
				throw;
			}
			finally
			{
				SetProgress(false);
			}
			return response;
		}

		/// <summary>
		/// Begins an asynchronous request for a remote 
		/// FTP server connection.
		/// </summary>
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// <param name="server">
		/// A string containing the DNS name of the FTP server.
		/// </param>
		/// <param name="port">
		/// The port number for the FTP server. 
		/// Usually FTP server is located on port number 21.
		/// </param>
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this request.
		/// </param>
		/// <returns>
		/// An <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that references the asynchronous connection.
		/// </returns>
		/// <remarks>
		/// The <b>BeginConnect</b> method starts an asynchronous
		/// request for a remote FTP server connection.
		/// It returns immediately and does not wait for 
		/// the asynchronous call to complete.
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndConnect">EndConnect</see>
		/// method is used to retrieve the results of 
		/// the asynchronous call. It can be called 
		/// any time after <b>BeginConnect</b>; if the asynchronous 
		/// call has not completed,
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndConnect">EndConnect</see>
		/// will block until it completes.
		/// </para>
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>server</i> is null reference (<b>Nothing</b>
		/// in Visual Basic)
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <i>port</i> is less than <see cref="System.Net.IPEndPoint.MinPort">MinPort</see>. 
		///<para>-or-</para>
		///	<i>port</i> is greater than <see cref="System.Net.IPEndPoint.MaxPort">MaxPort</see>.
		///	<para>-or-</para>
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// </exception>
		public IAsyncResult BeginConnect(int timeout, 
			string server, 
			int port,
			AsyncCallback callback,
			object state)
		{
			CheckDisposed();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("server", server);
			if(port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
				throw new ArgumentOutOfRangeException("port", "Value, specified for the port, is out of valid range."); 

			SetProgress(true);
			try
			{
				if(null != _cc)
					_cc.Close();

				_cc = new FtpControlConnection(this, UsedEncoding, MaxLineLength);
				_cc.CommandSent += new FtpControlConnection.CommandSentEventHandler(CC_CommandSent);
				_cc.ResponseReceived +=new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);
				return _cc.BeginConnect(timeout, server, port, callback, state);
			}
			catch
			{
				if(null != _cc)
				{
					_cc.CommandSent -= new FtpControlConnection.CommandSentEventHandler(CC_CommandSent);
					_cc.ResponseReceived -= new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);
					_cc.Close();
					_cc = null;
				}
				SetProgress(false);
				throw;
			}
		}

		/// <summary>
		/// Ends a pending asynchronous connection request.
		/// </summary>
		/// <param name="asyncResult">
		/// An <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information 
		/// and any user defined data for this 
		/// asynchronous operation.
		/// </param>
		/// <returns>
		/// An instance of <see cref="BytesRoad.Net.Ftp.FtpResponse">
		/// FtpResponse</see> class which represents the response from
		/// the FTP server after the connection is established.
		/// </returns>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginConnect">BeginConnect</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndConnect</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// Operation was times out. See the Remarks section for more 
		/// information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <remarks>
		/// <b>EndConnect</b> is a blocking method that completes the 
		/// asynchronous remote FTP server connection request 
		/// started in the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginConnect">BeginConnect</see>
		/// method.
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		public FtpResponse EndConnect(IAsyncResult asyncResult)
		{
			FtpResponse response = null;
			try
			{
				response = _cc.EndConnect(asyncResult);
			}
			catch
			{
				FtpControlConnection cc = _cc;
				if(null != cc)
				{
					cc.CommandSent -= new FtpControlConnection.CommandSentEventHandler(CC_CommandSent);
					cc.ResponseReceived -= new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);
					cc.Close();
					_cc = null;
				}

				CheckDisposed();
				throw;
			}
			finally
			{
				SetProgress(false);
			}
			return response;
		}
		#endregion

		#region Login functions
		Cmd_Login _cmdLogin = null;

		/// <overloads>
		/// Login to the FTP server.
		/// </overloads>
		/// <summary>
		/// Login to the FTP server with 
		/// specified user name and password.
		/// </summary>
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="username">
		/// The user name used for login to the FTP server.
		/// </param>
		/// 
		/// <param name="password">
		/// The password used for login to the FTP server.
		/// </param>
		/// 
		/// <remarks>
		/// <para>
		/// The <b>Login</b> method synchronously send 
		/// commands required for login. These commands
		/// are <b>USER</b> and <b>PASS</b>.
		/// If the <b>USER</b> command is enough to succesfully
		/// login to the FTP server then <b>PASS</b> command
		/// will not be sent.
		/// </para>
		/// 
		/// <para>
		/// Some sites may require an account information for login 
		/// and if this is a case the 
		/// <see cref="BytesRoad.Net.Ftp.FtpAccountRequiredException">FtpAccountRequiredException</see>
		/// exception will be thrown. Use other version of 
		/// <b>Login</b> method to login with an account information.
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>username</i> is null reference (<b>Nothing</b>
		/// in Visual Basic).
		/// <para>-or-</para>
		/// <i>password</i> is null reference (<b>Nothing</b>
		/// in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAccountRequiredException">
		/// Account is required to succesfully login to the 
		/// FTP server. See Remarks section for more information.
		/// </exception>
		public void Login(int timeout, 
			string username, 
			string password)
		{
			CheckReadyForCmd();
			ThrowIfNull("username", username);
			ThrowIfNull("password", password);
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdLogin = new Cmd_Login(this);
						_currentCmd = _cmdLogin;
					}
				}
				CheckDisposed();
				_cmdLogin.Execute(timeout, 
					username,
					password,
					null);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdLogin = null;
				SetProgress(false);
				CheckDisposed();
			}
		}

		/// <summary>
		/// Login to the FTP server with 
		/// specified user name, password and account.
		/// </summary>
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="username">
		/// The user name used for login to the FTP server.
		/// </param>
		/// 
		/// <param name="password">
		/// The password used for login to the FTP server.
		/// </param>
		/// 
		/// <param name="account">
		/// The account used for login to the FTP server.
		/// </param>
		/// 
		/// <remarks>
		/// <para>
		/// The <b>Login</b> method synchronously send 
		/// commands required for login. These commands
		/// are <b>USER</b>, <b>PASS</b> and <b>ACCT</b>.
		/// Although the <b>ACCT</b> command is not mandatory,
		/// some FTP servers may require an account for login.
		/// If the FTP server do not require account information
		/// for login you may use other version of <b>Login</b>
		/// method. The <b>Login</b> method will send as much
		/// commands as it needs to succesfully login to the
		/// FTP server, if the <b>USER</b> and <b>PASS</b> commands
		/// are enough then <b>ACCT</b> command
		/// will not be sent.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The
		/// <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>username</i> is null reference (<b>Nothing</b>
		/// in Visual Basic).
		/// <para>-or-</para>
		/// <i>password</i> is null reference (<b>Nothing</b>
		/// in Visual Basic).
		/// <para>-or-</para>
		/// <i>account</i> is null reference (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public void Login(int timeout, 
			string username, 
			string password,
			string account)
		{
			CheckReadyForCmd();
			ThrowIfNull("username", username);
			ThrowIfNull("password", password);
			ThrowIfNull("account", account);
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdLogin = new Cmd_Login(this);
						_currentCmd = _cmdLogin;
					}
				}
				CheckDisposed();
				_cmdLogin.Execute(timeout, 
					username,
					password,
					account);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdLogin = null;
				SetProgress(false);
				CheckDisposed();
			}
		}

		/// <summary>
		/// Begins an asynchronous login with specified
		/// user name, password and optionally an account
		/// information.
		/// </summary>
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="username">
		/// The user name used for login to the FTP server.
		/// </param>
		/// 
		/// <param name="password">
		/// The password used for login to the FTP server.
		/// </param>
		/// 
		/// <param name="account">
		/// The account used for login to the FTP server.
		/// The value of this parameter may be null (<b>Nothing</b>
		/// in Visual Basic)
		/// </param>
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this request.
		/// </param>
		/// <returns>
		/// An <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that references the asynchronous login.
		/// </returns>
		/// 
		/// <remarks>
		/// <para>
		/// The <b>BeginLogin</b> method starts an asynchronous login
		/// operation against the remote FTP server established
		/// in the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Connect">Connect</see>
		/// or 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginConnect">BeginConnect</see> 
		/// method. <b>BeginLogin</b> will
		/// throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if connection with FTP server is absent.
		/// <b>BeginLogin</b> returns immediately
		/// and does not wait for the asynchronous call to complete.
		/// </para>
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndLogin">EndLogin</see>
		/// method is used to retrieve the results of 
		/// the asynchronous call. It can be called 
		/// any time after <b>BeginLogin</b>; if the asynchronous 
		/// call has not completed, <b>EndLogin</b>
		/// will block until it completes.
		/// </para>
		/// <para>
		/// Commands required for login
		/// are <b>USER</b>, <b>PASS</b> and <b>ACCT</b>.
		/// Although the <b>ACCT</b> command is not mandatory,
		/// some FTP servers may require an account for login.
		/// Only required commands will be send to the
		/// FTP server, if the <b>USER</b> and <b>PASS</b> commands
		/// are enough then <b>ACCT</b> command will not be sent.
		/// However if an account information is required for login and
		/// the value of the <i>account</i> parameter is null (<b>Nothing</b>
		/// in Visual Basic) then 
		/// <see cref="BytesRoad.Net.Ftp.FtpAccountRequiredException">FtpAccountRequiredException</see> 
		/// exception will be thrown by the <b>EndLogin</b> method.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// 
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>username</i> is null reference (<b>Nothing</b>
		/// in Visual Basic).
		/// <para>-or-</para>
		/// <i>password</i> is null reference (<b>Nothing</b>
		/// in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginLogin(int timeout,
			string username,
			string password,
			string account,
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			ThrowIfNull("username", username);
			ThrowIfNull("password", password);
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdLogin = new Cmd_Login(this);
						_currentCmd = _cmdLogin;
					}
				}
				CheckDisposed();

				return _cmdLogin.BeginExecute(timeout,
					username,
					password,
					account,
					callback,
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdLogin = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdLogin = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_currentCmd = null;
				_cmdLogin = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}

		/// <summary>
		/// Ends a pending asynchronous login.
		/// </summary>
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// <remarks>
		/// <b>EndLogin</b> is a blocking method that completes the 
		/// asynchronous login operation started in 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginLogin">BeginLogin</see>.
		/// 
		/// <para>
		/// <b>EndLogin</b> will throw an
		/// <see cref="BytesRoad.Net.Ftp.FtpAccountRequiredException">FtpAccountRequiredException</see>
		/// if the FTP server require an account information and
		/// it was not supplied with <b>BeginLogin</b> call.
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The
		/// <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginLogin">BeginLogin</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndLogin</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAccountRequiredException">
		/// Account is required for login.
		/// </exception>
		public void EndLogin(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdLogin.ARType, "EndLogin");
			try
			{
				_cmdLogin.EndExecute(asyncResult);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdLogin = null;
				SetProgress(false);
				CheckDisposed();
			}		
		}
		#endregion

		//service functions
		#region ResolveOS functions
		Cmd_Single _cmdSyst = null;
		/// <summary>
		/// Resolves the type of operating system at the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <returns>
		/// A string containing the name of operating
		/// system at the FTP server. 
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>ResolveOS</b> method synchronously queries 
		/// type of operation system at the FTP server by 
		/// sending <b>SYST</b> command. <b>ResolveOS</b> method
		/// blocks until the query is completed or exception is thrown.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// 
		/// <note>
		/// According RFC 959 (File Transfer Protocol) the
		/// FTP server should return one of the name
		/// listed in the Assigned Numbers document. 
		/// The document is maintained by 
		/// <see href="http://www.iana.org">
		/// Internet Assigned Numbers Authority</see> 
		/// (IANA, http://www.iana.org).
		/// However some FTP servers do not obey specification
		/// and return the name which is not listed in the document.
		/// </note>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public string ResolveOS(int timeout)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
            
			string ret = null;
			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdSyst = new Cmd_Single(this);
						_currentCmd = _cmdSyst;
					}
				}
				CheckDisposed();

				FtpResponse response = _cmdSyst.Execute(timeout, "SYST");
				CheckCompletionResponse(response);
				ret = GetResponseText(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdSyst = null;
				SetProgress(false);
				CheckDisposed();
			}
			return ret;
		}

		/// <summary>
		/// Begins to asynchronously resolving the name 
		/// of operating system at the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this request.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous resolving.
		/// </returns>
		///
		/// <remarks>
		/// The <b>BeginResolveOS</b> method starts asynchronous 
		/// request for the type of operation system at the FTP 
		/// server by sending <b>SYST</b> command. 
		/// <b>BeginResolveOS</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if connection with FTP server is absent. 
		/// <b>BeginResolveOS</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndResolveOS">EndResolveOS</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginResolveOS</b>; if the asynchronous
		/// call has not completed, <b>EndResolveOS</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		///</remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginResolveOS(int timeout, AsyncCallback callback, object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdSyst = new Cmd_Single(this);
						_currentCmd = _cmdSyst;
					}
				}
				CheckDisposed();

				return _cmdSyst.BeginExecute(timeout,
					"SYST",
					callback,
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdSyst = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdSyst = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_currentCmd = null;
				_cmdSyst = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending asynchronous resolving.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <returns>
		/// A string containing the name of operating
		/// system at the FTP server. 
		/// </returns>
		///
		///<remarks>
		/// The <b>EndResolveOS</b>
		/// method completes the asynchronous resolving started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginResolveOS">BeginResolveOS</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <note>
		/// According RFC 959 (File Transfer Protocol) the
		/// FTP server should return one of the name
		/// listed in the Assigned Numbers document. 
		/// The document is maintained by 
		/// <see href="http://www.iana.org">
		/// Internet Assigned Numbers Authority</see> 
		/// (IANA, http://www.iana.org).
		/// However some FTP servers do not obey specification
		/// and return the name which is not listed in the document.
		/// </note>
		///</remarks>
		///
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginResolveOS">BeginResolveOS</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndResolveOS</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <seealso cref="BytesRoad.Net.Ftp.FtpClient.BeginResolveOS">BeginResolveOS</seealso>
		/// <seealso cref="BytesRoad.Net.Ftp.FtpClient.ResolveOS">ResolveOS</seealso>
		public string EndResolveOS(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdSyst.ARType, "EndResolveOS");
			string ret = null;
			try
			{
				FtpResponse response = _cmdSyst.EndExecute(asyncResult);
				CheckCompletionResponse(response);
				ret = GetResponseText(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdSyst = null;
				SetProgress(false);
				CheckDisposed();
			}
			return ret;
		}
		#endregion

		#region Ping functions
		Cmd_Single _cmdPing = null;
		/// <summary>
		/// Ping the FTP server.
		/// </summary>
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>Ping</b> method synchronously send 
		/// <b>NOOP</b> command to the FTP server. 
		/// <b>Ping</b> method blocks until the operation is
		/// completed or exception is thrown.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public void Ping(int timeout)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdPing = new Cmd_Single(this);
						_currentCmd = _cmdPing;
					}
				}
				CheckDisposed();

				FtpResponse response = _cmdPing.Execute(timeout, "NOOP");
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdPing = null;
				SetProgress(false);
				CheckDisposed();
			}
		}


		/// <summary>
		/// Begins to asynchronously ping the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this request.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous resolving.
		/// </returns>
		///
		/// <remarks>
		/// The <b>BeginPing</b> method starts an asynchronous 
		/// ping operation by sending <b>NOOP</b> command. 
		/// <b>BeginPing</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if connection with FTP server is absent. 
		/// <b>BeginPing</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndPing">EndPing</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginPing</b>; if the asynchronous
		/// call has not completed, <b>EndPing</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		///</remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginPing(int timeout, AsyncCallback callback, object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdPing = new Cmd_Single(this);
						_currentCmd = _cmdPing;
					}
				}
				CheckDisposed();

				return _cmdPing.BeginExecute(timeout,
					"NOOP",
					callback,
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdPing = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdPing = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_currentCmd = null;
				_cmdPing = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}

		/// <summary>
		/// Ends a pending asynchronous ping.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		///<remarks>
		/// The <b>EndPing</b>
		/// method completes the asynchronous ping started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginPing">BeginPing</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		///</remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginPing">BeginPing</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndPing</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public void EndPing(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdPing.ARType, "EndPing");
			try
			{
				_cmdPing.EndExecute(asyncResult);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdPing = null;
				SetProgress(false);
				CheckDisposed();
			}		
		}
		#endregion

		#region GetDirectoryList functions

		Cmd_GetDirectoryList _getDirectoryListCmd = null;

		#region Sinchro part
		/// <overloads>
		/// Gets the content of the directory.
		/// </overloads>
		/// <summary>
		/// Get the contents of the current working
		/// directory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <returns>
		/// Array of <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see>
		/// instances which
		/// describes the content of the current working directory.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>GetDirectoryList</b> method synchronously retrieve
		/// content of the current working directory.
		/// The FTP's command
		/// used for that is <b>LIST</b>. <b>GetDirectoryList</b> method 
		/// blocks until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// As the answer to the <b>LIST</b> command FTP server
		/// will send the content of the directory via the data channel.
		/// The data channel automatically configured in ascii mode.
		/// Format of the data which describes the content of the
		/// directory is not specified anywhere and different FTP
		/// servers may use different format. The <b>GetDirectoryList</b>
		/// method understand commonly used formats. But if the 
		/// format is unknown then each returned instance of the <b>FtpItem</b>
		/// class would have the value of 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem.ItemType">FtpItem.ItemType</see>
		/// property equal to 
		/// <see cref="BytesRoad.Net.Ftp.FtpItemType.Unresolved">FtpItemType.Unresolved</see>.
		/// You may extend algorithm used for
		/// processing incoming data
		/// by setting up custom ftp item's resolver via
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.FtpItemResolver">FtpItemResolver</see>
		/// property.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <event cref="BytesRoad.Net.Ftp.FtpClient.NewFtpItem">
		/// Occurs when one more <b>FtpItem</b> received.
		/// </event>
		public FtpItem[] GetDirectoryList(int timeout)
		{
			return GetDirectoryList(timeout, (string)null);
		}

		/// <summary>
		/// Gets the content of the specified directory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Pathname of the directory which contents to be
		/// retrieved. If the value is null (<b>Nothing</b> 
		/// in Visual Basic) then the contents of the
		/// current working directory will be retrieved.
		/// </param>
		/// 
		/// <returns>
		/// Array of <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see>
		/// instances which
		/// describes the content of the specified directory.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>GetDirectoryList</b> method synchronously retrieve
		/// content of the specified directory.
		/// The FTP's command
		/// used for that is <b>LIST</b>. 
		/// 
		/// <b>GetDirectoryList</b> method 
		/// blocks until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// As the answer to the <b>LIST</b> command FTP server
		/// will send the content of the directory via the data channel.
		/// The data channel automatically configured in ascii mode.
		/// Format of the data which describes the content of the
		/// directory is not specified anywhere and different FTP
		/// servers may use different format. The <b>GetDirectoryList</b>
		/// method understand commonly used formats. But if the 
		/// format is unknown then each returned instance of the <b>FtpItem</b>
		/// class would have the value of 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem.ItemType">FtpItem.ItemType</see>
		/// property equal to 
		/// <see cref="BytesRoad.Net.Ftp.FtpItemType.Unresolved">FtpItemType.Unresolved</see>.
		/// You may extend algorithm used for
		/// processing incoming data
		/// by setting up custom ftp item's resolver via
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.FtpItemResolver">FtpItemResolver</see>
		/// property.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <event cref="BytesRoad.Net.Ftp.FtpClient.NewFtpItem">
		/// Occurs when one more <b>FtpItem</b> received.
		/// </event>

		public FtpItem[] GetDirectoryList(int timeout, string path)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			FtpItem[] items = null;
			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_getDirectoryListCmd = new Cmd_GetDirectoryList(this);
						_currentCmd = _getDirectoryListCmd;
					}
				}
				CheckDisposed();

				_issueDataTransEvent = false;
				_getDirectoryListCmd.NewFtpItem += new Cmd_GetDirectoryList.NewFtpItemEventHandler(OnNewFtpItem);
				items = _getDirectoryListCmd.Execute(timeout, path);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_getDirectoryListCmd.NewFtpItem -= new Cmd_GetDirectoryList.NewFtpItemEventHandler(OnNewFtpItem);
				_issueDataTransEvent = true;
				_currentCmd = null;
				_getDirectoryListCmd = null;
				SetProgress(false);
				CheckDisposed();
			}
			return items;
		}
		#endregion

		#region Async part

		/// <summary>
		/// Begins an asynchronous retrieve
		/// the content of the specified directory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Pathname of the directory which contents to be
		/// retrieved. If the value is null (<b>Nothing</b> 
		/// in Visual Basic) then the contents of the
		/// current working directory will be retrieved.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginGetDirectoryList</b> method starts an asynchronous 
		/// retrieve
		/// content of the specified directory at the FTP server. 
		/// The FTP's command used 
		/// for that is <b>LIST</b>.
		/// <b>BeginGetDirectoryList</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginGetDirectoryList</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndGetDirectoryList">EndGetDirectoryList</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginGetDirectoryList</b>; if the asynchronous
		/// call has not completed, <b>EndGetDirectoryList</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// As the answer to the <b>LIST</b> command FTP server
		/// will send the content of the directory via the data channel.
		/// The data channel automatically configured in ascii mode.
		/// Format of the data which describes the content of the
		/// directory is not specified anywhere and different FTP
		/// servers may use different format. If the 
		/// format is unknown then each returned instance of the <b>FtpItem</b>
		/// class would have the value of 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem.ItemType">FtpItem.ItemType</see>
		/// property equal to 
		/// <see cref="BytesRoad.Net.Ftp.FtpItemType.Unresolved">FtpItemType.Unresolved</see>.
		/// You may extend algorithm used for
		/// processing incoming data
		/// by setting up custom ftp item's resolver via
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.FtpItemResolver">FtpItemResolver</see>
		/// property.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <event cref="BytesRoad.Net.Ftp.FtpClient.NewFtpItem">
		/// Occurs when one more <b>FtpItem</b> received.
		/// </event>

		public IAsyncResult BeginGetDirectoryList(int timeout, 
			string path, 
			AsyncCallback callback, 
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_getDirectoryListCmd = new Cmd_GetDirectoryList(this);
						_currentCmd = _getDirectoryListCmd;
					}
				}
				CheckDisposed();
				_issueDataTransEvent = false;
				_getDirectoryListCmd.NewFtpItem += 
					new Cmd_GetDirectoryList.NewFtpItemEventHandler(OnNewFtpItem);
				return _getDirectoryListCmd.BeginExecute(timeout, 
					path, callback, state);
			}
			catch(FtpFatalErrorException)
			{
				_getDirectoryListCmd.NewFtpItem -= 
					new Cmd_GetDirectoryList.NewFtpItemEventHandler(OnNewFtpItem);
				_issueDataTransEvent = true;
				_currentCmd = null;
				_getDirectoryListCmd = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_getDirectoryListCmd.NewFtpItem -= 
					new Cmd_GetDirectoryList.NewFtpItemEventHandler(OnNewFtpItem);
				_issueDataTransEvent = true;
				_currentCmd = null;
				_getDirectoryListCmd = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_getDirectoryListCmd.NewFtpItem -= 
					new Cmd_GetDirectoryList.NewFtpItemEventHandler(OnNewFtpItem);
				_issueDataTransEvent = true;
				_currentCmd = null;
				_getDirectoryListCmd = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending asynchronous retrieve content
		/// of the specified directory.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <returns>
		/// Array of <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see>
		/// instances which
		/// describes the content of the specified directory.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>EndGetDirectoryList</b>
		/// method completes the asynchronous retrieve content of the
		/// directory command, 
		/// started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginGetDirectoryList">BeginGetDirectoryList</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginGetDirectoryList">BeginGetDirectoryList</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndGetDirectoryList</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>		
		public FtpItem[] EndGetDirectoryList(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _getDirectoryListCmd.ARType, "EndGetDirectoryList");
			FtpItem[] items = null;
			try
			{
				items = _getDirectoryListCmd.EndExecute(asyncResult);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_getDirectoryListCmd.NewFtpItem -= 
					new Cmd_GetDirectoryList.NewFtpItemEventHandler(OnNewFtpItem);
				_issueDataTransEvent = true;
				_currentCmd = null;
				_getDirectoryListCmd = null;
				SetProgress(false);
				CheckDisposed();
			}
			return items;
		}
		#endregion
		
		#endregion

		#region SendCommand
		Cmd_Single _cmdSendCommand = null;

		/// <summary>
		/// Sends user-defined command to the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="command">
		/// A string that contains a user-defined command to be sent.
		/// </param>
		/// 
		/// <returns>
		/// An instance of <see cref="BytesRoad.Net.Ftp.FtpResponse">
		/// FtpResponse</see> class which represents the response from
		/// the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>SendCommand</b> method synchronously send 
		/// user-defined command to the FTP server. 
		/// <b>SendCommand</b> method blocks until the operation is
		/// completed or exception is thrown. This method is 
		/// implemented because some FTP server may extend
		/// base functionality by introducing new commands, not
		/// defined in the RFC 959 (File Transfer Protocol).
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>command</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public FtpResponse SendCommand(int timeout, string command)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("command", command);

			FtpResponse response = null;
			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdSendCommand = new Cmd_Single(this);
						_currentCmd = _cmdSendCommand;
					}
				}

				CheckDisposed();
				response = _cmdSendCommand.Execute(timeout, command);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdSendCommand = null;
				SetProgress(false);
				CheckDisposed();
			}
			return response;
		}

	
		/// <summary>
		/// Sends user-defined command asynchronously to the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="command">
		/// A string that contains a user-defined command to be sent.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this request.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// <remarks>
		/// The <b>BeginSendCommand</b> method starts an asynchronous 
		/// send user-defined command operation. This method 
		/// is implemented because some FTP server may extends base
		/// functionality by introducing new commands, not
		/// defined in the RFC 959 (File Transfer Protocol).
		/// <b>BeginSendCommand</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if connection with FTP server is absent. 
		/// <b>SendCommand</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndSendCommand">EndSendCommand</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginSendCommand</b>; if the asynchronous
		/// call has not completed, <b>EndSendCommand</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>command</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginSendCommand(int timeout, 
			string command,
			AsyncCallback callback, 
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("command", command);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdSendCommand = new Cmd_Single(this);
						_currentCmd = _cmdSendCommand;
					}
				}
				CheckDisposed();

				return _cmdSendCommand.BeginExecute(timeout,
					command,
					callback,
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdSendCommand = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdSendCommand = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_currentCmd = null;
				_cmdSendCommand = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending asynchronous send user-defined command.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <returns>
		/// An instance of 
		/// <see cref="BytesRoad.Net.Ftp.FtpResponse">FtpResponse</see>
		/// class which represents the response from the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>EndSendCommand</b>
		/// method completes the asynchronous send user-defined
		/// command started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginSendCommand">BeginSendCommand</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginSendCommand">BeginSendCommand</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndSendCommand</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public FtpResponse EndSendCommand(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdSendCommand.ARType, "EndSendCommand");
			FtpResponse response = null;
			try
			{
				response = _cmdSendCommand.EndExecute(asyncResult);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdSendCommand = null;
				SetProgress(false);
				CheckDisposed();
			}	
			return response;
		}

		#endregion

		//files functions
		#region GetFile functions

		Cmd_GetFile _cmdGetFile = null;

		#region Downloading to memory

		/// <overloads>
		/// Download file from the FTP server.
		/// </overloads>
		/// <summary>
		/// Download file into memory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// A string that contains path of the file to download.
		/// </param>
		/// 
		/// <returns>
		/// Array of bytes which contains the content of the file.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>GetFile</b> method synchronously download the
		/// file from the FTP server into memory. The data channel
		/// used for downloading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>GetFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Downloading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public byte[] GetFile(int timeout, string path)
		{
			return GetFile(timeout, path, 0, _4GVal);
		}

		/// <summary>
		/// Download part of the file into memory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// A string that contains path of the file to download.
		/// </param>
		/// 
		/// <param name="offset">
		/// Offset from beginning of the file where the 
		/// downloads should start. Value cannot be negative.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to download.
		/// </param>
		/// 
		/// <returns>
		/// Array of bytes which contains the specified part of the file.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>GetFile</b> method synchronously download the
		/// part of the
		/// file from the FTP server into memory. Note, that
		/// it is not possible to download files into memory 
		/// with the size more then 4Gb. Use other versions
		/// of the <b>GetFile</b> method for this purpose.
		/// The data channel
		/// used for downloading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>GetFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// Note that not all FTP servers support downloading from the
		/// middle of the file. If this is a case the 
		/// <see cref="BytesRoad.Net.Ftp.FtpRestartNotSupportedException">FtpRestartNotSupportedException</see>
		/// exception will be thrown.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// <para>-or-</para>
		/// <i>length</i> is more then 4294967296 (4Gb).
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Downloading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpRestartNotSupportedException">
		/// The FTP server do not support downloading from the middle
		/// of the file.
		/// </exception>
		public byte[] GetFile(int timeout, 
			string path, long offset, long length)
		{
			CheckReadyForCmd();
			ThrowIfNull("path", path);
			ThrowIfNegative("offset", offset);
			ThrowIfNegative("length", length);
			if(length > _4GVal)
				throw new ArgumentOutOfRangeException("length", "Value cannot be more then 4294967296.");

			byte[] retData = null;
			MemoryStream ms = new MemoryStream();
			try
			{
				GetFile(timeout, ms, path, offset, length);

				long maxInt = (long)int.MaxValue;
				long dataSize = ms.Length;
				retData = new byte[dataSize];

				//handle the big buffer case
				if(dataSize > maxInt)
				{
					ms.Seek(0, SeekOrigin.Begin);
					ms.Read(retData, 0, (int)maxInt);
					ms.Seek(maxInt, SeekOrigin.Begin);
					ms.Read(retData, (int)maxInt, (int)(dataSize - maxInt));
				}
				else
				{
					ms.Seek(0, SeekOrigin.Begin);
					ms.Read(retData, 0, (int)dataSize);
				}
			}
			finally
			{
				ms.Close(); //is GC not enough?
			}
			return retData;
		}

		#endregion

		#region Downloading to file

		/// <summary>
		/// Download file to the specified location.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// A string that contains path of the file to download.
		/// </param>
		/// 
		/// <param name="destPath">
		/// The name of the destination file.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>GetFile</b> method synchronously download the
		/// file from the FTP server to the specified destination file.
		/// The data channel, used for downloading, configured either in 
		/// ascii or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>GetFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The following code is used to open the file:
		/// <code>FileStream fs = File.Create(destPath);</code>
		/// If the value of <i>destPath</i> parameter is incorrect and
		/// <see cref="System.IO.File.Create">File.Create</see> method will 
		/// throw an exception then <b>GetFile</b> method will rethrow it.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>destPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Downloading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void GetFile(int timeout, 
			string destPath, string path)
		{
			GetFile(timeout, destPath, path, 0, long.MaxValue);
		}

		/// <summary>
		/// Download part of the file to the specified location.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// A string that contains path of the file to download.
		/// </param>
		/// 
		/// <param name="offset">
		/// Offset from beginning of the file where the 
		/// downloads should start. The value cannot be negative.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to download.
		/// </param>
		/// 
		/// <param name="destPath">
		/// The name of the destination file.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>GetFile</b> method synchronously download the
		/// file from the FTP server to the specified destination file.
		/// The data channel
		/// used for downloading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>GetFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// Note that not all FTP servers support downloading from the
		/// middle of the file. If this is a case the 
		/// <see cref="BytesRoad.Net.Ftp.FtpRestartNotSupportedException">FtpRestartNotSupportedException</see>
		/// exception will be thrown.
		/// </para>
		/// 
		/// <para>
		/// The following code is used to open the file:
		/// <code>FileStream fs = File.Create(destPath);</code>
		/// If the value of <i>destPath</i> parameter is incorrect and
		/// <see cref="System.IO.File.Create">File.Create</see> method will 
		/// throw an exception then <b>GetFile</b> method will rethrow it.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>destPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Downloading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpRestartNotSupportedException">
		/// The FTP server do not support downloading from the middle
		/// of the file.
		/// </exception>
		public void GetFile(int timeout, string destPath, 
			string path, long offset, long length)
		{
			CheckReadyForCmd();
			ThrowIfNull("path", path);
			ThrowIfNull("destPath", destPath);
			ThrowIfNegative("offset", offset);
			ThrowIfNegative("length", length);

			FileStream fs = File.Create(destPath);
			try
			{
				GetFile(timeout, fs, path, offset, length);
			}
			finally
			{
				fs.Close();
			}
		}
		#endregion

		#region Downloading to stream


		/// 
		/// <summary>
		/// Download file to the stream specified.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// A string that contains path of the file to download.
		/// </param>
		/// 
		/// <param name="userStream">
		/// The stream to which the downloading data will written.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>GetFile</b> method synchronously download the
		/// file from the FTP server to the specified stream. Received 
		/// data synchronously written to the stream as it arrives.
		/// The data channel
		/// used for downloading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>GetFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Downloading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void GetFile(int timeout, Stream userStream,
			string path)
		{
			GetFile(timeout, userStream, path, 0, long.MaxValue);
		}

		/// <summary>
		/// Download part of the file to the stream specified.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// A string that contains path of the file to download.
		/// </param>
		/// 
		/// <param name="offset">
		/// Offset from beginning of the file where the 
		/// downloads should start. The value cannot be negtive.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to download.
		/// </param>
		/// 
		/// <param name="userStream">
		/// The stream to which the downloading data will written.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>GetFile</b> method synchronously download the part of the
		/// file from the FTP server to specified stream. Received 
		/// data synchronously written to the stream as it arrives.
		/// The data channel
		/// used for downloading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>GetFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// Note that not all FTP servers support downloading from the
		/// middle of the file. If this is a case the 
		/// <see cref="BytesRoad.Net.Ftp.FtpRestartNotSupportedException">FtpRestartNotSupportedException</see>
		/// exception will be thrown.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Downloading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpRestartNotSupportedException">
		/// The FTP server do not support downloading from the middle
		/// of the file.
		/// </exception>
		public void GetFile(int timeout, Stream userStream,
			string path, long offset, long length)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);
			ThrowIfNull("userStream", userStream);
			ThrowIfNegative("offset", offset);
			ThrowIfNegative("length", length);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdGetFile = new Cmd_GetFile(this);
						_currentCmd = _cmdGetFile;
					}
				}
				CheckDisposed();

				_cmdGetFile.Execute(timeout, 
					userStream, 
					path, 
					offset,
					length);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_cmdGetFile.Dispose();
				_cmdGetFile = null;
				_currentCmd = null;
				SetProgress(false);
				CheckDisposed();
			}
		}
		#endregion

		/// <summary>
		/// Begins an asynchronous download of the file.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// A string that contains path of the file to download.
		/// </param>
		/// 
		/// <param name="offset">
		/// Position from beginning of the file where the 
		/// downloads should start. The value cannot be negative.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to download. 
		/// </param>
		/// 
		/// <param name="userStream">
		/// The stream to which the downloading data will written.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginGetFile</b> method starts an asynchronous 
		/// download the specified part of the
		/// file from the FTP server to the stream. Received 
		/// data asynchronously written to the stream as it arrives.
		/// The data channel, used for downloading, configured either in
		/// ascii or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>BeginGetFile</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginGetFile</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndGetFile">EndGetFile</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginGetFile</b>; if the asynchronous
		/// call has not completed, <b>EndGetFile</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that not all FTP servers support downloading from the
		/// middle of the file. If this is a case the 
		/// <see cref="BytesRoad.Net.Ftp.FtpRestartNotSupportedException">FtpRestartNotSupportedException</see>
		/// exception will be thrown by the <b>EndGetFile</b> method.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// 
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginGetFile(int timeout, 
			Stream userStream,
			string path, 
			long offset, 
			long length, 
			AsyncCallback callback, 
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);
			ThrowIfNull("userStream", userStream);
			ThrowIfNegative("offset", offset);
			ThrowIfNegative("length", length);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdGetFile = new Cmd_GetFile(this);
						_currentCmd = _cmdGetFile;
					}
				}
				CheckDisposed();

				return _cmdGetFile.BeginExecute(timeout,
					userStream, 
					path,
					offset, 
					length,
					callback, 
					state);
			}
			catch(FtpFatalErrorException)
			{
				_cmdGetFile.Dispose();
				_cmdGetFile = null;
				_currentCmd = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_cmdGetFile.Dispose();
				_cmdGetFile = null;
				_currentCmd = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_cmdGetFile.Dispose();
				_cmdGetFile = null;
				_currentCmd = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}

		
		/// <summary>
		/// Ends a pending asynchronous download of the file.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>EndGetFile</b>
		/// method completes the asynchronous download operation started
		/// in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginGetFile">BeginGetFile</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginGetFile">BeginGetFile</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndGetFile</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Downloading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpRestartNotSupportedException">
		/// The FTP server do not support downloading from the middle
		/// of the file.
		/// </exception>
		public void EndGetFile(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdGetFile.ARType, "EndGetFile");
			try
			{
				_cmdGetFile.EndExecute(asyncResult);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_cmdGetFile.Dispose();
				_cmdGetFile = null;
				_currentCmd = null;
				SetProgress(false);
				CheckDisposed();
			}
		}

		#endregion

		#region PutFile functions

		Cmd_PutFile _cmdPutFile = null;

		#region Uploading from memory

		/// <overloads>
		/// Stores the data as a file on FTP server.
		/// </overloads>
		/// <summary>
		/// Stores byte array as a file on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="data">
		/// Array of bytes to store as a file on FTP server.
		/// </param>
		/// 
		/// <param name="path">
		/// The name for the destination file. If the file with the
		/// same name already exists at the server, then it will be 
		/// overwritten.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>PutFile</b> method synchronously stores
		/// array of bytes as a file on FTP server. The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>data</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void PutFile(int timeout, string path, byte[] data)
		{
			ThrowIfNull("data", data);
			PutFile(timeout, path, data, 0, data.Length);
		}

		/// <summary>
		/// Stores part of the byte array as a file on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="data">
		/// Array of bytes, the part of which to store
		/// as a file on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Offset from the lower bound of an array of bytes from
		/// where the uploading should start.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <param name="path">
		/// The name for the destination file. If the file with the
		/// same name already exists at the server, then it will be 
		/// overwritten.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>PutFile</b> method synchronously stores part of the
		/// array of bytes as a file on FTP server. The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>data</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void PutFile(int timeout,
			string path,
			byte[] data, 
			long offset, //from the lower bound
			long length)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("data", data);
			ThrowIfNull("path", path);
			ThrowIfNegative("length", length);
			ThrowIfNegative("offset", offset);

			MemoryStream ms = new MemoryStream(data);
			if(length > data.Length)
				length = data.Length;
			
			PutFile(timeout, path, ms, offset, length);
		}
		#endregion

		#region Uploading from file

		/// <summary>
		/// Uploads a file to FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="srcPath">
		/// The name of the file to upload to the FTP server.
		/// </param>
		/// 
		/// <param name="path">
		/// The name for the destination file. If the file with the
		/// same name already exists at the server, then it will be 
		/// overwritten.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>PutFile</b> method synchronously upload specified
		/// file to the FTP server. The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The following code is used to open the file:
		/// <code>
		/// FileStream fs = File.Open(srcPath, 
		///   FileMode.Open, 
		///   FileAccess.Read,
		///   FileShare.Read);
		/// </code>
		/// If the value of <i>srcPath</i> parameter is incorrect and
		/// <see cref="System.IO.File.Open">File.Open</see> method will 
		/// throw an exception then <b>PutFile</b> method will rethrow it.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>srcPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void PutFile(int timeout, string path, string srcPath)
		{
			PutFile(timeout, path, srcPath, 0, long.MaxValue);
		}

		/// <summary>
		/// Uploads part of the file to FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="srcPath">
		/// The name of the file, part of which to store
		/// as a file on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Offset in the source file from
		/// where the uploading should start.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <param name="path">
		/// The name for the destination file. If the file with the
		/// same name already exists at the server, then it will be 
		/// overwritten.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>PutFile</b> method synchronously uploads part
		/// of the file to FTP server. The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The following code is used to open the file:
		/// <code>
		/// FileStream fs = File.Open(srcPath, 
		///   FileMode.Open, 
		///   FileAccess.Read,
		///   FileShare.Read);
		/// </code>
		/// If the value of <i>srcPath</i> parameter is incorrect and
		/// <see cref="System.IO.File.Open">File.Open</see> method will 
		/// throw an exception then <b>PutFile</b> method will rethrow it.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>srcPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void PutFile(int timeout,
			string path,
			string srcPath, 
			long offset,
			long length)
		{
			CheckReadyForCmd();

			ThrowIfNull("srcPath", srcPath);
			ThrowIfNull("path", path);

			ThrowIfNegative("length", length);
			ThrowIfNegative("offset", offset);

			FileStream fs = File.Open(srcPath, 
				FileMode.Open, 
				FileAccess.Read,
				FileShare.Read);

			try
			{
				if(length > fs.Length)
					length = fs.Length;

				PutFile(timeout, path, fs, offset, length);
			}
			finally
			{
				fs.Close();
			}
		}
		#endregion

		#region Uploading from stream

		/// <summary>
		/// Stores data from the stream as a file on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="userStream">
		/// The stream from which the data is retrieving for
		/// uploading.
		/// </param>
		/// 
		/// <param name="path">
		/// The name for the destination file. If the file with the
		/// same name already exists at the server, then it will be 
		/// overwritten.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>PutFile</b> method synchronously stores data
		/// from the user stream as file on FTP server. 
		/// <b>PutFile</b> starts reading the user stream from the current
		/// position and immediately send data to the FTP server.
		/// This loop will continue till the stream has data to read
		/// (<see cref="System.IO.Stream.EndRead">Stream.EndRead</see>
		/// method returns non zero value). If you need to
		/// limit the amount of data to be uploaded then use
		/// other version of the <b>PutFile</b> method.
		/// The data channel used for uploading configured 
		/// either in ascii or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void PutFile(int timeout, string path, Stream userStream)
		{
			PutFile(timeout, path, userStream, -1, long.MaxValue);
		}

		/// <summary>
		/// Stores the specified part of the stream as
		/// file on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="userStream">
		/// User stream, part of which to store
		/// as a file on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Origin in the user stream from 
		/// where to start reading the data. 
		/// Specify negative value to start
		/// read from the current position.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <param name="path">
		/// The name for the destination file. If the file with the
		/// same name already exists at the server, then it will be 
		/// overwritten.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>PutFile</b> method synchronously stores 
		/// specified part of the user stream as file
		/// on FTP server. If the value of the <i>offset</i>
		/// parameter is positive then the 
		/// <see cref="System.IO.Stream.Seek">Stream.Seek</see> 
		/// method 
		/// is called on the <i>userStream</i> before reading starts.
		/// Then, <b>PutFile</b> starts reading the 
		/// user stream and immediately send data to the FTP server.
		/// This loop will stop when specified number of bytes
		/// will be sent or when the user stream has no more data to read
		/// (<see cref="System.IO.Stream.Read">Stream.Read</see>
		/// method return zero value).
		/// 
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void PutFile(int timeout, 
			string path,
			Stream userStream, 
			long offset, 
			long length)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("userStream", userStream);
			ThrowIfNull("path", path);
			ThrowIfNegative("length", length);

			SetProgress(true);
			try
			{
				if(offset >= 0)
					userStream.Seek(offset, SeekOrigin.Begin);

				lock(this)
				{
					if(!_disposed)
					{
						_cmdPutFile = new Cmd_PutFile(this);
						_currentCmd = _cmdPutFile;
					}
				}
				CheckDisposed();

				_cmdPutFile.Execute(timeout, 
					userStream, 
					path, 
					length);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdPutFile = null;
				SetProgress(false);
				CheckDisposed();
			}
		}
		#endregion


		/// <summary>
		/// Begins an asynchronous upload of the file.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="userStream">
		/// User stream, part of which to store
		/// as a file on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Origin in the user stream from 
		/// where to start reading the data. 
		/// Specify negative value to start
		/// read from the current position.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <param name="path">
		/// The name for the destination file. If the file with the
		/// same name already exists at the server, then it will be 
		/// overwritten.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginPutFile</b> method starts an asynchronous 
		/// storing the specified part of the user stream
		/// as file on FTP server.
		/// If the value of the <i>offset</i>
		/// parameter is positive then the 
		/// <see cref="System.IO.Stream.Seek">Stream.Seek</see> 
		/// method is called on the <i>userStream</i> before
		/// reading starts. Then, <b>BeginPutFile</b> 
		/// initiate an asynchronous loop - reading data
		/// from the user stream and uploading these
		/// data to the FTP server.
		/// This loop will stop when specified number of bytes
		/// will be sent or when the user stream has no more data 
		/// to read
		/// (<see cref="System.IO.Stream.EndRead">Stream.EndRead</see>
		/// method return zero value).
		/// The data channel, used for uploading, configured either in
		/// ascii or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>BeginPutFile</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginPutFile</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndPutFile">EndPutFile</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginPutFile</b>; if the asynchronous
		/// call has not completed, <b>EndPutFile</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// 
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginPutFile(int timeout, 
			string path,
			Stream userStream, 
			long offset, 
			long length,
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			ThrowIfNull("userStream", userStream);
			ThrowIfNull("path", path);
			ThrowIfNegative("length", length);
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			if(offset >= 0)
				userStream.Seek(offset, SeekOrigin.Begin);

			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdPutFile = new Cmd_PutFile(this);
						_currentCmd = _cmdPutFile;
					}
				}
				CheckDisposed();

				return _cmdPutFile.BeginExecute(timeout,
					userStream, 
					path,
					length,
					callback, 
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdPutFile = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdPutFile = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_currentCmd = null;
				_cmdPutFile = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending uploads of the file.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>EndPutFile</b>
		/// method completes the asynchronous 
		/// upload operation started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginPutFile">BeginPutFile</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginPutFile">BeginPutFile</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndPutFile</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void EndPutFile(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdPutFile.ARType, "EndPutFile");
			try
			{
				_cmdPutFile.EndExecute(asyncResult);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdPutFile = null;
				SetProgress(false);
				CheckDisposed();
			}
		}

		#endregion

		#region PutFileUnique functions

		Cmd_PutFileUnique _cmdPutFileUnique = null;

		#region Uploading from the memory

		/// <overloads>
		/// Stores the data as a file with unique name 
		/// on FTP server.
		/// </overloads>
		/// <summary>
		/// Stores an array of bytes as a file with unique name
		/// on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="data">
		/// Array of bytes to store as a file on FTP server.
		/// </param>
		/// 
		/// <returns>
		/// A string that contains a name of the file under
		/// which the data is stored on the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>PutFileUnique</b> method synchronously stores
		/// array of bytes as a file on FTP server. The resultant
		/// file is to be created in the current working directory
		/// under a name unique to that directory.
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFileUnique</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// Note that according RFC 959 (File Transfer Protocol)
		/// the response from the FTP server must include the 
		/// name generated, but some servers do not obey specification
		/// and do not provide the name for the newly created file. If
		/// this is a case <b>PutFileUnique</b> will return null
		/// (<b>Noting</b> in Visual Basic).
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>data</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public string PutFileUnique(int timeout, byte[] data)
		{
			ThrowIfNull("data", data);
			return PutFileUnique(timeout, data, 0, data.Length);
		}

		/// <summary>
		/// Stores part of the byte array as a file
		/// with unique name on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="data">
		/// Array of bytes, the part of which to store
		/// as a file with unique name on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Offset from the lower bound of an array of bytes from
		/// where the uploading should start. Specify zero to start
		/// from the beginning of an array.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <returns>
		/// A string that contains a name of the file under
		/// which the data is stored on the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>PutFileUnique</b> method synchronously stores
		/// part of the array of bytes as a file on FTP server. 
		/// The resultant
		/// file is to be created in the current working directory
		/// under a name unique to that directory.
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFileUnique</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// Note that according RFC 959 (File Transfer Protocol)
		/// the response from the FTP server must include the 
		/// name generated, but some servers do not obey specification
		/// and do not provide the name for the newly created file. If
		/// this is a case <b>PutFileUnique</b> will return null
		/// (<b>Noting</b> in Visual Basic).
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>data</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public string PutFileUnique(int timeout, 
			byte[] data, 
			long offset, 
			long length)
		{
			return PutFileUnique(timeout, data, offset, length, null);
		}


		/// <summary>
		/// Stores part of the byte array as a file
		/// with unique name on FTP server. Extend
		/// file name resolving mechanism with
		/// regular expression specified.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="data">
		/// Array of bytes, the part of which to store
		/// as a file with unique name on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Offset from the lower bound of an array of bytes from
		/// where the uploading should start. Specify zero to start
		/// from the beginning of an array.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <param name="regEx">
		/// An instance of the 
		/// <see cref="System.Text.RegularExpressions.Regex">Regex</see>
		/// class used to extract unique file name from the FTP's
		/// server response. May be null (<b>Nothing</b> 
		/// in Visual Basic) in which case the default mechanism
		/// will be used. See Remarks section for details.
		/// </param>
		/// 
		/// <returns>
		/// A string that contains a name of the file under
		/// which the data is stored on the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>PutFileUnique</b> method synchronously stores
		/// part of the array of bytes as a file on FTP server. 
		/// The resultant
		/// file is to be created in the current working directory
		/// under a name unique to that directory.
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFileUnique</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// Note that according RFC 959 (File Transfer Protocol)
		/// the response from the FTP server must include the 
		/// name generated, but some servers do not obey specification
		/// and do not provide the name for the newly created file. If
		/// this is a case <b>PutFileUnique</b> will return null
		/// (<b>Noting</b> in Visual Basic).
		/// </para>
		/// 
		/// <para>
		/// If <i>regEx</i> is not null then to extract the generated
		/// file name from the server's response its text match against the
		/// <i>regEx</i> regular expression and if the match
		/// is found then the value of the group with the name "name"
		/// is returned. If <i>regEx</i> is null (<b>Nothing</b>
		/// in Visual Basic) or match is not found then the default 
		/// mechanism of extracting file name is used. The default
		/// mechanism recognize generated file name for most commonly used FTP 
		/// servers. See example of using <b>PutFileUnique</b> method
		/// below.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>data</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		/// <example>
		/// Example below shows how to extend base functionality
		/// of the <b>PutFileUnique</b> method by using
		/// regular expression.
		/// <code>
		/// void StoreUnique(FtpClient ftp, int timeout, Stream stream)
		///	{
		///		Regex re = 
		///			new Regex("^150[- ]Opening BINARY mode data connection for *(?&lt;name&gt;.*)\r\n$", 
		///			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		///
		///		string cwd = ftp.GetWorkingDirectory(timeout);
		///		string fname = ftp.PutFileUnique(timeout, stream, 0, (int)stream.Length, re);
		///		Console.WriteLine("File in '{0}' directory; name is '{1}'.", cwd, fname);
		///	}
		/// </code>
		/// </example>
		public string PutFileUnique(int timeout, 
			byte[] data, 
			long offset, 
			long length,
			Regex regEx)
		{
			ThrowIfNull("data", data);
			ThrowIfNegative("offset", offset);
			ThrowIfNegative("length", length);

			MemoryStream ms = new MemoryStream(data);
			if(length > data.Length)
				length = data.Length;
			
			return PutFileUnique(timeout, ms, offset, length, regEx);
		}

		#endregion

		#region Uploading from the file

		/// <summary>
		/// Stores file under unique name on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="srcPath">
		/// The name of the file to upload to the FTP server.
		/// </param>
		/// 
		/// <returns>
		/// A string that contains a name of the newly
		/// created file on the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>PutFileUnique</b> method synchronously stores
		/// specified file on FTP server. The resultant
		/// file is to be created in the current working directory
		/// under a name unique to that directory.
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFileUnique</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The following code is used to open the source file:
		/// <code>
		/// FileStream fs = File.Open(srcPath, 
		///   FileMode.Open, 
		///   FileAccess.Read,
		///   FileShare.Read);
		/// </code>
		/// If the value of <i>srcPath</i> parameter is incorrect and
		/// <see cref="System.IO.File.Open">File.Open</see> method will 
		/// throw an exception then <b>PutFileUnique</b> method will rethrow it.
		/// </para>
		/// 
		/// <para>
		/// Note that according RFC 959 (File Transfer Protocol)
		/// the response from the FTP server must include the 
		/// name generated, but some servers do not obey specification
		/// and do not provide the name for the newly created file. If
		/// this is a case <b>PutFileUnique</b> will return null
		/// (<b>Noting</b> in Visual Basic).
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>srcPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public string PutFileUnique(int timeout, string srcPath)
		{
			return PutFileUnique(timeout, srcPath, 0, long.MaxValue);
		}

		/// <summary>
		/// Uploads part of the file on the FTP server and store
		/// it under unique name.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="srcPath">
		/// The name of the file, part of which to store
		/// as a file on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Offset from the beginning of the file from
		/// where the uploads should start.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <returns>
		/// A string that contains a name of the file under
		/// which the data is stored on the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>PutFileUnique</b> method synchronously uploads
		/// part of the file on FTP server. The resultant
		/// file is to be created in the current working directory
		/// under a name unique to that directory.
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFileUnique</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The following code is used to open the source file:
		/// <code>
		/// FileStream fs = File.Open(srcPath, 
		///   FileMode.Open, 
		///   FileAccess.Read,
		///   FileShare.Read);
		/// </code>
		/// If the value of <i>srcPath</i> parameter is incorrect and
		/// <see cref="System.IO.File.Open">File.Open</see> method will 
		/// throw an exception then <b>PutFileUnique</b> method will rethrow it.
		/// </para>
		/// 
		/// <para>
		/// Note that according RFC 959 (File Transfer Protocol)
		/// the response from the FTP server must include the 
		/// name generated, but some servers do not obey specification
		/// and do not provide the name for the newly created file. If
		/// this is a case <b>PutFileUnique</b> will return null
		/// (<b>Noting</b> in Visual Basic).
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>srcPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public string PutFileUnique(int timeout, 
			string srcPath, 
			long offset, 
			long length)
		{
			return PutFileUnique(timeout, srcPath, offset, length, null);
		}


		/// <summary>
		/// Uploads part of the file on the FTP server and 
		/// store it under unique name. Extend file name
		/// resolving mechanism with regular expression
		/// specified.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="srcPath">
		/// The name of the file, part of which to store
		/// as a file on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Offset from the beginning of the file from
		/// where the uploads should start.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <param name="regEx">
		/// An instance of the 
		/// <see cref="System.Text.RegularExpressions.Regex">Regex</see>
		/// class used to extract unique file name from the FTP's
		/// server response. May be null (<b>Nothing</b> 
		/// in Visual Basic) in which case the default mechanism
		/// will be used. See Remarks section for details.
		/// </param>
		/// 
		/// <returns>
		/// A string that contains a name of the file under
		/// which the data is stored on the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>PutFileUnique</b> method synchronously stores
		/// part of the file on FTP server. The resultant
		/// file is to be created in the current working directory
		/// under a name unique to that directory.
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFileUnique</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The following code is used to open the source file:
		/// <code>
		/// FileStream fs = File.Open(srcPath, 
		///   FileMode.Open, 
		///   FileAccess.Read,
		///   FileShare.Read);
		/// </code>
		/// If the value of <i>srcPath</i> parameter is incorrect and
		/// <see cref="System.IO.File.Open">File.Open</see> method will 
		/// throw an exception then <b>PutFileUnique</b> method will rethrow it.
		/// </para>
		/// 
		/// <para>
		/// Note that according RFC 959 (File Transfer Protocol)
		/// the response from the FTP server must include the 
		/// name generated, but some servers do not obey specification
		/// and do not provide the name for the newly created file. If
		/// this is a case <b>PutFileUnique</b> will return null
		/// (<b>Noting</b> in Visual Basic).
		/// </para>
		/// 
		/// <para>
		/// If <i>regEx</i> is not null then to extract the generated
		/// file name from the server's response its text match against the
		/// <i>regEx</i> regular expression and if the match
		/// is found then the value of the group with the name "name"
		/// is returned. If <i>regEx</i> is null (<b>Nothing</b>
		/// in Visual Basic) or match is not found then the default 
		/// mechanism of extracting file name is used. The default
		/// mechanism recognize generated file name for most commonly
		/// used FTP servers. See example of using <b>PutFileUnique</b>
		/// method below.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>srcPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		/// <example>
		/// Example below shows how to extend base functionality
		/// of the <b>PutFileUnique</b> method by using
		/// regular expression.
		/// <code>
		/// void StoreUnique(FtpClient ftp, int timeout, string srcPath)
		///	{
		///		Regex re = 
		///			new Regex("^150[- ]Opening BINARY mode data connection for *(?&lt;name&gt;.*)\r\n$", 
		///			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		///
		///		string cwd = ftp.GetWorkingDirectory(timeout);
		///		string fname = ftp.PutFileUnique(timeout, srcPath, 0, int.MaxValue, re);
		///		Console.WriteLine("File in '{0}' directory; name is '{1}'.", cwd, fname);
		///	}
		/// </code>
		/// </example>
		public string PutFileUnique(int timeout, 
			string srcPath, 
			long offset, 
			long length,
			Regex regEx)
		{
			CheckReadyForCmd();
			ThrowIfNull("srcPath", srcPath);
			ThrowIfNegative("length", length);
			ThrowIfNegative("offset", length);
			timeout = GetTimeoutValue(timeout);

			FileStream fs = File.Open(srcPath, 
				FileMode.Open, 
				FileAccess.Read,
				FileShare.Read);

			string retName = null;
			try
			{
				if(length > fs.Length)
					length = fs.Length;

				retName = PutFileUnique(timeout, fs, offset, length, regEx);
			}
			finally
			{
				fs.Close();
			}

			return retName;
		}
		#endregion

		#region Uploading from stream

		/// <summary>
		/// Stores data from the stream as a file with
		/// unique name on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="userStream">
		/// The stream which contains the data to upload.
		/// </param>
		/// 
		/// <returns>
		/// A string that contains a name of the newly
		/// created file on the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>PutFileUnique</b> method synchronously stores
		/// data from the user stream (starting from the current position)
		/// as file on FTP server.
		/// The resultant
		/// file is to be created in the current working directory
		/// under a name unique to that directory.
		/// The <b>PutFileUnique</b> method initiate the synchronous loop - 
		/// reading data from the user stream and uploading this data to the
		/// FTP server. This loop will stop when the user stream has no more 
		/// data to read (<see cref="System.IO.Stream.Read">Stream.Read</see>
		/// method return zero value). 
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFileUnique</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// Note that according RFC 959 (File Transfer Protocol)
		/// the response from the FTP server must include the 
		/// name generated, but some servers do not obey specification
		/// and do not provide the name for the newly created file. If
		/// this is a case <b>PutFileUnique</b> will return null
		/// (<b>Noting</b> in Visual Basic).
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>	
		public string PutFileUnique(int timeout, Stream userStream)
		{
			return PutFileUnique(timeout, userStream, -1, long.MaxValue);
		}

		/// <summary>
		/// Stores specified part of the user stream as a file with
		/// unique name on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="userStream">
		/// User stream, part of which to store
		/// as a file on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Origin in the user stream from 
		/// where to start reading the data. 
		/// Specify negative value to start
		/// read from the current position.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <returns>
		/// A string that contains a name of the newly
		/// created file on the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>PutFileUnique</b> method synchronously stores
		/// the specified part of the user stream as file on FTP server.
		/// The resultant
		/// file is to be created in the current working directory
		/// under a name unique to that directory.
		/// If the value of the <i>offset</i> parameter is positive then the 
		/// <see cref="System.IO.Stream.Seek">Stream.Seek</see> method is 
		/// called on the <i>userStream</i> before reading.
		/// Then, <b>PutFileUnique</b> initiate the synchronous loop - 
		/// reading data from the user stream and uploading this data to the
		/// FTP server. This loop will stop when specified number of
		/// bytes will be sent or when the user stream has no more 
		/// data to read (<see cref="System.IO.Stream.Read">Stream.Read</see>
		/// method return zero value). 
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFileUnique</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// Note that according RFC 959 (File Transfer Protocol)
		/// the response from the FTP server must include the 
		/// name generated, but some servers do not obey specification
		/// and do not provide the name for the newly created file. If
		/// this is a case <b>PutFileUnique</b> will return null
		/// (<b>Noting</b> in Visual Basic).
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>	
		public string PutFileUnique(int timeout, 
			Stream userStream, 
			long offset, 
			long length)
		{
			return PutFileUnique(timeout, userStream, offset, length, null);
		}

		/// <summary>
		/// Stores specified part of the user stream as a file
		/// with unique name on FTP server. Extend
		/// file name resolving mechanism with
		/// regular expression specified.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="userStream">
		/// User stream, part of which to store
		/// as a file on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Origin in the user stream from 
		/// where to start reading the data. 
		/// Specify negative value to start
		/// read from the current position.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <param name="regEx">
		/// An instance of the 
		/// <see cref="System.Text.RegularExpressions.Regex">Regex</see>
		/// class used to extract unique file name from the FTP's
		/// server response. May be null (<b>Nothing</b> 
		/// in Visual Basic) in which case the default mechanism
		/// will be used. See Remarks section for details.
		/// </param>
		/// 
		/// <returns>
		/// A string that contains a name of the file under
		/// which the data is stored on the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>PutFileUnique</b> method synchronously stores
		/// the specified part of the user stream as file on FTP server.
		/// The resultant
		/// file is to be created in the current working directory
		/// under a name unique to that directory.
		/// If the value of the <i>offset</i> parameter is positive then the 
		/// <see cref="System.IO.Stream.Seek">Stream.Seek</see> method is 
		/// called on the <i>userStream</i> before reading.
		/// Then, <b>PutFileUnique</b> initiate the synchronous loop - 
		/// reading data from the user stream and uploading this data to the
		/// FTP server. This loop will stop when specified number of
		/// bytes will be sent or when the user stream has no more 
		/// data to read (<see cref="System.IO.Stream.Read">Stream.Read</see>
		/// method return zero value). 
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>PutFileUnique</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// Note that according RFC 959 (File Transfer Protocol)
		/// the response from the FTP server must include the 
		/// name generated, but some servers do not obey specification
		/// and do not provide the name for the newly created file. If
		/// this is a case <b>PutFileUnique</b> will return null
		/// (<b>Noting</b> in Visual Basic).
		/// </para>
		/// 
		/// <para>
		/// If <i>regEx</i> is not null then to extract the generated
		/// file name from the server's response its text match against the
		/// <i>regEx</i> regular expression and if the match
		/// is found then the value of the group with the name "name"
		/// is returned. If <i>regEx</i> is null (<b>Nothing</b>
		/// in Visual Basic) or match is not found then the default 
		/// mechanism of extracting file name is used. The default
		/// mechanism recognize generated file name for most commonly used FTP 
		/// servers. See example of using <b>PutFileUnique</b> method
		/// below.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		/// <example>
		/// Example below shows how to extend base functionality
		/// of the <b>PutFileUnique</b> method by using
		/// regular expression.
		/// <code>
		/// void StoreUnique(FtpClient ftp, int timeout, Stream stream)
		///	{
		///		Regex re = 
		///			new Regex("^150[- ]Opening BINARY mode data connection for *(?&lt;name&gt;.*)\r\n$", 
		///			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		///
		///		string cwd = ftp.GetWorkingDirectory(timeout);
		///		string fname = ftp.PutFileUnique(timeout, stream, 0, (int)stream.Length, re);
		///		Console.WriteLine("File in '{0}' directory; name is '{1}'.", cwd, fname);
		///	}
		/// </code>
		/// </example>
		public string PutFileUnique(int timeout, 
			Stream userStream, 
			long offset, 
			long length,
			Regex regEx)
		{
			CheckReadyForCmd();
			ThrowIfNull("userStream", userStream);
			ThrowIfNegative("length", length);
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			string uniqueFileName = null;
			try
			{
				if(offset >= 0)
					userStream.Seek(offset, SeekOrigin.Begin);

				lock(this)
				{
					if(!_disposed)
					{
						_cmdPutFileUnique = new Cmd_PutFileUnique(this, regEx);
						_currentCmd = _cmdPutFileUnique;
					}
				}
				CheckDisposed();

				uniqueFileName = _cmdPutFileUnique.Execute(timeout, 
					userStream, 
					length);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdPutFileUnique = null;
				SetProgress(false);
				CheckDisposed();
			}
			return uniqueFileName;
		}
		#endregion

		/// <summary>
		/// Begins an asynchronous upload of the file. File is
		/// to be created on the FTP server with unique name.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="userStream">
		/// User stream, part of which to store
		/// as a file on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Origin in the user stream from 
		/// where to start reading the data. 
		/// Specify negative value to start
		/// read from the current position.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <param name="regEx">
		/// An instance of the 
		/// <see cref="System.Text.RegularExpressions.Regex">Regex</see>
		/// class used to extract unique file name from the FTP's
		/// server response. May be null (<b>Nothing</b> 
		/// in Visual Basic) in which case the default mechanism
		/// will be used. See Remarks section for details.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginPutFileUnique</b> method starts an asynchronous 
		/// storing the specified part of the user stream
		/// as file on FTP server.
		/// The resultant
		/// file is to be created in the current working directory
		/// under a name unique to that directory.
		/// If the value of the <i>offset</i>
		/// parameter is positive then the 
		/// <see cref="System.IO.Stream.Seek">Stream.Seek</see> 
		/// method is called on the <i>userStream</i> before
		/// reading begins. Then, <b>BeginPutFileUnique</b> 
		/// initiate an asynchronous loop - reading data
		/// from the user stream and uploading this
		/// data to the FTP server.
		/// This loop will stop when specified number of bytes
		/// will be sent or when the user stream has no more data 
		/// to read
		/// (<see cref="System.IO.Stream.EndRead">Stream.EndRead</see>
		/// method return zero value).
		/// The data channel, used for uploading, configured either in
		/// ascii or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>BeginPutFileUnique</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginPutFileUnique</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndPutFileUnique">EndPutFileUnique</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginPutFileUnique</b>; if the asynchronous
		/// call has not completed, <b>EndPutFileUnique</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that according RFC 959 (File Transfer Protocol)
		/// the response from the FTP server must include the 
		/// name generated, but some servers do not obey specification
		/// and do not provide the name for the newly created file. If
		/// this is a case <b>EndPutFileUnique</b> will return null
		/// (<b>Noting</b> in Visual Basic).
		/// </para>
		/// 
		/// <para>
		/// If <i>regEx</i> is not null then to extract the generated
		/// file name from the server's response its text match against the
		/// <i>regEx</i> regular expression and if the match
		/// is found then the value of the group with the name "name"
		/// is returned. If <i>regEx</i> is null (<b>Nothing</b>
		/// in Visual Basic) or match is not found then the default 
		/// mechanism of extracting file name is used. The default
		/// mechanism recognize generated file name for most commonly used FTP 
		/// servers.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// 
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginPutFileUnique(int timeout, 
			Stream userStream, 
			long offset, 
			long length,
			Regex regEx,
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("userStream", userStream);
			ThrowIfNegative("length", length);

			SetProgress(true);
			
			if(offset >= 0)
				userStream.Seek(offset, SeekOrigin.Begin);

			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdPutFileUnique = new Cmd_PutFileUnique(this, regEx);
						_currentCmd = _cmdPutFileUnique;
					}
				}
				CheckDisposed();

				return _cmdPutFileUnique.BeginExecute(timeout,
					userStream, 
					length,
					callback, 
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdPutFileUnique = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdPutFileUnique = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(Exception)
			{
				_currentCmd = null;
				_cmdPutFileUnique = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending uploads of the file, which will be
		/// created under unique name.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <returns>
		/// A string that contains a name of the newly
		/// created file on the FTP server.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>EndPutFileUnique</b>
		/// method completes the asynchronous 
		/// upload operation started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginPutFileUnique">BeginPutFileUnique</see>
		/// method.
		/// 
		/// <para>
		/// Note that according RFC 959 (File Transfer Protocol)
		/// the response from the FTP server must include the 
		/// name generated, but some servers do not obey specification
		/// and do not provide the name for the newly created file. If
		/// this is a case <b>EndPutFileUnique</b> will return null
		/// (<b>Noting</b> in Visual Basic).
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginPutFileUnique">BeginPutFileUnique</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndPutFileUnique</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public string EndPutFileUnique(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdPutFileUnique.ARType, "EndPutFileUnique");
			string uniqueFileName = null;
			try
			{
				uniqueFileName = _cmdPutFileUnique.EndExecute(asyncResult);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdPutFileUnique = null;
				SetProgress(false);
			}
			return uniqueFileName;
		}

		#endregion

		#region AppendToFile functions

		Cmd_AppendToFile _cmdAppendToFile = null;

		#region Upload from the memory

		/// <overloads>
		/// Appends data to the file on FTP server.
		/// </overloads>
		/// <summary>
		/// Appends array of bytes to the file specified.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="data">
		/// Array of bytes which needs to append 
		/// to a file on FTP server.
		/// </param>
		/// 
		/// <param name="path">
		/// Name of the file to which to append the data.
		/// If the file doesn't exists it will be created.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>AppendToFile</b> method synchronously appends
		/// data to the file on FTP server. If the file specified
		/// in the <i>path</i> parameter doesn't exists at the server
		/// site, then it will be created.
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>AppendToFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>data</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void AppendToFile(int timeout, string path, byte[] data)
		{
			ThrowIfNull("data", data);
			AppendToFile(timeout, path, data, 0, data.Length);
		}


		/// <summary>
		/// Appends part of the array of bytes to the file specified.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Name of the file to which to append the data.
		/// If the file doesn't exists it will be created.
		/// </param>
		/// 
		/// <param name="data">
		/// Array of bytes the part of which needs to append 
		/// to a file on FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Offset from the lower bound of an array of bytes from
		/// where the uploading should start.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>AppendToFile</b> method synchronously appends
		/// specified part of the bytes array to the file on FTP server.
		/// If the file specified
		/// in the <i>path</i> parameter doesn't exists at the server
		/// site, then it will be created.
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>AppendToFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>data</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void AppendToFile(int timeout,
			string path,
			byte[] data, 
			long offset, 
			long length)
		{
			ThrowIfNull("data", data);
			ThrowIfNull("path", path);

			ThrowIfNegative("offset", offset);
			ThrowIfNegative("length", length);

			MemoryStream ms = new MemoryStream(data);
			if(length > data.Length)
				length = data.Length;

			AppendToFile(timeout, path, ms, offset, length);
		}
		#endregion

		#region Upload from the file

		/// <summary>
		/// Appends data from the local file to the 
		/// file at the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Name of the file to which to append the data.
		/// If the file doesn't exists it will be created.
		/// </param>
		/// 
		/// <param name="srcPath">
		/// Name of the local file which data should be appended
		/// to the file at the FTP server.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>AppendToFile</b> method synchronously appends
		/// data from the local file to the file at the FTP server. 
		/// If the file specified
		/// in the <i>path</i> parameter doesn't exists at the server
		/// site, then it will be created.
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>AppendToFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The following code is used to open the local file:
		/// <code>
		/// FileStream fs = File.Open(srcPath, 
		///   FileMode.Open, 
		///   FileAccess.Read,
		///   FileShare.Read);
		/// </code>
		/// If the value of <i>srcPath</i> parameter is incorrect and
		/// <see cref="System.IO.File.Open">File.Open</see> method will 
		/// throw an exception then <b>AppendToFile</b> method will rethrow it.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>srcPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void AppendToFile(int timeout,
			string path,
			string srcPath)
		{
			AppendToFile(timeout, path, srcPath, 0, long.MaxValue);
		}

		/// <summary>
		/// Appends part of the local file to the 
		/// file at the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Name of the file to which to append the data.
		/// If the file doesn't exists it will be created.
		/// </param>
		/// 
		/// <param name="srcPath">
		/// Name of the local file, part of which should be appended
		/// to the file at the FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Offset in the local file from
		/// where the uploading should start.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>AppendToFile</b> method synchronously appends
		/// part of the local file to the file at the FTP server. 
		/// If the file specified
		/// in the <i>path</i> parameter doesn't exists at the server
		/// site, then it will be created.
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>AppendToFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The following code is used to open the local file:
		/// <code>
		/// FileStream fs = File.Open(srcPath, 
		///   FileMode.Open, 
		///   FileAccess.Read,
		///   FileShare.Read);
		/// </code>
		/// If the value of <i>srcPath</i> parameter is incorrect and
		/// <see cref="System.IO.File.Open">File.Open</see> method will 
		/// throw an exception then <b>AppendToFile</b> method will rethrow it.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>offset</i> or <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>srcPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void AppendToFile(int timeout, 
			string path, 
			string srcPath,
			long offset,
			long length)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			ThrowIfNull("srcPath", srcPath);
			ThrowIfNull("path", path);

			ThrowIfNegative("length", length);
			ThrowIfNegative("offset", offset);

			FileStream fs = File.Open(srcPath, 
				FileMode.Open, 
				FileAccess.Read,
				FileShare.Read);

			try
			{
				if(length > fs.Length)
					length = fs.Length;

				AppendToFile(timeout, path, fs, offset, length);
			}
			finally
			{
				fs.Close();
			}
		}
		#endregion

		#region Uploading from stream

		/// <summary>
		/// Appends data from the user stream to the file on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Name of the file to which to append the data.
		/// If the file doesn't exists it will be created.
		/// </param>
		/// 
		/// <param name="userStream">
		/// User stream which data needs to append
		/// to the file at the FTP server.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>AppendToFile</b> method synchronously appends
		/// data from the user stream (starting from the current position)
		/// to the file on FTP server. 
		/// 
		/// If the file specified
		/// in the <i>path</i> parameter doesn't exists at the server
		/// site, then it will be created.
		/// 
		/// The <b>AppendToFile</b> method initiate the synchronous loop - 
		/// reading data from the user stream and appending this data to the
		/// file at the FTP server. This loop will stop when the user stream
		/// has no more data to read 
		/// (<see cref="System.IO.Stream.Read">Stream.Read</see>
		/// method return zero value).
		/// 
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>AppendToFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void AppendToFile(int timeout, string path, Stream userStream)
		{
			AppendToFile(timeout, path, userStream, -1, long.MaxValue);
		}

		/// <summary>
		/// Appends part of the user stream to the file on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Name of the file to which to append the data.
		/// If the file doesn't exists it will be created.
		/// </param>
		/// 
		/// <param name="userStream">
		/// User stream, part of which needs to be appended
		/// to the file at the FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Origin in the user stream from 
		/// where to start reading the data. 
		/// Specify negative value to start
		/// read from the current position.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>AppendToFile</b> method synchronously appends
		/// specified part of the user stream 
		/// to the file on FTP server. 
		/// 
		/// If the file specified
		/// in the <i>path</i> parameter doesn't exists at the server
		/// site, then it will be created.
		/// 
		/// If the value of the <i>offset</i> parameter is positive then the 
		/// <see cref="System.IO.Stream.Seek">Stream.Seek</see> method is 
		/// called on the <i>userStream</i> before reading begins.
		/// Then, <b>AppendToFile</b> initiate the synchronous loop - 
		/// reading data from the user stream and appending this data to the
		/// file at the FTP server. This loop will stop when specified number of
		/// bytes will be appended or when the user stream has no more 
		/// data to read (<see cref="System.IO.Stream.Read">Stream.Read</see>
		/// method return zero value).		
		/// 
		/// The data
		/// channel used for uploading configured either in ascii 
		/// or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>AppendToFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or if
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void AppendToFile(int timeout, 
			string path,
			Stream userStream, 
			long offset, 
			long length)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);
			ThrowIfNull("userStream", userStream);
			ThrowIfNegative("length", length);

			SetProgress(true);
			try
			{
				if(offset >= 0)
					userStream.Seek(offset, SeekOrigin.Begin);

				lock(this)
				{
					if(!_disposed)
					{
						_cmdAppendToFile = new Cmd_AppendToFile(this);
						_currentCmd = _cmdAppendToFile;
					}
				}
				CheckDisposed();

				_cmdAppendToFile.Execute(timeout, 
					userStream, 
					path, 
					length);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdAppendToFile = null;
				SetProgress(false);
				CheckDisposed();
			}
		}
		#endregion

		/// <summary>
		/// Begins an asynchronous append data to the file at FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Name of the file to which to append the data.
		/// If the file doesn't exists it will be created.
		/// </param>
		/// 
		/// <param name="userStream">
		/// User stream, part of which needs to be appended
		/// to the file at the FTP server.
		/// </param>
		/// 
		/// <param name="offset">
		/// Origin in the user stream from 
		/// where to start reading the data. 
		/// Specify negative value to start
		/// read from the current position.
		/// </param>
		/// 
		/// <param name="length">
		/// Number of bytes to upload.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginAppendToFile</b> method starts an asynchronous 
		/// appending the specified part of the user stream
		/// to the file at the FTP server.
		/// 
		/// If the file specified
		/// in the <i>path</i> parameter doesn't exists at the server
		/// site, then it will be created.
		/// 
		/// If the value of the <i>offset</i>
		/// parameter is positive then the 
		/// <see cref="System.IO.Stream.Seek">Stream.Seek</see> 
		/// method is called on the <i>userStream</i> before
		/// reading begins. 
		/// 
		/// Then, <b>BeginAppendToFile</b> 
		/// initiate an asynchronous loop - reading data
		/// from the user stream and appending this
		/// data to the file at the FTP server.
		/// This loop will stop either when specified number of bytes
		/// will be appended or when the user stream has no more data 
		/// to read
		/// (<see cref="System.IO.Stream.EndRead">Stream.EndRead</see>
		/// method return zero value).
		/// The data channel, used for uploading, configured either in
		/// ascii or in binary mode depending of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.DataType">FtpDataType</see>
		/// property value. <b>BeginAppendToFile</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginAppendToFile</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndAppendToFile">EndAppendToFile</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginAppendToFile</b>; if the asynchronous
		/// call has not completed, <b>EndAppendToFile</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// 
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// <para>-or-</para>
		/// <i>length</i> is negative.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>userStream</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginAppendToFile(int timeout, 
			string path, 
			Stream userStream, 
			long offset, 
			long length,
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);
			ThrowIfNull("userStream", userStream);
			ThrowIfNegative("length", length);

			SetProgress(true);
			
			if(offset >= 0)
				userStream.Seek(offset, SeekOrigin.Begin);

			try
			{
			
				lock(this)
				{
					if(!_disposed)
					{
						_cmdAppendToFile = new Cmd_AppendToFile(this);
						_currentCmd = _cmdAppendToFile;
					}
				}
				CheckDisposed();

				return _cmdAppendToFile.BeginExecute(timeout,
					userStream, 
					path,
					length,
					callback, 
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdAppendToFile = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdAppendToFile = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_currentCmd = null;
				_cmdAppendToFile = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}

		/// <summary>
		/// Ends a pending append.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>EndAppendToFile</b>
		/// method completes the asynchronous 
		/// append operation started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginAppendToFile">BeginAppendToFile</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginAppendToFile">BeginAppendToFile</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndAppendToFile</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpAbortedException">
		/// Uploading was aborted by 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Abort">Abort</see>, 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// methods or their asynchronous versions.
		/// </exception>
		public void EndAppendToFile(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdAppendToFile.ARType, "EndAppendToFile");
			try
			{
				_cmdAppendToFile.EndExecute(asyncResult);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdAppendToFile = null;
				SetProgress(false);
				CheckDisposed();
			}
		}

		#endregion

		#region Rename functions
		Cmd_Rename _cmdRename = null;

		/// <summary>
		/// Rename file at the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="srcPath">
		/// String which contains the old pathname of the file
		/// which is to be renamed.
		/// </param>
		/// 
		/// <param name="dstPath">
		/// String which contains the new pathname for the
		/// file which is to be renamed.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>RenameFile</b> method synchronously 
		/// rename file at the FTP server.
		/// The sequence consisting of two FTP's commands
		/// are used to complete this operation. These commands are
		/// <b>RNFR</b> and <b>RNTO</b>.
		/// <b>RenameFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>srcPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>dstPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public void RenameFile(int timeout, 
			string srcPath, 
			string dstPath)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("srcPath", srcPath);
			ThrowIfNull("dstPath", dstPath);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdRename = new Cmd_Rename(this);
						_currentCmd = _cmdRename;
					}
				}
				CheckDisposed();

				_cmdRename.Execute(timeout, 
					srcPath, 
					dstPath);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdRename = null;
				SetProgress(false);
				CheckDisposed();
			}
		}

		
		/// <summary>
		/// Begins an asynchronous rename file operation.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="srcPath">
		/// String which contains the old pathname of the file
		/// which is to be renamed.
		/// </param>
		/// 
		/// <param name="dstPath">
		/// String which contains the new pathname for the
		/// file which is to be renamed.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginRenameFile</b> method starts an asynchronous
		/// renaming of the file at the FTP server.
		/// The sequence consisting of two FTP's commands
		/// are used to complete this operation. This commands are
		/// <b>RNFR</b> and <b>RNTO</b>.
		/// <b>BeginRenameFile</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginRenameFile</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndRenameFile">EndRenameFile</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginRenameFile</b>; if the asynchronous
		/// call has not completed, <b>EndRenameFile</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>srcPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// <para>-or-</para>
		/// <i>dstPath</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginRenameFile(int timeout,
			string srcPath,
			string dstPath,
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("srcPath", srcPath);
			ThrowIfNull("dstPath", dstPath);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdRename = new Cmd_Rename(this);
						_currentCmd = _cmdRename;
					}
				}
				CheckDisposed();


				return _cmdRename.BeginExecute(timeout, 
					srcPath, 
					dstPath,
					callback,
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdRename = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdRename = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(Exception)
			{
				_currentCmd = null;
				_cmdRename = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}

		/// <summary>
		/// Ends a pending asynchronous renaming.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>EndRenameFile</b>
		/// method completes the asynchronous rename command started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginRenameFile">BeginRenameFile</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginRenameFile">BeginRenameFile</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndRenameFile</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public void EndRenameFile(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdRename.ARType, "EndRename");
			try
			{
				_cmdRename.EndExecute(asyncResult);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdRename = null;
				SetProgress(false);
				CheckDisposed();
			}
		}
		#endregion

		#region DeleteFile functions
		Cmd_Single _cmdDeleteFile = null;

		/// <summary>
		/// Delete the specified file at the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// The name of the file to be deleted. It should
		/// be absolute or relative to the current working directory.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>DeleteFile</b> method synchronously delete
		/// file at the FTP server. The FTP's command used for delete 
		/// the file is <b>DELE</b>. <b>DeleteFile</b> method blocks
		/// until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public void DeleteFile(int timeout, string path)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);


			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdDeleteFile = new Cmd_Single(this);
						_currentCmd = _cmdDeleteFile;
					}
				}
				CheckDisposed();

				string cmd = "DELE " + path;
				FtpResponse response = _cmdDeleteFile.Execute(timeout, cmd);
				CheckCompletionResponse(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdDeleteFile = null;
				SetProgress(false);
				CheckDisposed();
			}
		}

	
		/// <summary>
		/// Begins an asynchronous delete of the file specified.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// The name of the file to be deleted. It should
		/// be absolute or relative to the current working directory.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginDeleteFile</b> method starts an asynchronous 
		/// delete of the file specified. The FTP's command used for delete 
		/// the file is <b>DELE</b>. <b>BeginDeleteFile</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginDeleteFile</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndDeleteFile">EndDeleteFile</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginDeleteFile</b>; if the asynchronous
		/// call has not completed, <b>EndDeleteFile</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginDeleteFile(int timeout, 
			string path,
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdDeleteFile = new Cmd_Single(this);
						_currentCmd = _cmdDeleteFile;
					}
				}
				CheckDisposed();

				string cmd = "DELE " + path;
				return _cmdDeleteFile.BeginExecute(timeout,
					cmd,
					callback,
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdDeleteFile = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdDeleteFile = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(Exception)
			{
				_currentCmd = null;
				_cmdDeleteFile = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending asynchronous delete of the file.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>EndDeleteFile</b>
		/// method completes the asynchronous delete command started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginDeleteFile">BeginDeleteFile</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginDeleteFile">BeginDeleteFile</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndDeleteFile</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public void EndDeleteFile(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdDeleteFile.ARType, "EndDeleteFile");
			try
			{
				FtpResponse response = _cmdDeleteFile.EndExecute(asyncResult);
				CheckCompletionResponse(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdDeleteFile = null;
				SetProgress(false);
				CheckDisposed();
			}
		}
		#endregion

		//Directory functions
		#region ChangeDirectory functions
		Cmd_Single _cmdCWD = null;

		/// <summary>
		/// Change working directory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// String that contains pathname of existing directory
		/// which will become current directory.
		/// It should
		/// be absolute or relative to the current working directory.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>ChangeDirectory</b> method synchronously changes
		/// the current working directory at the FTP server. The FTP's command
		/// used for that is <b>CWD</b>. <b>ChangeDirectory</b> method 
		/// blocks until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public void ChangeDirectory(int timeout, string path)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdCWD = new Cmd_Single(this);
						_currentCmd = _cmdCWD;
					}
				}
				CheckDisposed();

				string cmd = "CWD " + path;
				FtpResponse response = _cmdCWD.Execute(timeout, cmd);
				CheckCompletionResponse(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdCWD = null;
				SetProgress(false);
				CheckDisposed();
			}
		}


		/// <summary>
		/// Begins an asynchronous change working directory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// String that contains pathname of existing directory
		/// which will become current directory.
		/// It should
		/// be absolute or relative to the current working directory.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginChangeDirectory</b> method starts an asynchronous 
		/// change working directory at the FTP server. The FTP's command used 
		/// for that is <b>CWD</b>.
		/// <b>BeginChangeDirectory</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginChangeDirectory</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndChangeDirectory">EndChangeDirectory</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginChangeDirectory</b>; if the asynchronous
		/// call has not completed, <b>EndChangeDirectory</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginChangeDirectory(int timeout, 
			string path,
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdCWD = new Cmd_Single(this);
						_currentCmd = _cmdCWD;
					}
				}
				CheckDisposed();

				return _cmdCWD.BeginExecute(timeout,
					"CWD " + path,
					callback,
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdCWD = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdCWD = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_currentCmd = null;
				_cmdCWD = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending asynchronous change working directory.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>EndChangeDirectory</b>
		/// method completes the asynchronous change working directory command 
		/// started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginChangeDirectory">BeginChangeDirectory</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginChangeDirectory">BeginChangeDirectory</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndChangeDirectory</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public void EndChangeDirectory(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdCWD.ARType, "EndChangeDirectory");
			try
			{
				FtpResponse response = _cmdCWD.EndExecute(asyncResult);
				CheckCompletionResponse(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdCWD = null;
				SetProgress(false);
				CheckDisposed();
			}
		}
		#endregion

		#region ChangeDirectoryUp functions
		Cmd_Single _cmdCDUP = null;

		/// <summary>
		/// Change working directory to parent directory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>ChangeDirectoryUp</b> method synchronously changes
		/// the current working directory to the parent working directory
		/// at the FTP server. The FTP's command
		/// used for that is <b>CDUP</b>. <b>ChangeDirectoryUp</b> method 
		/// blocks until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public void ChangeDirectoryUp(int timeout)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdCDUP = new Cmd_Single(this);
						_currentCmd = _cmdCDUP;
					}
				}
				CheckDisposed();

				FtpResponse response = _cmdCDUP.Execute(timeout, "CDUP");
				CheckCompletionResponse(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdCDUP = null;
				SetProgress(false);
				CheckDisposed();
			}
		}

	
		/// <summary>
		/// Begins an asynchronous change working directory to parent
		/// directory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginChangeDirectoryUp</b> method starts an asynchronous 
		/// change working directory to the parent directory at the FTP server. 
		/// The FTP's command used 
		/// for that is <b>CDUP</b>.
		/// <b>BeginChangeDirectoryUp</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginChangeDirectoryUp</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndChangeDirectoryUp">EndChangeDirectoryUp</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginChangeDirectoryUp</b>; if the asynchronous
		/// call has not completed, <b>EndChangeDirectoryUp</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>		
		public IAsyncResult BeginChangeDirectoryUp(int timeout, 
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdCDUP = new Cmd_Single(this);
						_currentCmd = _cmdCDUP;
					}
				}
				CheckDisposed();

				return _cmdCWD.BeginExecute(timeout,
					"CDUP",
					callback,
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdCDUP = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdCDUP = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_currentCmd = null;
				_cmdCDUP = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending asynchronous change working directory
		/// to parent directory.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>EndChangeDirectoryUp</b>
		/// method completes the asynchronous change working directory to
		/// parent directory command started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginChangeDirectoryUp">BeginChangeDirectoryUp</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginChangeDirectoryUp">BeginChangeDirectoryUp</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndChangeDirectoryUp</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public void EndChangeDirectoryUp(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdCDUP.ARType, "EndChangeDirectoryUp");
			try
			{
				FtpResponse response = _cmdCDUP.EndExecute(asyncResult);
				CheckCompletionResponse(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdCDUP = null;
				SetProgress(false);
				CheckDisposed();
			}
		}
		#endregion

		#region DeleteDirectory functions
		Cmd_Single _cmdDeleteDir = null;

		/// <summary>
		/// Remove specified directory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Pathname of the directory to be removed.
		/// It should
		/// be absolute or relative to the current working directory.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>DeleteDirectory</b> method synchronously delete specified
		/// directory at the FTP server.
		/// The FTP's command
		/// used for that is <b>RMD</b>. <b>DeleteDirectory</b> method 
		/// blocks until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public void DeleteDirectory(int timeout, string path)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);


			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdDeleteDir = new Cmd_Single(this);
						_currentCmd = _cmdDeleteDir;
					}
				}
				CheckDisposed();

				string cmd = "RMD " + path;
				FtpResponse response = _cmdDeleteDir.Execute(timeout, cmd);
				CheckCompletionResponse(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdDeleteDir = null;
				SetProgress(false);
				CheckDisposed();
			}
		}


		/// <summary>
		/// Begins an asynchronous delete directory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Pathname of the directory to be removed.
		/// It should
		/// be absolute or relative to the current working directory.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginDeleteDirectory</b> method starts an asynchronous 
		/// removing of the specified directory at the FTP server. 
		/// The FTP's command used 
		/// for that is <b>RMD</b>.
		/// <b>BeginDeleteDirectory</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginDeleteDirectory</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndDeleteDirectory">EndDeleteDirectory</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginDeleteDirectory</b>; if the asynchronous
		/// call has not completed, <b>EndDeleteDirectory</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginDeleteDirectory(int timeout, 
			string path,
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdDeleteDir = new Cmd_Single(this);
						_currentCmd = _cmdDeleteDir;
					}
				}
				CheckDisposed();

				string cmd = "RMD " + path;

				return _cmdDeleteDir.BeginExecute(timeout,
					cmd,
					callback,
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdDeleteDir = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdDeleteDir = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_currentCmd = null;
				_cmdDeleteDir = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending asynchronous delete directory.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>EndDeleteDirectory</b>
		/// method completes the asynchronous delete directory command 
		/// started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginDeleteDirectory">BeginDeleteDirectory</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginDeleteDirectory">BeginDeleteDirectory</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndDeleteDirectory</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>		
		public void EndDeleteDirectory(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdDeleteDir.ARType, "EndDeleteDirectory");
			try
			{
				FtpResponse response = _cmdDeleteDir.EndExecute(asyncResult);
				CheckCompletionResponse(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdDeleteDir = null;
				SetProgress(false);
				CheckDisposed();
			}
		}
		#endregion

		#region CreateDirectory functions
		Cmd_Single _cmdCreateDir = null;

		/// <summary>
		/// Create directory on FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Pathname of the directory to be created.
		/// It should
		/// be absolute or relative to the current working directory.
		/// </param>
		/// 
		/// <returns>
		/// String that contains the absolute pathname of the 
		/// newly created directory.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>CreateDirectory</b> method synchronously create specified
		/// directory at the FTP server.
		/// The FTP's command
		/// used for that is <b>MKD</b>. <b>CreateDirectory</b> method 
		/// blocks until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public string CreateDirectory(int timeout, string path)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);

			string dirName = null;
			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdCreateDir = new Cmd_Single(this);
						_currentCmd = _cmdCreateDir;
					}
				}
				CheckDisposed();

				string cmd = "MKD " + path;
				FtpResponse response = _cmdCreateDir.Execute(timeout, cmd);
				CheckCompletionResponse(response);
				dirName = GetNameFor257Response(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdCreateDir = null;
				SetProgress(false);
				CheckDisposed();
			}
			return dirName;
		}


		/// <summary>
		/// Begins an asynchronous create directory on the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="path">
		/// Pathname of the directory to be created.
		/// It should
		/// be absolute or relative to the current working directory.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginCreateDirectory</b> method starts an asynchronous 
		/// create of the specified directory at the FTP server. 
		/// The FTP's command used 
		/// for that is <b>MKD</b>.
		/// <b>BeginCreateDirectory</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginCreateDirectory</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndCreateDirectory">EndCreateDirectory</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginCreateDirectory</b>; if the asynchronous
		/// call has not completed, <b>EndCreateDirectory</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>path</i> is null reference (<b>Nothing</b> in
		/// Visual Basic).
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public IAsyncResult BeginCreateDirectory(int timeout, 
			string path,
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			ThrowIfNull("path", path);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdCreateDir = new Cmd_Single(this);
						_currentCmd = _cmdCreateDir;
					}
				}
				CheckDisposed();

				string cmd = "MKD " + path;

				return _cmdCreateDir.BeginExecute(timeout,
					cmd,
					callback,
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdCreateDir = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdCreateDir = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_currentCmd = null;
				_cmdCreateDir = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending asynchronous create directory.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <returns>
		/// String that contains the absolute pathname of the 
		/// newly created directory.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>EndCreateDirectory</b>
		/// method completes the asynchronous create directory command 
		/// started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginCreateDirectory">BeginCreateDirectory</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginCreateDirectory">BeginCreateDirectory</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndCreateDirectory</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>		
		public string EndCreateDirectory(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdCreateDir.ARType, "EndCreateDirectory");
			string dirName = null;
			try
			{
				FtpResponse response = _cmdCreateDir.EndExecute(asyncResult);
				CheckCompletionResponse(response);
				dirName = GetNameFor257Response(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdCreateDir = null;
				SetProgress(false);
				CheckDisposed();
			}
			return dirName;
		}
		#endregion

		#region GetWorkingDirectory functions
		Cmd_Single _cmdPWD = null;

		/// <summary>
		/// Gets the pathname of the current working directory.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <returns>
		/// String that contains the absolute pathname of the 
		/// current working directory.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>GetWorkingDirectory</b> method synchronously 
		/// retrieve the current working directory at the FTP server.
		/// The FTP's command
		/// used for that is <b>PWD</b>. <b>GetWorkingDirectory</b> method 
		/// blocks until the operation is completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server or
		/// there is another operation is in progress.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public string GetWorkingDirectory(int timeout)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			string dirName = null;
			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdPWD = new Cmd_Single(this);
						_currentCmd = _cmdPWD;
					}
				}
				CheckDisposed();

				FtpResponse response = _cmdPWD.Execute(timeout, "PWD");
				CheckCompletionResponse(response);
				dirName = GetNameFor257Response(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdPWD = null;
				SetProgress(false);
				CheckDisposed();
			}
			return dirName;
		}


		/// <summary>
		/// Begins an asynchronous gets the pathname 
		/// of the current working directory at the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginGetWorkingDirectory</b> method starts an asynchronous 
		/// retrieve the pathname of the current working directory at the FTP
		/// server.
		/// The FTP's command used 
		/// for that is <b>PWD</b>.
		/// <b>BeginGetWorkingDirectory</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected or there is another operation is in progress.
		/// <b>BeginGetWorkingDirectory</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndGetWorkingDirectory">EndGetWorkingDirectory</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginGetWorkingDirectory</b>; if the asynchronous
		/// call has not completed, <b>EndGetWorkingDirectory</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>		
		public IAsyncResult BeginGetWorkingDirectory(int timeout, 
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			SetProgress(true);
			try
			{
				lock(this)
				{
					if(!_disposed)
					{
						_cmdPWD = new Cmd_Single(this);
						_currentCmd = _cmdPWD;
					}
				}
				CheckDisposed();

				return _cmdPWD.BeginExecute(timeout,
					"PWD",
					callback,
					state);
			}
			catch(FtpFatalErrorException)
			{
				_currentCmd = null;
				_cmdPWD = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				_currentCmd = null;
				_cmdPWD = null;
				SetProgress(false);
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				_currentCmd = null;
				_cmdPWD = null;
				SetProgress(false);
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending asynchronous gets the pathname of the
		/// current working directory at the FTP server.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <returns>
		/// String that contains the absolute pathname of the 
		/// current working directory.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>EndGetWorkingDirectory</b>
		/// method completes the asynchronous create directory command 
		/// started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginGetWorkingDirectory">BeginGetWorkingDirectory</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginGetWorkingDirectory">BeginGetWorkingDirectory</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndGetWorkingDirectory</b> was previously called for the 
		/// asynchronous read.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>		
		public string EndGetWorkingDirectory(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdPWD.ARType, "EndGetWorkingDirectory");
			string dirName = null;
			try
			{
				FtpResponse response = _cmdPWD.EndExecute(asyncResult);
				CheckCompletionResponse(response);
				dirName = GetNameFor257Response(response);
			}
			catch(FtpFatalErrorException)
			{
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				DisconnectInternal();
				throw;
			}
			finally
			{
				_currentCmd = null;
				_cmdPWD = null;
				SetProgress(false);
				CheckDisposed();
			}
			return dirName;
		}
		#endregion

		//finishing functions
		#region Abort functions
		Cmd_Abort _cmdAbort = null;

		/// <summary>
		/// Abort the data transfering operation.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>Abort</b> method aborts the data transfering
		/// operation.
		/// The FTP's command
		/// used for that is <b>ABOR</b>. <b>Abort</b> method 
		/// blocks until it completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server.
		/// 
		/// <para>
		/// If there is 
		/// operation which is transfering the data at the time
		/// the <b>Abort</b> method is called, then this operation
		/// will be finished with 
		/// <see cref="BytesRoad.Net.Ftp.FtpAbortedException">FtpAbortedException</see>
		/// exception.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>
		public void Abort(int timeout)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			try
			{
				_cmdAbort = new Cmd_Abort(this);
				_cmdAbort.Execute(timeout);
				_cmdAbort = null;
			}
			catch(FtpFatalErrorException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Begins an asynchronous abort of the
		/// data transfering operation.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginAbort</b> method starts an asynchronous 
		/// abort of the data transfering operation.
		/// The FTP's command used 
		/// for that is <b>ABOR</b>.
		/// <b>BeginAbort</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected.
		/// <b>BeginAbort</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndAbort">EndAbort</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginAbort</b>; if the asynchronous
		/// call has not completed, <b>EndAbort</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// If there is 
		/// operation which is transfering the data at the time
		/// the <b>BeginAbort</b> method is called, then this operation
		/// will be finished with 
		/// <see cref="BytesRoad.Net.Ftp.FtpAbortedException">FtpAbortedException</see>
		/// exception.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>		
		public IAsyncResult BeginAbort(int timeout,
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);

			try
			{
				_cmdAbort = new Cmd_Abort(this);
				return _cmdAbort.BeginExecute(timeout, callback, state);
			}
			catch(FtpFatalErrorException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Ends a pending asynchronous abort.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>EndAbort</b>
		/// method completes the asynchronous abort command 
		/// started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginAbort">BeginAbort</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginAbort">BeginAbort</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndAbort</b> was previously called for the 
		/// asynchronous operation.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpProtocolException">
		/// Violation of FTP protocol occurs. Connection with the
		/// FTP server will be terminated.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpErrorException">
		/// The FTP server returns negative response.
		/// </exception>		
		public void EndAbort(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdAbort.ARType, "EndAbort");
			try
			{
				_cmdAbort.EndExecute(asyncResult);
				_cmdAbort = null;
			}
			catch(FtpFatalErrorException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				CheckDisposed();
				throw;
			}
		}
		#endregion

		#region Reset function
		Cmd_Reset _cmdReset = null;

		/// <summary>
		/// Reset the data transfering operation.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>Reset</b> method reset the data transfering
		/// operation. No commands are sent to the FTP server,
		/// instead the data channel simply closed.
		/// <b>Reset</b> method 
		/// blocks until it completed or exception is thrown.
		/// Throw 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see> 
		/// exception if
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// is not connected to the FTP server.
		/// 
		/// <para>
		/// If there is 
		/// operation which is transfering the data at the time
		/// the <b>Reset</b> method is called, then this operation
		/// will be finished with 
		/// <see cref="BytesRoad.Net.Ftp.FtpAbortedException">FtpAbortedException</see>
		/// exception.
		/// </para>
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public void Reset(int timeout)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			try
			{
				_cmdReset = new Cmd_Reset(this);
				_cmdReset.Execute(timeout);
				_cmdReset = null;
			}
			catch(FtpFatalErrorException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				CheckDisposed();
				throw;
			}
		}


		/// <summary>
		/// Begins an asynchronous reset of the
		/// data transfering operation.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>BeginReset</b> method starts an asynchronous 
		/// reset of the data transfering operation.
		/// 
		/// No commands is sent to the FTP server,
		/// instead the data channel simply closed.
		/// 
		/// <b>BeginReset</b> will throw an 
		/// <see cref="System.InvalidOperationException">InvalidOperationException</see>
		/// exception if 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> is not
		/// connected.
		/// <b>BeginReset</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndReset">EndReset</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginReset</b>; if the asynchronous
		/// call has not completed, <b>EndReset</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// If there is 
		/// operation which is transfering the data at the time
		/// the <b>BeginReset</b> method is called, then this operation
		/// will be finished with 
		/// <see cref="BytesRoad.Net.Ftp.FtpAbortedException">FtpAbortedException</see>
		/// exception.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// Other operation is in progress.
		/// <para>-or-</para>
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>		
		public IAsyncResult BeginReset(int timeout, 
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			try
			{
				_cmdReset = new Cmd_Reset(this);
				return _cmdReset.BeginExecute(timeout, callback, state);
			}
			catch(FtpFatalErrorException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				CheckDisposed();
				throw;
			}
		}

		/// <summary>
		/// Ends a pending asynchronous reset.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>EndReset</b>
		/// method completes the asynchronous reset command 
		/// started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginReset">BeginReset</see>
		/// method.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
		/// exception will be thrown if one of the "atomic" operation
		/// was times out. The <b>FtpTimeoutException</b> as well as
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// and
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>
		/// are fatal exceptions. If one of them is thrown the connection
		/// with FTP server will be terminated. To continue work you need
		/// to establish connection again.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginReset">BeginReset</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndReset</b> was previously called for the 
		/// asynchronous operation.
		/// </exception>
		/// <exception cref="BytesRoad.Net.Ftp.FtpTimeoutException">
		/// One of the "atomic" operation was times out. Connection with
		/// the FTP server will be terminated. See the Remarks section 
		/// for more information.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>
		public void EndReset(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdReset.ARType, "EndReset");
			try
			{
				_cmdReset.EndExecute(asyncResult);
				_cmdReset = null;
			}
			catch(FtpFatalErrorException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch(SocketException)
			{
				CheckDisposed();
				DisconnectInternal();
				throw;
			}
			catch
			{
				CheckDisposed();
				throw;
			}
		}
		#endregion

		#region Disconnect functions

		Cmd_Disconnect _cmdDisconnect = null;

		/// <summary>
		/// Disconnect from the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <remarks>
		/// First, the <b>Disconnect</b> method resets the data transfering
		/// operation (if any) by calling 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Reset">Reset</see>
		/// method. Then FTP's <b>QUIT</b> command would be sent
		/// to gracefully close the control connection.
		/// <b>Disconnect</b> method blocks until the disconnect
		/// operation is completed. After <b>Disconnect</b>
		/// method is finished the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// instance moved to disconnected state.
		/// 
		/// <para>
		/// If there is 
		/// operation which is transfering the data at the time
		/// the <b>Disconnect</b> method is called, then this operation
		/// will be finished with 
		/// <see cref="BytesRoad.Net.Ftp.FtpAbortedException">FtpAbortedException</see>
		/// exception.
		/// </para>
		/// 
		/// <para>
		/// There are few exceptions which are meaningless in the context
		/// of <b>Disconnect</b> method. These exceptions are 
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>,
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>,
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see> and
		/// <see cref="BytesRoad.Net.Ftp.FtpErrorException">FtpErrorException</see>.
		/// So they would be swallowed and connection with FTP server would
		/// be terminated.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		public void Disconnect(int timeout)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			try
			{
				_cmdDisconnect = new Cmd_Disconnect(this);
				_cmdDisconnect.Execute(timeout);
				_cmdDisconnect = null;
			}
			catch(Exception)
			{
				//suppress any exceptions
				//
			}

			FtpControlConnection cc = _cc;
			if(null != cc)
			{
				cc.CommandSent -= new FtpControlConnection.CommandSentEventHandler(CC_CommandSent);
				cc.ResponseReceived -= new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);
				cc.Close();
				_cc = null;
			}
			_currentCmd = null;
			_currentDTP = null;
		}


		/// <summary>
		/// Begins an asynchronous disconnect 
		/// from the FTP server.
		/// </summary>
		/// 
		/// <param name="timeout">
		/// Time out period for each "atomic" operation 
		/// participating to complete the whole operation.
		/// Specify zero or
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>
		/// for no time out. See the Remarks section for more 
		/// information.
		/// </param>
		/// 
		/// <param name="callback">
		/// The <see cref="System.AsyncCallback">AsyncCallback</see> delegate.
		/// </param>
		/// <param name="state">
		/// An object containing state information for this operation.
		/// </param>
		/// 
		/// <returns>
		/// An <see cref="System.IAsyncResult"/> that references
		/// the asynchronous operation.
		/// </returns>
		/// 
		/// <remarks>
		/// The <b>Disconnect</b> method starts an asynchronous
		/// procedure of disconnecting from the FTP server. This
		/// procedure consist of few steps. First, resets any data 
		/// transfering operation by calling 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginReset">BeginReset</see>
		/// method and then send FTP's command <b>QUIT</b>
		/// to gracefully close the control connection.
		/// <b>BeginDisconnect</b> returns immediately and does not 
		/// wait for the asynchronous call to complete.
		/// 
		/// <para>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.EndDisconnect">EndDisconnect</see>
		/// method is used to retrieve the 
		/// results of the asynchronous call. It can be called any 
		/// time after <b>BeginDisconnect</b>; if the asynchronous
		/// call has not completed, <b>EndDisconnect</b> will block 
		/// until it completes.
		/// </para>
		/// 
		/// <para>
		/// If there is 
		/// operation which is transfering the data at the time
		/// the <b>BeginDisconnect</b> method is called, then this operation
		/// will be finished with 
		/// <see cref="BytesRoad.Net.Ftp.FtpAbortedException">FtpAbortedException</see>
		/// exception.
		/// </para>
		/// 
		/// <para>
		/// Note that the value of <i>timeout</i> parameter doesn't define the 
		/// period of time within which the operation should be completed, 
		/// instead it defines time out period for each "atomic" operation 
		/// participating to complete the whole operation. In practice
		/// it is possible that the whole operation may take a time which 
		/// is little shorter then the time specified by <i>timeout</i> parameter
		/// multiplied by the number of "atomic" operation.
		/// </para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// is not connected.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///	<i>timeout</i> is less then zero and not equals to
		/// <see cref="System.Threading.Timeout.Infinite">Timeout.Infinite</see>.
		/// </exception>
		/// <exception cref="System.Net.Sockets.SocketException">
		/// An error occurred when attempting to access
		/// the socket used to complete requested operation.
		/// Connection with the FTP server will be terminated.
		/// </exception>		
		public IAsyncResult BeginDisconnect(int timeout, 
			AsyncCallback callback,
			object state)
		{
			CheckReadyForCmd();
			timeout = GetTimeoutValue(timeout);
			IAsyncResult ar = null;

			try
			{
				_cmdDisconnect = new Cmd_Disconnect(this);
				return _cmdDisconnect.BeginExecute(timeout, callback, state);
			}
			catch
			{
				FtpControlConnection cc = _cc;
				if(null != cc)
				{
					cc.CommandSent -= new FtpControlConnection.CommandSentEventHandler(CC_CommandSent);
					cc.ResponseReceived -= new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);
					cc.Close();
					_cc = null;
				}

				ar = _cmdDisconnect.AsyncResult;
				_currentCmd = null;
				_currentDTP = null;
				_cmdDisconnect = null;
			}
			return ar;
		}


		/// <summary>
		/// Ends a pending asynchronous disconnect.
		/// </summary>
		/// 
		/// <param name="asyncResult">
		/// An 
		/// <see cref="System.IAsyncResult">IAsyncResult</see>
		/// that stores state information for 
		/// this asynchronous operation.
		/// </param>
		/// 
		/// <remarks>
		/// The <b>EndDisconnect</b>
		/// method completes the asynchronous disconnect operation
		/// started in the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginDisconnect">BeginDisconnect</see>
		/// method. After <b>EndDisconnect</b> method is finished the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// instance moved to disconnected state.
		/// 
		/// <para>
		/// There are few exceptions which are meaningless in the context
		/// of disconnect operation. These exceptions are 
		/// <see cref="System.Net.Sockets.SocketException">SocketException</see>,
		/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>,
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see> and
		/// <see cref="BytesRoad.Net.Ftp.FtpErrorException">FtpErrorException</see>.
		/// So they would be swallowed and connection with FTP server would
		/// be terminated.
		/// </para>
		/// </remarks>
		///
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// object was disposed.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		/// <i>asyncResult</i> is a null reference 
		/// (<b>Nothing</b> in Visual Basic).
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// <i>asyncResult</i> was not returned by a call to the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.BeginDisconnect">BeginDisconnect</see> 
		/// method.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <b>EndDisconnect</b> was previously called for the 
		/// asynchronous operation.
		/// </exception>
		public void EndDisconnect(IAsyncResult asyncResult)
		{
			AsyncBase.VerifyAsyncResult(asyncResult, _cmdDisconnect.ARType, "EndDisconnect");
			try
			{
				_cmdDisconnect.EndExecute(asyncResult);
				_cmdDisconnect = null;
			}
			catch
			{
				//suppress any exceptions
			}

			FtpControlConnection cc = _cc;
			if(null != cc)
			{
				cc.CommandSent -= new FtpControlConnection.CommandSentEventHandler(CC_CommandSent);
				cc.ResponseReceived -= new FtpControlConnection.ResponseReceivedEventHandler(CC_ResponseReceived);
				cc.Close();
				_cc = null;
			}
			_currentCmd = null;
			_currentDTP = null;
		}

		#endregion

		#region Events handlers
		private void DTP_DataTransfered(object sender, DataTransferedEventArgs e)
		{
			if(null != DataTransfered && _issueDataTransEvent)
				DataTransfered(this, e);
		}

		private void CC_CommandSent(object sender, CommandSentEventArgs e)
		{
			if(null != CommandSent)
				CommandSent(this, e);
		}

		private void CC_ResponseReceived(object sender, ResponseReceivedEventArgs e)
		{
			if(null != ResponseReceived)
				ResponseReceived(this, e);
		}

		private void OnNewFtpItem(object sender, NewFtpItemEventArgs e)
		{
			if(null != NewFtpItem)
				NewFtpItem(this, e);
		}

		#endregion

		#region Disposable pattern
		/// <summary>
		/// Frees resources used by the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// class.
		/// </summary>
		/// <remarks>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
		/// class finalizer calls the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Dispose">Dispose</see>
		/// method to free resources associated with the <b>FtpClient</b>
		/// object.
		/// </remarks>
		~FtpClient()
		{
			Dispose(false);
		}

		/// <overloads>
		/// Releases the resources used by the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>.
		/// </overloads>
		/// 
		/// <summary>
		/// Releases all resources used by the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>.
		/// </summary>
		/// <remarks>
		/// Call <b>Dispose</b> when you are finished using the
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>. 
		/// The <b>Dispose</b> method leaves the <b>FtpClient</b> in an 
		/// unusable state. After calling <b>Dispose</b>, you must release 
		/// all references to the <b>FtpClient</b> so the garbage collector
		/// can reclaim the memory that the <b>FtpClient</b> was occupying.
		/// </remarks>
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		/// <summary>
		/// Releases the unmanaged resources used by the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see> 
		/// and optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing">
		/// <b>true</b> to release both managed and unmanaged resources;
		/// <b>false</b> to release only unmanaged resources.
		/// </param>
		/// <remarks>
		/// This method is called by the public 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Dispose">Dispose()</see>
		/// method and the 
		/// <see cref="BytesRoad.Net.Ftp.FtpClient.Finalize">Finalize</see>
		/// method. <b>Dispose()</b> invokes the protected 
		/// <b>Dispose(Boolean)</b>
		/// method with the <i>disposing</i> parameter set to <b>true</b>.
		/// <b>Finalize</b> invokes <b>Dispose</b> with <i>disposing</i>
		/// set to <b>false</b>.
		/// When the <i>disposing</i> parameter is <b>true</b>,
		/// this method releases all resources held by any managed 
		/// objects that this <b>FtpClient</b> references. 
		/// This method invokes the <b>Dispose()</b> method of each 
		/// referenced object.
		/// </remarks>
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

					IDisposable curCmd = _currentCmd;
					if(null != curCmd)
						curCmd.Dispose();

					FtpControlConnection cc = _cc;
					if(null != cc)
						cc.Dispose();
				}
			}
		}
		#endregion
	}
}
