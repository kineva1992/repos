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
	/// Specifies the type of the ftp item an instance of the 
	/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class
	/// describes.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The examples of the raw strings for all
	/// ftp item's types follows (UNIX directory listing style is used):
	/// </para>
	/// Link type: 
	/// <code>
	/// lr-xr-xr-x   1 owner    group           14017 May 18  1998 TheLink -> TheTarget
	/// </code>
	/// File type:
	/// <code>
	/// -rwxrwxrwx   1 owner    group        67992940 Sep 18 17:34 RFC-all.zip
	/// </code>
	/// Directory type:
	/// <code>
	/// drwxrwxrwx   1 owner    group               0 Oct  5  4:32 DirName
	/// </code>
	/// <b>Unresolved</b> type means that it was inpossible to resolve
	/// raw string to the appropriate ftp item's type, 
	/// due to unknown directory listing style.
	/// </remarks>
	public enum FtpItemType
	{
		/// <summary>
		/// An instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class
		/// describes ftp item which was not resolved.
		/// </summary>
		Unresolved,

		/// <summary>
		/// An instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class
		/// describes a directory.
		/// </summary>
		Directory,

		/// <summary>
		/// An instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class
		/// describes the link to other ftp item.
		/// </summary>
		Link,


		/// <summary>
		/// An instance of the 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class
		/// describes a file.
		/// </summary>
		File
	}

	/// <summary>
	/// Represents an ftp item.
	/// </summary>
	/// <remarks>
	/// Each directory at the FTP server may contains numerous 
	/// ftp items such as other directories, files, links. The <b>FtpItem</b>
	/// class is represents each of them.
	/// </remarks>
	public class FtpItem
	{
		string _rawString = null;
		string _name = null;
		string _refName = null;
		DateTime _date = DateTime.MinValue;
		long _size = -1;
		FtpItemType _type = FtpItemType.Unresolved;

		///<overloads>
		///Initialize a new instance of the 
		///<see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class.
		///</overloads>
		/// <summary>
		/// Initialize a new instance of 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class
		/// with 
		/// <see cref="BytesRoad.Net.Ftp.FtpItemType.Unresolved">FtpItemType.Unresolved</see>
		/// type.
		/// </summary>
		/// <param name="rawString">Raw string which represents an ftp item.</param>
		public FtpItem(string rawString)
		{
			_rawString = rawString;
		}

		/// <summary>
		/// Initialize a new instance of 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see> class
		/// with specified type.
		/// </summary>
		/// <param name="rawString">Raw string which represents an ftp item.</param>
		/// <param name="name">Name of the ftp item.</param>
		/// <param name="refName">
		/// Link name the ftp item points to.
		/// </param>
		/// <param name="date">
		/// Creation date of the ftp item.
		/// </param>
		/// <param name="size">Size of the ftp item.</param>
		/// <param name="type">One of the
		/// <see cref="BytesRoad.Net.Ftp.FtpItemType">FtpItemType</see>
		/// values.
		/// </param>
		/// <remarks>
		/// Note that 
		/// <see cref="BytesRoad.Net.Ftp.FtpItemType.Link">FtpItemType.Link</see>
		/// is the only type of the
		/// <see cref="BytesRoad.Net.Ftp.FtpItem">FtpItem</see>
		/// class instance which requires all parameters
		/// to be valid. 
		/// If type is 
		/// <see cref="BytesRoad.Net.Ftp.FtpItemType.Directory">FtpItemType.Directory</see>,
		/// the <b>refName</b> and <b>size</b> has no meaning, also if type is
		/// <see cref="BytesRoad.Net.Ftp.FtpItemType.File">FtpItemType.File</see>,
		/// the <b>refName</b> has no meaning as well.
		/// </remarks>
		public FtpItem(string rawString, 
			string name,
			string refName,
			DateTime date, 
			long size, 
			FtpItemType type)
		{
			_rawString = rawString;
			_name = name;
			_refName = refName;
			_date = date;
			_size = size;
			_type = type;
		}

		#region Attributes
		/// <summary>
		/// Gets the raw string which represents an ftp item.
		/// </summary>
		virtual public string RawString
		{
			get { return _rawString; }
		}

		/// <summary>
		/// Gets the name of the ftp item.
		/// </summary>
		virtual public string Name 
		{
			get { return _name; }
		}

		/// <summary>
		/// Gets the link name the ftp item points to.
		/// </summary>
		virtual public string RefName
		{
			get { return _refName; } 
		}

		/// <summary>
		/// Gets the creation date of the ftp item.
		/// </summary>
		virtual public DateTime Date 
		{
			get { return _date; }
		}

		/// <summary>
		/// Gets the size of ftp item.
		/// </summary>
		virtual public long Size 
		{
			get { return _size; }
		}

		/// <summary>
		/// Gets the type of the ftp item.
		/// </summary>
		/// <value>
		/// One of the
		/// <see cref="BytesRoad.Net.Ftp.FtpItemType">FtpItemType</see>
		/// values.
		/// </value>
		/// <remarks>
		/// The 
		/// <see cref="BytesRoad.Net.Ftp.FtpItemType.Unresolved">FtpItemType.Unresolved</see>
		/// type means that the 
		/// <see cref="BytesRoad.Net.Ftp.FtpItem.RawString">RawString</see>
		/// property is valid only.
		/// </remarks>
		virtual public FtpItemType ItemType
		{ 
			get { return _type; } 
		}
		#endregion
	}
}
