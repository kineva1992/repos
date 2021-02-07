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

using BytesRoad.Diag;

namespace BytesRoad.Net.Ftp.Advanced
{
	/// <summary>
	/// Summary description for DTPStreamMem.
	/// </summary>
	internal class DTPStreamCommon : DTPStream
	{
		Stream _stream = null;
		DTPStreamType _type;
		long _available = long.MaxValue;

		internal DTPStreamCommon(Stream stream, DTPStreamType type)
		{
			_stream = stream;
			_type = type;
		}

		internal DTPStreamCommon(Stream stream, 
			DTPStreamType type, 
			long maxData)
		{
			_stream = stream;
			_type = type;

			if(maxData > 0)
				_available = maxData;
		}

		internal override DTPStreamType Type
		{
			get { return _type; }
		}

		internal override long AvailableSpace
		{
			get { return _available; }
		}

		internal override long AvailableData
		{
			get { return _available; }
		}

		#region Attributes
		public override bool CanRead { get { return _stream.CanRead; } }
		public override bool CanSeek { get { return _stream.CanSeek; } }
		public override bool CanWrite { get { return _stream.CanWrite; } }
		public override long Length {get { return _stream.Length; } }
		public override long Position 
		{
			get { return _stream.Position; }
			set { _stream.Position = value; }
		}
		#endregion

		#region Functions

		#region Read
		public override int Read(byte[] buffer, int offset, int size)
		{
			if(0 == _available)
			{
				string msg = "Trying to read after the end of the stream.";
				NSTrace.WriteLineError(msg);
				throw new EndOfStreamException(msg);
			}

			if(size > _available)
				size = (int)_available;

			int r = _stream.Read(buffer, offset, size);
			_available -= r;
			return r;
		}

		public override IAsyncResult BeginRead(byte[] buffer,
			int offset,
			int size,
			AsyncCallback callback,
			object state
			)
		{
			if(0 == _available)
			{
				string msg = "Trying to read after the end of the stream.";
				NSTrace.WriteLineError(msg);
				throw new EndOfStreamException(msg);
			}

			if(size > _available)
				size = (int)_available;

			return _stream.BeginRead(buffer, offset, size, callback, state);
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			int r = _stream.EndRead(asyncResult);
			_available -= r;
			return r;
		}

		#endregion

		#region Write
		public override void Write(byte[] buffer, int offset, int size)
		{
			if((0 == _available) || (size > _available))
			{
				string msg = "Trying to write after the end of the stream.";
				NSTrace.WriteLineError(msg);
				throw new EndOfStreamException(msg);
			}
		
			_stream.Write(buffer, offset, size);
			_available -= size;
		}

		public override IAsyncResult BeginWrite(byte[] buffer,
			int offset,
			int size,
			AsyncCallback callback,
			object state
			)
		{
			if((0 == _available) || (size > _available))
			{
				string msg = "Trying to write after the end of the stream.";
				NSTrace.WriteLineError(msg);
				throw new EndOfStreamException(msg);
			}
		
			IAsyncResult ar = _stream.BeginWrite(buffer, offset, size, callback, state);
			_available -= size;
			return ar;
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			_stream.EndWrite(asyncResult);
		}
		#endregion

		public override void Close()
		{
			_stream.Close();
		}

		public override void Flush()
		{
			_stream.Flush();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return _stream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			_stream.SetLength(value);
		}
		#endregion
	}
}
