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

using BytesRoad.Net.Ftp;

namespace BytesRoad.Net.Ftp
{
	/// <summary>
	/// The exception that is thrown when a
	/// negative response returned by the FTP server. 
	/// </summary>
	/// <remarks>
	/// The <b>FtpErrorException</b> exception may be thrown in many
	/// cases. For example, when you are trying to delete file by calling 
	/// <see cref="BytesRoad.Net.Ftp.FtpClient.DeleteFile">DeleteFile</see>,
	/// the <b>FtpErrorException</b> will be thrown
	/// if you do not have a rights to delete the files.
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
	/// When the <b>FtpErrorException</b> (or one of its descendants)
	/// is thrown, the connection with FTP server stay
	/// alive and you may continue work without reconnecting.
	/// </para>
	/// </remarks>
	public class FtpErrorException : Exception
	{
		FtpResponse _response = null;

		/// <summary>
		/// </summary>
		/// <exclude/>
		internal protected FtpErrorException() //we need this constructor for FtpAbortException
		{
		}

		/// <summary>
		/// Initializes a new instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpErrorException">FtpErrorException</see>
		/// class with the specified message and additional information.
		/// </summary>
		/// <param name="message">The error message string.</param>
		/// <param name="response">
		/// An instance of the
		/// <see cref="BytesRoad.Net.Ftp.FtpResponse">FtpResponse</see>
		/// class which represents the negative response.
		/// </param>
		public FtpErrorException(string message, FtpResponse response) : base(message)
		{
			_response = response;
		}

		/// <summary>
		/// Gets the negative response.
		/// </summary>
		/// <value>
		/// An instance of the
		/// <see cref="BytesRoad.Net.Ftp.FtpResponse">FtpResponse</see>
		/// class which represents the negative response.
		/// </value>
		public FtpResponse Response
		{
			get { return _response; }
		}

		/// <summary>
		/// Gets the negative response's code.
		/// </summary>
		public int Code
		{
			get { return _response.Code; }
		}

		internal void SetResponse(FtpResponse r)
		{
			_response = r;
		}
	}
}
