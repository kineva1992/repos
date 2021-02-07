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

namespace BytesRoad.Net.Ftp
{
	/// <summary>
	/// The exception that is thrown when a
	/// violation of FTP protocol occurs.
	/// </summary>
	/// 
	/// <remarks>
	/// The <b>FtpPtotocolException</b> may be thrown
	/// in case the data received from the FTP server
	/// doesn't conform FTP protocol specification
	/// as defined in RFC 959.  
	/// <para>
	/// For example, each response
	/// from the FTP server consist of the one or more 
	/// text lines where each line should be terminated
	/// with <b>CRLF</b> sequence. If this will be violated
	/// the <b>FtpProtocolException</b> exception will be
	/// thrown. In such case 
	/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException.Response">Response</see>, 
	/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException.Line">Line</see>
	/// and 
	/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException.Position">Position</see> 
	/// properties may be accessed to gather more information.
	/// </para>
	/// 
	/// <para>
	/// Another example when <b>FtpProtocolException</b> will be
	/// thrown is when the data is expected to came from
	/// the FTP server, but the connection is unexpectedly
	/// closed by the server. In such case there is nothing
	/// to do with
	/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException.Response">Response</see>, 
	/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException.Line">Line</see>
	/// and 
	/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException.Position">Position</see> 
	/// properties and they will be equals to
	/// <b>null</b>, <b>-1</b> and <b>-1</b> respectively.
	/// </para>
	/// <note>
	/// When <b>FtpProtocolException</b> is thrown 
	/// the connection with FTP server is terminated.
	/// To continue work you need to establish the 
	/// connection again.
	/// </note>
	/// </remarks>
	public class FtpProtocolException : FtpFatalErrorException
	{
		int _pos = -1;
		int _line = -1;
		FtpResponse _response = null;

		///<overloads>
		/// Initializes a new instance of the 
		/// <b>FtpFatalErrorException</b> class.
		/// </overloads>
		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// class.
		/// </summary>
		public FtpProtocolException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// class with the specified message.
		/// </summary>
		/// <param name="message">The error message string.</param>
		public FtpProtocolException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
		/// class with the specified message and additional information.
		/// </summary>
		/// <param name="message">The error message string.</param>
		/// <param name="respone">An instance of the
		/// <see cref="BytesRoad.Net.Ftp.FtpResponse">FtpResponse</see>
		/// class which represents the malformed response.
		/// </param>
		/// <param name="line">
		/// Index of malformed line in response.
		/// </param>
		/// <param name="pos">
		/// Position in line, where the error occurs.
		/// </param>
		public FtpProtocolException(string message, 
			FtpResponse respone, 
			int line,
			int pos) : base(message)
		{
			_response = Response;
			_line = line;
			_pos = pos;
		}

		/// <summary>
		/// Gets malformed response.
		/// </summary>
		/// <value>
		/// An instance of the
		/// <see cref="BytesRoad.Net.Ftp.FtpResponse">FtpResponse</see>
		/// class which represents the malformed response.
		/// </value>
		/// <remarks>
		/// In some cases this property may be meaningless and
		/// its value will be equal to <b>null</b>. For example when
		/// server unexpectedly close the connection.
		/// </remarks>
		public FtpResponse Response
		{
			get { return _response; }
		}

		/// <summary>
		/// Gets the index of malformed line in response.
		/// </summary>
		/// <remarks>
		/// In some cases this property may be meaningless and
		/// its value will be equal to <b>-1</b>. For example when
		/// server unexpectedly close the connection.
		/// </remarks>
		public int Line
		{
			get { return _line; }
		}

		/// <summary>
		/// Gets the position in line, where the error occurs.
		/// </summary>
		/// <remarks>
		/// In some cases this property may be meaningless and
		/// its value will be equal to <b>-1</b>. For example when
		/// server unexpectedly close the connection.
		/// </remarks>
		public int Position
		{
			get { return _pos; }
		}

		internal void SetResponse(FtpResponse r)
		{
			_response = r;
		}

		internal void SetLine(int line)
		{
			_line = line;
		}

		internal void SetPosition(int p)
		{
			_pos = p;
		}
	}
}
