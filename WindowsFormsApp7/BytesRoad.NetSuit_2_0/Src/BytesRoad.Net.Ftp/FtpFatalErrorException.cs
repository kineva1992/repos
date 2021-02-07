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

namespace BytesRoad.Net.Ftp
{
	/// <summary>
	/// The exception that is thrown when a
	/// error during communication with FTP server occurs.
	/// </summary>
	/// <remarks>
	/// Usually, methods of the 
	/// <see cref="BytesRoad.Net.Ftp.FtpClient">FtpClient</see>
	/// class are not throws the <b>FtpFatalErrorException</b>,
	/// one of the derived exceptions -
	/// <see cref="BytesRoad.Net.Ftp.FtpProtocolException">FtpProtocolException</see>
	/// or
	/// <see cref="BytesRoad.Net.Ftp.FtpTimeoutException">FtpTimeoutException</see>
	/// is thrown instead.
	/// <note>
	/// When <b>FtpFatalErrorException</b> is thrown (or one of its descendants)
	/// the connection with FTP server is terminated.
	/// To continue work you need to establish the 
	/// connection again.
	/// </note>
	/// </remarks>
	public class FtpFatalErrorException : Exception
	{
		///<overloads>
		/// Initializes a new instance of the 
		/// <b>FtpFatalErrorException</b> class.
		/// </overloads>
		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpFatalErrorException">FtpFatalErrorException</see>
		/// class.
		/// </summary>
		public FtpFatalErrorException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpFatalErrorException">FtpFatalErrorException</see>
		/// class with the specified message.
		/// </summary>
		/// <param name="message">The error message string.</param>
		public FtpFatalErrorException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpFatalErrorException">FtpFatalErrorException</see>
		/// class with a specified error message 
		/// and a reference to the inner exception 
		/// that is the cause of this exception.
		/// </summary>
		/// <param name="message">The error message string.</param>
		/// <param name="innerException">
		/// The exception that is the cause of the 
		/// current exception. If the <i>innerException</i>
		/// parameter is not a null reference (<b>Nothing</b>
		/// in Visual Basic), the current exception 
		/// is raised in a <b>catch</b> block that handles 
		/// the inner exception.
		/// </param>
		public FtpFatalErrorException(string message, Exception innerException)
			: base (message, innerException)
		{
		}
	}
}
