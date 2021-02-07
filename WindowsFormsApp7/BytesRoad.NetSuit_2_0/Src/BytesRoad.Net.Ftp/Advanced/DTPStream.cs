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
using System.IO;

namespace BytesRoad.Net.Ftp.Advanced
{
	internal enum DTPStreamType
	{
		ForReading,
		ForWriting
	}

	internal abstract class DTPStream : Stream
	{
		internal DTPStream()
		{
		}
		

		//in case there is no limitation on reading amount
		//this propery should return long.MaxValue
		abstract internal long AvailableData { get; }

		//in case there is no limitation on writing amount
		//this propery should return long.MaxValue
		abstract internal long AvailableSpace { get; }

		//get the type of the stream
		abstract internal DTPStreamType Type { get; }
	}
}
