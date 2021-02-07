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

namespace BytesRoad.Net.Ftp.Advanced
{
	internal class NewLineEventArgs : EventArgs
	{
		LineInfo _line;
		int _index;

		internal NewLineEventArgs(LineInfo line, int index)
		{
			_line = line;
			_index = index;
		}

		internal LineInfo Line
		{
			get { return _line; }
		}

		internal int Index
		{
			get { return _index; }
		}
	}


	internal enum  LineInfoState
	{
		NotCompleted,
		Completed,
		Invalid
	}

	internal class LinesBuilder
	{
		int _maxLineLength = -1; 

		ByteVector _workBuffer = new ByteVector();
		ArrayList _lines = null;
		bool _singleLineMode = false;
		bool _invalidState = false;

		internal LinesBuilder(int maxLineLength)
		{
			_maxLineLength = maxLineLength;
		}

		#region Events
		void OnNewLine(LineInfo line, int index)
		{
			if(NewLineEvent != null)
				NewLineEvent(this, new NewLineEventArgs(line, index));
		}

		internal delegate void NewLineEventHandler(object sender, NewLineEventArgs e);
		internal event NewLineEventHandler NewLineEvent;
		#endregion

		#region Attributes

		internal int MaxLineLength
		{
			get { return _maxLineLength; }
		}

		internal int Available
		{
			get { return _workBuffer.Size; }
		}

		internal LineInfo[] Lines
		{
			get
			{
				LineInfo[] ret = null;
				if(null != _lines)
				{
					int count = _lines.Count;
					if(count > 0)
					{
						ret = new LineInfo[count];
						_lines.CopyTo(0, ret, 0, count);
					}
				}
				return ret;
			}
		}
		internal bool SingleLineMode
		{
			get { return _singleLineMode; }
			set { _singleLineMode = value; }
		}
		#endregion

		#region Protected
		void MoveToNextLine()
		{
			_lines.Add(null);
		}

		int GetCurrentLineIndex()
		{
			if(null == _lines)
				return -1;

			return _lines.Count - 1;
		}

		LineInfo GetCurrentLine()
		{
			if(null == _lines)
			{
				_lines = new ArrayList();
				_lines.Add(null);
			}

			int curLine = GetCurrentLineIndex();
			LineInfo line = (LineInfo)_lines[curLine];
			if(null == line)
			{
				line = new LineInfo(_maxLineLength);
				_lines[curLine] = line;
			}
			return line;
		}
		#endregion

		#region internal
		internal void Reset()
		{
			_workBuffer = new ByteVector();
			_lines = null;
			_invalidState = false;
		}
		internal void PutData(byte[] data)
		{
			PutData(data, data.Length);
		}

		internal void PutData(byte[] data, int size)
		{
			PutData(data, size, true);
		}

		internal void PutData(byte[] data, int size, bool parseIt)
		{
			if(true == _invalidState)
				throw new InvalidOperationException(this.GetType().FullName + " is in invalid state.");

			if(null == data)
				throw new ArgumentNullException("data", "The 'data' cannot be null.");

			if(0 == data.Length)
				throw new ArgumentException("The 'data' cannot be empty.", "data");


			_workBuffer.Add(data, 0, size);
			if(parseIt)
				ParseExistentData();
		}
		
		internal void ParseExistentData()
		{
			if(0 == _workBuffer.Size)
				return;

			DataChunk[] chunks = DataChunk.ParseData(_workBuffer.Data, _workBuffer.Size);
			int chunksNum = chunks.Length, i = 0;
			try
			{
				for(;i < chunksNum;i++)
				{
					LineInfo line = GetCurrentLine();
					if(LineInfoState.Completed == line.State)
					{
						MoveToNextLine();
						line = GetCurrentLine();
					}


					try
					{
						line.PutChunk(_workBuffer.Data, chunks[i]);
					}
					catch(LineException e)
					{
						e.Line = GetCurrentLineIndex();
						throw;
					}

					if(LineInfoState.Completed == line.State)
					{
						OnNewLine(line, GetCurrentLineIndex());
						if(true == SingleLineMode && (i != chunksNum-1))
							break;
					}
				}
			}
			catch
			{
				_invalidState = true;
				throw;
			}

			if(chunksNum == i) //all working data was parsed?
				_workBuffer.Clear();
			else
				_workBuffer.CutHead(chunks[i].Start + chunks[i].Length);
		}

		internal void EndData()
		{
			LineInfo line = GetCurrentLine();
			if(LineInfoState.NotCompleted == line.State)
				OnNewLine(line, GetCurrentLineIndex());
			Reset();
		}

		internal void ClearCompleted()
		{
			if(null != _lines)
			{
				int lastCompleted = -1;
				for(int i=0;i<_lines.Count;i++)
				{
					if(LineInfoState.Completed == ((LineInfo)_lines[i]).State)
						lastCompleted = i;
					else
						break;
				}

				if(lastCompleted >= 0)
				{
					if(_lines.Count - 1  == lastCompleted)
						_lines = null;
					else
						_lines.RemoveRange(0, lastCompleted);
				}
			}
		}
		#endregion
	}

	#region Lines exceptions
	internal class LineException : Exception
	{
		int _line = -1;

		internal LineException()
		{
		}

		internal LineException(string message) : base(message)
		{
		}

		internal int Line
		{
			get { return _line; }
			set { _line = value; }
		}
	}

	internal class LineLengthExceededException : LineException
	{
		internal LineLengthExceededException()
		{}

		internal LineLengthExceededException(string message) 
			: base(message)
		{}
	}

	internal class LineFormatException : LineException
	{
		int _position = -1;

		internal LineFormatException(string message) : base(message)
		{
		}

		internal int Position
		{
			get { return _position; }
			set { _position = value; }
		}
	}
	#endregion

	internal class LineInfo
	{
		ByteVector _content = new ByteVector();
		LineInfoState _state = LineInfoState.NotCompleted;
		bool _seekLF = false;
		bool _requiredCR = true;
		int _maxLineLength = -1;

		internal LineInfo(int maxLineLength)
		{
			_maxLineLength = maxLineLength;
		}

		#region Attributes
		internal LineInfoState State
		{
			get { return _state; }
		}

		internal bool RequiredCR
		{
			get { return _requiredCR; }
			set { _requiredCR = value; }
		}

		internal ByteVector Content
		{
			get { return _content; }
		}
		#endregion

		void ThrowFormatException(string expected, string found)
		{
			_state = LineInfoState.Invalid;
			string msg = string.Format("Expected: '{0}'. '{1}' found instead.", expected, found);
			LineFormatException e = new LineFormatException(msg);
			e.Position = _content.Size;
		}

		void AppendToContent(byte[] data, DataChunk chunk)
		{
			_content.Add(data, chunk.Start, chunk.Length);
		}

		void CheckMaxLineLength(int newChunkLength)
		{
			int curLength = (null == _content)?0:_content.Size;
			if(newChunkLength + curLength >= _maxLineLength)
			{
				_state = LineInfoState.Invalid;
				throw new LineLengthExceededException();
			}
		}

		internal void PutChunk(byte[] data, DataChunk chunk)
		{
			if(LineInfoState.Invalid == State)
				throw new InvalidOperationException("Invalid line cannot be modified.");

			CheckMaxLineLength(chunk.Length);
			if(false == _seekLF)
			{
				if(DataChunkType.CR == chunk.Type)
					_seekLF = true;
				else
				{
					if(DataChunkType.LF == chunk.Type)
					{
						if(_requiredCR)
							ThrowFormatException("<CR> or <Data>", chunk.ToString());
						else
							_state = LineInfoState.Completed;

					}
				}
				AppendToContent(data, chunk);
			}
			else
			{
				//Missing LF, let's handle it...

				if(DataChunkType.LF != chunk.Type)
					ThrowFormatException("<LF>", chunk.ToString());

				AppendToContent(data, chunk);
				_state = LineInfoState.Completed;
			}
		}
	}

	#region CHUNKS STUFF
	enum DataChunkType
	{
		Data, 
		CR,
		LF
	}

	class DataChunk
	{
		const int _CR = 13;
		const int _LF = 10;

		internal readonly DataChunkType Type;
		internal readonly int Start;
		internal readonly int Length;

		internal DataChunk(int start, int length, DataChunkType t)
		{
			Start = start;
			Length = length;
			Type = t;
		}

		public override string ToString()
		{
			switch(Type)
			{
				case DataChunkType.CR: return "<CR>";
				case DataChunkType.LF: return "<LF>";
				case DataChunkType.Data: return "<Data>";
			}
			return "<Unknown>";
		}

		static internal DataChunk[] ParseData(byte []data)
		{
			return ParseData(data, data.Length);
		}

		static internal DataChunk[] ParseData(byte []data, int count)
		{
			ArrayList chunks = new ArrayList(1);
			for(int pos=0; pos < count;)
			{
				byte c = data[pos];
				if(_CR == c)
					chunks.Add(new DataChunk(pos++, 1, DataChunkType.CR));
				else if(_LF == c)
					chunks.Add(new DataChunk(pos++, 1, DataChunkType.LF));
				else
				{
					int dataStart = pos;
					while((pos < count) && (_CR != c) && (_LF != c))
						c = data[pos++];

					if((_CR == c) || (_LF == c))
						pos--;

					chunks.Add(new DataChunk(dataStart, pos - dataStart, DataChunkType.Data));
				}
			}

			if(0 < chunks.Count)
			{
				DataChunk[] retChunks = new DataChunk[chunks.Count];
				chunks.CopyTo(retChunks);
				return retChunks;
			}
			return null;
		}
	}
	#endregion
}
