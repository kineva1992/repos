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
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;

namespace BytesRoad.Net.Ftp
{
	/// <summary>
	/// Summary description for FptItemResolverImpl.
	/// </summary>
	internal class FtpItemResolverImpl : IFtpItemResolver
	{
		static FtpItemResolverImpl _resolver = null;
		static char[] crlfChars = new char[]{'\r', '\n'};

		Regex _dosRegEx = new Regex(@"^(?<month>\d\d)-(?<day>\d\d)-(?<year>\d\d)" +
			@" +(?<hour>\d\d):(?<min>\d\d)(?<half>AM|PM)" +
			@" +(?<dir><DIR>)? +(?<size>\d+)? +(?<name>.*)$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		Regex _unixRegEx = new Regex(@"^(?<type>[-dl])[-rwxs]{9}" +
			@" +\d+" +
			@" +(?<owner>[^ ]+)"+
			@" +(?<group>[^ ]+)" +
			@" +(?<size>-?\d+)" +  //minus added because big file problems at some servers
			@" +(?<month>[^ ]+)" +
			@" +(?<day>\d+)" + 
			@" +(?<dummy>(?<year>\d{4})|(?<hour>\d{1,2}):(?<min>\d\d))" +
			@" +(?<dummy2>((?<name>.+) -> (?<link>.+)|(?<name>.+)))$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);


		Hashtable _monthesHash = new Hashtable();
			
		protected FtpItemResolverImpl()
		{
			_monthesHash["Jan"] = 1;
			_monthesHash["Feb"] = 2;
			_monthesHash["Mar"] = 3;
			_monthesHash["Apr"] = 4;
			_monthesHash["May"] = 5;
			_monthesHash["Jun"] = 6;
			_monthesHash["Jul"] = 7;
			_monthesHash["Aug"] = 8;
			_monthesHash["Sep"] = 9;
			_monthesHash["Oct"] = 10;
			_monthesHash["Nov"] = 11;
			_monthesHash["Dec"] = 12;
		}

		static internal FtpItemResolverImpl Instance
		{
			get
			{
				if(null == _resolver)
					_resolver = new FtpItemResolverImpl();
				return _resolver;
			}
		}

		FtpItem ResolveAsUnix(string line)
		{
			Match m = _unixRegEx.Match(line);
			if(15 != m.Groups.Count)
				return null;

			//looks like a UNIX style
			FtpItemType itemType = FtpItemType.Unresolved;
			string name = null;
			string refName = null;
			long size = 0;

			string strType = m.Groups["type"].Value;
			if(strType == "-")
			{
				itemType = FtpItemType.File;
			}
			else if(strType == "d")
			{
				itemType = FtpItemType.Directory;
			}
			else if(strType == "l")
			{
				itemType = FtpItemType.Link;
				refName = m.Groups["link"].Value;
			}
			else
				return null; //unknown 

			size = long.Parse(m.Groups["size"].Value);
			name = m.Groups["name"].Value;

			int year = 0;
			string strYear = m.Groups["year"].Value;
			if(strYear.Length > 0)
				year = int.Parse(strYear);
			else
				year = DateTime.Now.Year;

			int month = (int)_monthesHash[m.Groups["month"].Value];
			int day = int.Parse(m.Groups["day"].Value);
			
			int hour = 0, minutes = 0;
			string strHour = m.Groups["hour"].Value;
			if(strHour.Length > 0)
				hour = int.Parse(strHour);

			string strMinutes = m.Groups["min"].Value;
			if(strMinutes.Length > 0)
				minutes = int.Parse(strMinutes);

			DateTime dt = new DateTime(year, month, day, hour, minutes, 0, 0);
			return new FtpItem(line, name, refName, dt, size, itemType);
		}

		FtpItem ResolveAsMsDos(string line)
		{
			Match m = _dosRegEx.Match(line);
			if(10 != m.Groups.Count)
				return null;

			//looks like a DOS style
			FtpItemType itemType = FtpItemType.Unresolved;
			string name = null;
			long size = 0;

			Group dir = m.Groups["dir"];
			if(dir.Length > 0)
			{
				itemType = FtpItemType.Directory;
			}
			else
			{
				size = long.Parse(m.Groups["size"].Value);
				itemType = FtpItemType.File;
			}

			name = m.Groups["name"].Value;

			int year = int.Parse(m.Groups["year"].Value);
			year = year>70?(year+1900):(year+2000);
			int month = int.Parse(m.Groups["month"].Value);
			int day = int.Parse(m.Groups["day"].Value);
			int hour = int.Parse(m.Groups["hour"].Value);
			string half = m.Groups["half"].Value.ToUpper();
			if((half == "PM") &&
				(hour >= 1) &&
				(hour <= 11))
			{
				hour += 12;
			}

			if((half == "AM") && (hour == 12))
				hour -= 12;

			int minute = int.Parse(m.Groups["min"].Value);
			DateTime dt = new DateTime(year, month, day, hour, minute, 0, 0);

			return new FtpItem(line, name, null, dt, size, itemType);
		}

		#region IFtpItemResolver Members

		public FtpItem Resolve(string line)
		{
			line = line.TrimEnd(crlfChars);

			FtpItem item = ResolveAsUnix(line);
			if(null == item)
				item = ResolveAsMsDos(line);

			if(null == item)
				item = new FtpItem(line);

			return item;
		}

		#endregion
	}
}
