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
	/// it is inpossible to start downloading from
	/// the middle of the file.
	/// </summary>
	/// <remarks>
	/// To start downloading from the middle of the file the <b>REST</b>
	/// FTP's command sent first and if the server's response to this
	/// command will be negative the <b>FtpRestartNotSupportedException</b>
	/// exception is thrown.
	/// 
	/// <para>
	/// An instance of the 
	/// <see cref="BytesRoad.Net.Ftp.FtpResponse">FtpResponse</see> 
	/// class, which represents the
	/// negative response, may be accessed via
	/// <see cref="BytesRoad.Net.Ftp.FtpErrorException.Response">Response</see>
	/// property. Value of the response's code
	/// stored in the 
	/// <see cref="BytesRoad.Net.Ftp.FtpErrorException.Code">Code</see>
	/// property.
	/// </para>
	/// <para>
	/// When the <b>FtpRestartNotSupportedException</b>
	/// is thrown, the connection with FTP server stay
	/// alive and you may continue work without reconnecting.
	/// </para>
	/// </remarks>
	public class FtpRestartNotSupportedException : FtpErrorException
	{
		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpRestartNotSupportedException">FtpRestartNotSupportedException</see>
		/// class with the specified message and additional information.
		/// </summary>
		/// <param name="message">The error message string.</param>
		/// <param name="response">
		/// An instance of the
		/// <see cref="BytesRoad.Net.Ftp.FtpResponse">FtpResponse</see>
		/// class which represents the negative response.
		/// </param>
		public FtpRestartNotSupportedException(string message, 
			FtpResponse response) : base(message, response)
		{
		}
	}
}
