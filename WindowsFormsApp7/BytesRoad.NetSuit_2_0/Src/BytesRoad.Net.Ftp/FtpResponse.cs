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

using BytesRoad.Net.Ftp.Advanced;
using BytesRoad.Diag;

namespace BytesRoad.Net.Ftp
{
	internal enum FtpResponseState
	{
		NotCompleted,
		Intermediate,
		Completed,
		FormatError
	}

	#region State classes
	abstract class FtpResponseCurrent
	{
		abstract internal void OnNewResponseLine(FtpResponse ctx, FtpResponseLine line);
		abstract internal FtpResponseState GetState();
	}

	class FtpResponseNotCompleted : FtpResponseCurrent
	{
		override internal FtpResponseState GetState()
		{
			return FtpResponseState.NotCompleted;
		}

		override internal void OnNewResponseLine(FtpResponse ctx, FtpResponseLine line)
		{
			if(FtpResponseLineType.Single == line.Type)
			{
				ctx.ChangeState(new FtpResponseCompleted());
			}
			else if(FtpResponseLineType.First == line.Type)
			{
				ctx.ChangeState(new FtpResponseIntermediate());
			}
			else
			{
				ctx.ChangeState(new FtpResponseFormatError());
				throw new FtpProtocolException("Unexpected response line received.", null, -1, -1);
				//throw new FtpResponseFormatException("Unexpected respone line received.", ctx.CurrentLineNumber);
			}
		}
	}

	class FtpResponseIntermediate : FtpResponseCurrent
	{
		override internal FtpResponseState GetState()
		{
			return FtpResponseState.Intermediate;
		}

		override internal void OnNewResponseLine(FtpResponse ctx, FtpResponseLine line)
		{
			if(FtpResponseLineType.Single == line.Type)
			{
				if(line.Code != ctx.Code) //first code and the last should be the equal
				{
					ctx.ChangeState(new FtpResponseFormatError());
					//throw new FtpResponseFormatException("First and last line reply codes is not equals.", ctx.CurrentLineNumber);
					throw new FtpProtocolException("First and last line response's codes are not equals.", null, -1, -1);
				}
				else
					ctx.ChangeState(new FtpResponseCompleted());
			}
			else if(FtpResponseLineType.Intermediate == line.Type)
			{
				//noop
			}
			else if(FtpResponseLineType.First == line.Type)
			{
				//noop
			}
			else
			{
				ctx.ChangeState(new FtpResponseFormatError());
				throw new FtpProtocolException("Unexpected response line received.", null, -1, -1);
			}
		}
	}

	class FtpResponseCompleted : FtpResponseCurrent
	{
		override internal FtpResponseState GetState()
		{
			return FtpResponseState.Completed;
		}

		override internal void OnNewResponseLine(FtpResponse ctx, FtpResponseLine line)
		{
			throw new InvalidOperationException("Completed response can't be modified");
		}
	}

	class FtpResponseFormatError : FtpResponseCurrent
	{
		override internal FtpResponseState GetState()
		{
			return FtpResponseState.FormatError;
		}

		override internal void OnNewResponseLine(FtpResponse ctx, FtpResponseLine line)
		{
			throw new InvalidOperationException("Invalid response can't be modified");
		}
	}
	#endregion

	/// <summary>
	/// Represents a response from the FTP server.
	/// </summary>
	/// <remarks>
	/// Every command sent to FTP server must generate
	/// at least one response. An FTP response consists
	/// of a one or more lines of text. The first line
	/// contain three digit numbers (the response's code)
	/// followed by some text. The response's code
	/// denotes whether the response is good, bad or incomplete.
	/// Each response falls into one of the five groups,
	/// depending of the response's code:
	///  <list type="bullet">
	///  <item>
	///  <term>[100-199]</term><description>Positive Preliminary response</description>
	///  </item>
	///  <item>
	///  <term>[200-299]</term><description>Positive Completion response</description>
	///  </item>
	///  <item>
	///  <term>[300-399]</term><description>Positive Intermediate response</description>
	///  </item>
	///  <item>
	///  <term>[400-499]</term><description>Transient Negative Completion response</description>
	///  </item>
	///  <item>
	///  <term>[500-599]</term><description>Permanent Negative Completion response</description>
	///  </item>
	///  </list>
	///  More specific information regarding each group is
	///  described in RFC 959 (File Transfer Protocol).
	/// </remarks>
	public class FtpResponse
	{
		FtpResponseCurrent _current = new FtpResponseNotCompleted();

		ByteVector _rawData = new ByteVector();
		ArrayList _lines = null;
		int _code = -2;
		Encoding _encoding = null;
		static char[] crlfChars = new char[]{'\r', '\n'};

		internal FtpResponse(Encoding en)
		{
			if(null == en)
				throw new ArgumentNullException("en", "FtpResponse requires non null encoding.");

			_encoding = en;
		}

		#region Helpers
		static bool IsInRange(int val, int min, int max)
		{
			if(val>min && val<max)
				return true;
			return false;
		}
		#endregion

		#region Attributes

		/// <summary>
		/// Gets the response from the FTP server
		/// as an array of bytes.
		/// </summary>
		public byte[] RawBytes
		{
			get 
			{
				byte[] data = _rawData.Data;
				int size = _rawData.Size;
				byte[] ret = new byte[size];
				Array.Copy(data, 0, ret, 0, size);
				return ret;
			}
		}


		/// <summary>
		/// Gets the response from the FTP server
		/// as a single string.
		/// </summary>
		public string RawString
		{
			get
			{
				if(_rawData.Size > 0)
				{
					string ret = _encoding.GetString(_rawData.Data, 0, _rawData.Size);
					return ret.TrimEnd(crlfChars);
				}
				return null;
			}
		}


		/// <summary>
		/// Gets the response from the FTP server
		/// as a set of strings.
		/// </summary>
		public string[] RawStrings
		{
			get
			{
				string[] ret = null;
				if((null != _lines) && (_lines.Count > 0))
				{
					int count = _lines.Count;
					ret = new string[count];
					for(int i=0;i<count;i++)
					{
						FtpResponseLine line = (FtpResponseLine)_lines[i];
						ByteVector content = line.Content;
						ret[i] = _encoding.GetString(content.Data, 0, content.Size);
						ret[i] = ret[i].TrimEnd(crlfChars);
					}
				}
				return ret;
			}
		}


		/// <summary>
		/// Gets the response's code.
		/// </summary>
		public int Code
		{
			get 
			{
				if(_code == -2)
				{
					if(null != _lines)
					{
						FtpResponseLine line = (FtpResponseLine)_lines[0];
						_code = line.Code;
					}
					else
					{
						_code = -1;
					}
				}
				return _code;
			}
		}


		#region Internal attributes
		internal bool IsMultiLine
		{
			get 
			{
				if(null == _lines)
					return false;
				FtpResponseLine line = (FtpResponseLine)_lines[0];
				return (FtpResponseLineType.First == line.Type)?true:false; 
			}
		}

		/// <summary>
		/// Gets the flag indicating whether
		/// the response was completed.
		/// </summary>
		/// <remarks>
		/// Some exceptions thrown by methods
		/// of FtpClient class may contains a 
		/// reference to the FtpResponse instance.
		/// </remarks>
		internal bool IsCompleted
		{
			get { return (FtpResponseState.Completed == State); }
		}


		internal FtpResponseLine[] Lines
		{
			get
			{
				FtpResponseLine[] ret = null;
				if(null != _lines)
				{
					int count = _lines.Count;
					if(count > 0)
					{
						ret = new FtpResponseLine[count];
						_lines.CopyTo(0, ret, 0, count);
					}
				}
				return ret;
			}
		}

		internal int CurrentLineNumber
		{
			get { return _lines.Count - 1; }
		}

		internal FtpResponseState State
		{
			get { return _current.GetState(); }
		}

		internal bool IsPreliminaryReply
		{
			get { return IsInRange(Code, 99, 200); }
		}

		internal bool IsCompletionReply
		{
			get { return IsInRange(Code, 199, 300); }
		}

		internal bool IsIntermediateReply
		{
			get { return IsInRange(Code, 299, 400); }
		}

		internal bool IsTempNegativeReply
		{
			get { return IsInRange(Code, 399, 500); }
		}

		internal bool IsNegativeReply
		{
			get { return IsInRange(Code, 499, 600); }
		}
		#endregion

		#endregion

		#region Events
		internal delegate void NewLineEventHandler(FtpResponse sender, FtpResponseLine line);
		internal delegate void CompletedEventHandler(FtpResponse sender);

		internal event NewLineEventHandler NewLineEvent;
		internal event CompletedEventHandler CompletedEvent;

		void OnNewResponseLine(FtpResponseLine line)
		{
			_current.OnNewResponseLine(this, line);
			if(NewLineEvent != null)
				NewLineEvent(this, line);
		}

		void OnCompleted()
		{
			if(null != CompletedEvent)
				CompletedEvent(this);
		}
		#endregion

		internal void ChangeState(FtpResponseCurrent current)
		{
			_current = current;
		}
		
		internal void PutLine(LineInfo lineInfo)
		{
			if(null == lineInfo)
				throw new ArgumentNullException("lineInfo", "Value cannot be null.");

			_rawData.Add(lineInfo.Content);

			FtpResponseLine line = new FtpResponseLine(lineInfo);
			if(null == _lines)
				_lines = new ArrayList();
			_lines.Add(line);
			try
			{
				_current.OnNewResponseLine(this, line);
			}
			catch(FtpProtocolException e)
			{
				NSTrace.WriteLineError("Protocol error: " + e.ToString());
				e.SetResponse(this);
				e.SetLine(_lines.Count - 1);
				throw;
			}

			if(FtpResponseState.Completed == State)
				OnCompleted();
			else if(FtpResponseState.Intermediate != State)
			{
				string msg = string.Format("Response object is in invalid state: {0}. Stack: {1}.", State.ToString(), Environment.StackTrace);
				NSTrace.WriteLineError(msg);
				throw new InvalidOperationException(msg);
			}
		}
	}
}
