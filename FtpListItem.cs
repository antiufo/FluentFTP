﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace System.Net.FtpClient {
	/// <summary>
	/// Parses MLST/MLSD and LIST formats
	/// </summary>
	public class FtpListItem {
		FtpObjectType _objectType = FtpObjectType.Unknown;
		/// <summary>
		/// Gets the type of object (File/Directory/Unknown)
		/// </summary>
		public FtpObjectType Type {
			get { return _objectType; }
			private set { _objectType = value; }
		}

		string _name = null;
		/// <summary>
		/// The file/directory name from the listing
		/// </summary>
		public string Name {
			get { return _name; }
			private set { _name = value; }
		}

		long _size = -1;
		/// <summary>
		/// The file size from the listing, default -1
		/// </summary>
		public long Size {
			get { return _size; }
			set { _size = value; }
		}

		DateTime _modify = DateTime.MinValue;
		/// <summary>
		/// The last write time from the listing
		/// </summary>
		public DateTime Modify {
			get { return _modify; }
			set { _modify = value; }
		}

		#region LIST parsing
		/// <summary>
		/// Parses DOS and UNIX LIST style listings
		/// </summary>
		/// <param name="listing"></param>
		private void ParseListListing(string listing) {
			foreach(FtpListFormatParser p in FtpListFormatParser.Parsers) {
				if(p.Parse(listing)) {
					this.Type = p.ObjectType;
					this.Name = p.Name;
					this.Size = p.Size;
					this.Modify = p.Modify;

					return;
				}
			}
		}
		#endregion

		#region MLS* Parsing
		private void ParseMachineListing(string listing) {
			List<string> matches = new List<string>();
			Regex re = new Regex(@"(.+?)=(.+?);|  ?(.+?)$");
			Match m;

			if(Regex.Match(listing, "^[0-9]+").Success) {
				// this is probably info messages, don't try to parse it
				return;
			}

			if((m = re.Match(listing)).Success) {
				do {
					matches.Clear();

					for(int i = 1; i < m.Groups.Count; i++) {
						if(m.Groups[i].Success) {
							matches.Add(m.Groups[i].Value);
						}
					}

					if(matches.Count == 2) {
						// key=value pair
						switch(matches[0].Trim().ToLower()) {
							case "type":
								if(this.Type == FtpObjectType.Unknown) {
									if(matches[1].ToLower() == "file") {
										this.Type = FtpObjectType.File;
									}
									else if(matches[1].ToLower() == "dir") {
										this.Type = FtpObjectType.Directory;
									}
								}
								break;
							case "size":
								if(this.Size == -1) {
									this.Size = long.Parse(matches[1]);
								}
								break;
							case "modify":
								if(this.Modify == DateTime.MinValue) {
									DateTime tmodify;
									string[] formats = new string[] { "yyyyMMddHHmmss", "yyyyMMddHHmmss.fff" };
									if(DateTime.TryParseExact(matches[1], formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out tmodify)) {
										this.Modify = tmodify;
									}
								}
								break;
						}
					}
					else if(matches.Count == 1 && this.Name == null) {
						// filename
						this.Name = matches[0];
					}
				} while((m = m.NextMatch()).Success);
			}
		}
		#endregion

		/// <summary>
		/// Parses a given listing
		/// </summary>
		/// <param name="listing">The single line that needs to be parsed</param>
		/// <param name="type">The command that generated the line to be parsed</param>
		public bool Parse(string listing, FtpListType type) {
			if(type == FtpListType.MLSD || type == FtpListType.MLST) {
				this.ParseMachineListing(listing);
			}
			else if(type == FtpListType.LIST) {
				this.ParseListListing(listing);
			}
			else {
				throw new NotImplementedException(string.Format("{0} style formats are not supported.", type.ToString()));
			}

			return this.Type != FtpObjectType.Unknown;
		}

		public override string ToString() {
			return string.Format("Type: {0} Name: {1} Size: {2}: Modify: {3}",
				this.Type, this.Name, this.Size, this.Modify);
		}

		/// <summary>
		/// Initializes an empty parser
		/// </summary>
		public FtpListItem() {

		}

		/// <summary>
		/// Initializes a new FtpListItem object from a parser's results.
		/// </summary>
		/// <param name="parser"></param>
		public FtpListItem(FtpListFormatParser parser) {
			this.Type = parser.ObjectType;
			this.Name = parser.Name;
			this.Size = parser.Size;
			this.Modify = parser.Modify;
		}

		/// <summary>
		/// Parses a given listing
		/// </summary>
		/// <param name="listing">The single line that needs to be parsed</param>
		/// <param name="type">The command that generated the line to be parsed</param>
		public FtpListItem(string listing, FtpListType type)
			: this() {
			this.Parse(listing, type);
		}

		/// <summary>
		/// Parses a given listing
		/// </summary>
		/// <param name="listing"></param>
		/// <param name="type"></param>
		public FtpListItem(string[] listing, FtpListType type)
			: this() {
			foreach(string s in listing) {
				this.Parse(s, type);
			}
		}

		/// <summary>
		/// Parses an array of list results
		/// </summary>
		/// <param name="items">Array of list results</param>
		/// <param name="type">The command that generated the list being parsed</param>
		/// <returns></returns>
		public static FtpListItem[] ParseList(string[] items, FtpListType type) {
			List<FtpListItem> lst = new List<FtpListItem>();

			foreach(string s in items) {
				FtpListItem i = new FtpListItem(s, type);

				if(i.Type != FtpObjectType.Unknown) {
					lst.Add(i);
				}
			}

			return lst.ToArray();
		}
	}
}
