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
using System.Text;

using BytesRoad.Net;
using BytesRoad.Net.Ftp.Advanced;

namespace BytesRoad.Net.Ftp
{
	internal enum FtpResponseLineType
	{
		Unspecified,
		First,
		Intermediate,
		Single
	}

	internal enum FtpResponseLineState
	{
		NotCompleted,
		Completed,
		Invalid
	}

	/// <summary>
	/// Summary description for FtpResponseLine.
	/// </summary>
	internal class FtpResponseLine
	{
		FtpResponseLineType _type = FtpResponseLineType.Unspecified;
		LineInfo _lineInfo = null;
		int _code = -1;

		internal FtpResponseLine(LineInfo lineInfo)
		{
			_lineInfo = lineInfo;
			ResolveLineType();
		}

		#region ATTRIBUTES
		internal int Code
		{
			get { return _code; }
		}

		internal FtpResponseLineType Type
		{
			get { return _type; }
		}
	
		/// <summary>
		/// 
		/// </summary>
		/// <exception cref="System.OutOfMemoryException"/>
		internal ByteVector Content
		{
			get 
			{ 
				if(null != _lineInfo)
					return _lineInfo.Content;
				return null; 
			}
		}
		#endregion

		void ResolveLineType()
		{
			_type = FtpResponseLineType.Intermediate;
			byte[] content = _lineInfo.Content.Data;

			if(content.Length > 3)
			{
				byte byte0 = (byte)content[0];
				byte byte1 = (byte)content[1];
				byte byte2 = (byte)content[2];

				//check for the first three digits
				bool firstIsDigits = char.IsDigit((char)byte0);
				firstIsDigits = firstIsDigits && char.IsDigit((char)byte1);
				firstIsDigits = firstIsDigits && char.IsDigit((char)byte2);

				//if all first three is digits then either it 'first' or 'single'
				//
				if(true == firstIsDigits)
				{
					if((byte)content[3] == (byte)'-') //check for 'first'
						_type = FtpResponseLineType.First;
					else
						_type = FtpResponseLineType.Single;

					//extract code
					//
					_code = byte0 - (byte)'0';
					_code = 10*_code + byte1 - (byte)'0';
					_code = 10*_code + byte2 - (byte)'0';
				}
			}
		}
	}

}

