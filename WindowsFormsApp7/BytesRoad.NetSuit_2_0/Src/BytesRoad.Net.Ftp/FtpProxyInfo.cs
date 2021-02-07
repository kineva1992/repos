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
using System.Text;
using System.Net;

namespace BytesRoad.Net.Ftp
{
	/// <summary>
	/// Specifies the type of the proxy server an 
	/// instance of the 
	/// <see cref="BytesRoad.Net.Ftp.FtpProxyInfo">FtpProxyInfo</see>
	/// class describes.
	/// </summary>
	public enum FtpProxyType
	{
		/// <summary>
		/// Specify SOCKS4 proxy server.
		/// </summary>
		Socks4,

		/// <summary>
		/// Specify SOCKS4a proxy server.
		/// </summary>
		Socks4a,

		/// <summary>
		/// Specify SOCKS5 proxy server. 
		/// </summary>
		Socks5,

		/// <summary>
		/// Specify Web proxy server with support for TCP tunelling.
		/// </summary>
		HttpConnect
	}

	/// <summary>
	/// Contains proxy settings for the 
	/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
	/// class.
	/// </summary>
	/// <remarks>
	/// The <b>FtpProxyInfo</b> class contains proxy settings
	/// that <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
	/// instances use to communicate with FTP server.
	/// </remarks>
	public class FtpProxyInfo
	{
		FtpProxyType _type = FtpProxyType.Socks4;
		string _server = null;
		int _port = -1;
		byte[] _user = null;
		byte[] _password = null;
		bool _preAuthenticate = true;

		Encoding _encoding = Encoding.Default;

		/// <summary>
		/// Initializes an empty instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpProxyInfo">
		/// FtpProxyInfo</see> class.
		/// </summary>
		public FtpProxyInfo()
		{
		}

		#region Attributes
		internal byte[] UserBytes
		{
			get { return _user; }
		}

		internal byte[] PasswordBytes
		{
			get { return _password; }
		}

		/// <summary>
		/// Gets or sets the encoding instance.
		/// </summary>
		/// <value>
		/// The <see cref="System.Text.Encoding">Encoding</see> instance
		/// used for converting string to bytes array and vice versa. 
		/// Default value is <see cref="System.Text.Encoding.Default">
		/// Encoding.Default</see>.
		/// </value>
		/// <exception cref="System.ArgumentNullException">
		/// UsedEncoding is set to null 
		/// reference (<b>Nothing</b> in Visual Basic).
		/// </exception>
		public Encoding UsedEncoding 
		{
			get { return _encoding; }
			set
			{
				if(null == value)
					throw new ArgumentNullException("UsedEncoding", "Value cannot be null.");
				_encoding = value;
			}
		}

		/// <summary>
		/// Gets or sets the type of the proxy server.
		/// </summary>
		/// <value>
		/// The type of the proxy server. Default value is 
		/// <see cref="BytesRoad.Net.Ftp.FtpProxyType.Socks4">FtpProxyType.Socks4</see>.
		/// </value>
		public FtpProxyType Type
		{
			get { return _type; }
			set { _type = value; }
		}


		/// <summary>
		/// Gets or sets the host name of the proxy server.
		/// </summary>
		/// <value>
		/// The host name of the proxy server.
		/// </value>
		public string Server
		{
			get { return _server; }
			set { _server = value; }
		}

		/// <summary>
		/// Gets or sets the port number of the proxy server.
		/// </summary>
		/// <value>
		/// An integer value in the range 
		/// <see cref="System.Net.IPEndPoint.MinPort">IPEndPoint.MinPort</see>
		/// to
		/// <see cref="System.Net.IPEndPoint.MaxPort">IPEndPoint.MaxPort</see>
		/// indicating the TCP port number of the proxy server.
		/// </value>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// Value is less than 
		/// <see cref="System.Net.IPEndPoint.MinPort">IPEndPoint.MinPort</see>
		///  or greater then 
		///  <see cref="System.Net.IPEndPoint.MaxPort">IPEndPoint.MaxPort</see>.
		/// </exception>
		public int Port
		{
			get { return _port; }
			set 
			{ 
				if(value < IPEndPoint.MinPort || value > IPEndPoint.MaxPort)
					throw new ArgumentOutOfRangeException("Port", "Value specified is out of valid range."); 
				_port = value; 
			}
		}

		/// <summary>
		/// Gets or sets the user name to use during proxy authentication.
		/// </summary>
		public string User
		{
			get
			{ 
				if(null == _user)
					return null;
				return _encoding.GetString(_user);
			}

			set
			{
				if(null == value)
				{
					_user = null;
				}
				else
				{
					_user = _encoding.GetBytes(value);
				}
			}
		}


		/// <summary>
		/// Gets or sets the password to use during proxy authentication.
		/// </summary>
		public string Password
		{
			get
			{
				if(null == _password)
					return null;
				return _encoding.GetString(_password);
			}
			set
			{
				if(null == value)
					_password = null;
				else
					_password = _encoding.GetBytes(value);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to preauthenticate
		/// a connection with proxy server.
		/// </summary>
		/// <value>
		/// <b>true</b>, if you want preauthenticate 
		/// connection with proxy server; otherwise, <b>false</b>.
		/// Default value is <b>true</b>.
		/// </value>
		/// <remarks>
		/// Preauthentication allow clients to improve server 
		/// efficiency by avoiding extra round trips caused 
		/// by authentication challenges. This property takes effect
		/// for <b>Socks5</b> and <b>HttpConnect</b> proxy servers.
		/// </remarks>
		public bool PreAuthenticate
		{
			get { return _preAuthenticate; }
			set { _preAuthenticate = value; }
		}

		#endregion
	}
}
