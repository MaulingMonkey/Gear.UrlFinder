using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Gear.UrlFinder {
	public static class UrlFinder {
		public struct Result {
			public string Searched;
			public int Start, End;
			public int Count  { get { return End-Start; }}
			public int Length { get { return End-Start; }}
			public string Value { get { return Searched.Substring(Start,Count); }}
		}

		static readonly string[] KnownNestedHosts = new[]
			{ "web.archive.org"
			};

		static readonly string[] KnownProtocols = new[]
			{ "http://"
			, "https://"
			, "file://"
			, "ftp://"
			, "irc://"
			, "ircs://"
			, "svn://"
			, "svn+ssh://"
			}; // TODO: Steal 'known' protocols from the windows registry? Also http://en.wikipedia.org/wiki/URI_scheme

		static readonly string[] KnownTLDs = new[]
			{ ".net"
			, ".org"
			, ".com"
			};

		static readonly Dictionary<string,string> KnownBLDs = new Dictionary<string,string>()
			{ { "www.", "http" }
			, { "ftp.", "ftp"  }
			, { "irc.", "irc"  }
			};

		static UrlFinder() {
			Debug.Assert( KnownProtocols.All( p => p.EndsWith  ("://") ) );
			Debug.Assert( KnownTLDs     .All( p => p.StartsWith( "." ) ) );
			Debug.Assert( KnownBLDs.Keys.All( p => p.EndsWith  ( "." ) ) );
		}

		static IEnumerable<int> IndiciesOf( string input, string substring ) {
			for ( int i = input.IndexOf(substring) ; i != -1 ; i = (i+1<input.Length) ? input.IndexOf(substring,i+1) : -1 ) yield return i;
		}

		public static IEnumerable<Result> Search( string input ) {
			var results = new List<Result>();

			// v2:
			// 1.  Locate ://s
			// 2.  Determine known protocols
			// 3.  Read forward, locate likely url end
			// 4.  Locate BLDs in remaining text (should be preceeded by non-ASCII)
			// 5.  Read forward, locate likely url end
			// 6.  Locate TLDs in remaining text
			// 7.  Read backward, locate likely url start
			// 8.  Read forward, locate likely url end
			// 9.  Read backward to determine remaining unknown protocols

			Func<char,bool> is_probably_a_protocol_character = ch => 
				(  ( 'a'<=ch && ch<='z' )
				|| ( 'A'<=ch && ch<='Z' )
				|| ( '0'<=ch && ch<='9' )
				|| "+".Contains(ch) // Not including "." even though it's legitimately part of some protocols -- too unlikely to be correct, better to handle those on a case by case basis via KnownProtocols
				// http://en.wikipedia.org/wiki/URI_scheme
				);

			var protocol_joins = IndiciesOf( input, "://" );

			foreach ( var join in protocol_joins ) {
				var result = new Result()
					{ Searched = input
					, Start    = -1
					};

				foreach ( var proto in KnownProtocols ) {
					var len = proto.Length-3;
					if ( join-len>=0 && Enumerable.Range(0,len).All(k=>input[join-len+k] == proto[k]) ) {
						result.Start = join-len;
						break;
					}
				}

				if ( result.Start==-1 ) {
					result.Start = join-1;
					while ( result.Start>=0 && is_probably_a_protocol_character(input[result.Start]) ) --result.Start;
				}

				result.End = join+3;
				results.Add(result);
			}

			for ( int resulti=0 ; resulti<results.Count ; ++resulti ) {
				var result = results[resulti];
				try {
					var dns_start = result.End;

					var url_end = input.IndexOfAny(new[]{' ','/','#'},dns_start);
					result.End = url_end==-1 ? input.Length : url_end;

					if ( input.IndexOf('.',result.Start,result.End-result.Start) == -1 ) { // TODO: also handle invalid urls like '.a.'
						// throw out invalid domain names which contain no periods
						result.Start = -1;
					} else if ( url_end == -1 ) {
					} else if ( input[url_end] == ' ' ) {
					} else { // # or /
						bool nested = KnownNestedHosts.Any( host => (dns_start+host.Length<input.Length) && Enumerable.Range(0,host.Length).All( i => host[i] == input[i+dns_start] ) );
						var maxend = (resulti+(nested?2:1)<results.Count) ? results[resulti+(nested?2:1)].Start : input.Length;

						int parens=0, brackets=0, curleys=0;
						bool ws=false;

						for ( int i=result.End ; i<maxend ; result.End=++i ) {
							switch ( input[i] ) {
							case '(': ++parens;   break;
							case '[': ++brackets; break;
							case '{': ++curleys;  break;
							case ')': --parens;   break;
							case ']': --brackets; break;
							case '}': --curleys;  break;
							case ' ': ws=true;    break;
							}

							if ( parens<0 || brackets<0 || curleys<0 ) break; // Probably punctuation parenthesis/brackets/curleys, end url here
							if ( input[i]==' ' && parens==0 && brackets==0 && curleys==0 ) break; // Probably a punctuation space, end url here
							if ( i+2<=maxend && ".!?".Contains(input[i+0]) && input[i+1]==' ' ) break; // Probably punctuation, end url here
						}

						if ( result.End == maxend && ws && (parens>0 || brackets>0 || curleys>0) ) {
							// unclosed parens/brackets/curleys, reguess url end via plain whitespace
							result.End = input.IndexOf(' ',result.Start,maxend-result.Start);
						}
					}

					if ( result.Start!=-1 ) while ( result.End>result.Start && ".!?".Contains(input[result.End-1]) ) --result.End; // remove trailing punctuation
				} finally {
					results[resulti] = result;
				}
			}

			int lastend = 0;
			for ( int i=0 ; i<results.Count ; ++i ) {
				var r = results[i];

				if ( r.Start<lastend ) r.Start=-1;
				else lastend = results[i].End;

				results[i] = r;
			}

			results.RemoveAll( r => r.Start==-1 );

			// TODO:  Steps 4+

			var dots = IndiciesOf( input, "." ).Where( i => !results.Any( r => r.Start<=i && i<r.End ) ).ToArray();

			//foreach ( var tld in KnownTLDs      ) for ( int i = input.IndexOf(tld) ; i != -1 ; i = input.IndexOf(tld,i+1) ) tells.Add( new Tell() { Index =  i, TellType = TellType.KnownTLD } );
			//foreach ( var bld in KnownBLDs.Keys ) for ( int i = input.IndexOf(bld) ; i != -1 ; i = input.IndexOf(bld,i+1) ) tells.Add( new Tell() { Index =  i, TellType = TellType.KnownBLD } );

			Debug.Assert( results.All( r1 => results.Except(new[]{r1}).All( r2 => (r1.Start+r1.Length <= r2.Start) || (r2.Start+r2.Length <= r1.Start) ) ) ); // no overlapping results

			return results;
		}
	}
}

#if false
		public static readonly string fWhen = @"\[(?<when>[^\]]+)\]";
		public static readonly Regex reWhen = new Regex("^"+fWhen, RegexOptions.Compiled);
		public static readonly Regex reLogFilename = new Regex(@".*\\(?<network>[^-\\]+)-(?<channel>#[^-\\]+)-(?<year>\d+)-(?<month>\d+)-(?<day>\d+)\.log",RegexOptions.Compiled);
		public static readonly Regex reWho = new Regex(@"(?<nick>[^*;! ]+)!(?<user>[^@ ]+)@(?<host>[^&> ]+)", RegexOptions.Compiled);
		public static readonly Regex reUrlProtocol = new Regex("^"+fUrlProtocol,RegexOptions.Compiled);
		public static readonly Regex reUrlPatterns = new Regex(@"\b(?:" + fUrlProtocol + "|"  + fUrlTLD + "|"  + fUrlBLD + ")", RegexOptions.Compiled);

		const string fUrlContinue = "(?:[^.,;:!?')\"\\s]|(\\S(?=\\S|$)))";
		const string fUrlProtocol = @"([-.+a-zA-Z0-9]+?:\/\/"+fUrlContinue+"+)";
		const string fUrlTLD      = @"([^\s]+?\.(?:com|net|org|edu|gov|mil|info|biz)"+fUrlContinue+"*)";
		const string fUrlBLD      = @"((?:www|ftp)\."+fUrlContinue+"+)";

		static string GuessAndPrependProtocol( string url ) {
			Match m = reUrlProtocol.Match(url);
			if ( m.Success ) return url;
			else if ( url.StartsWith("www.") ) return "http://"+url;
			else if ( url.StartsWith("ftp.") ) return "ftp://"+url;
			else return "http://"+url;
		}

		public static string HtmlizeUrls( string text ) {
			return reUrlPatterns.Replace( text, m => { var url=GuessAndPrependProtocol(m.Value); return "<a rel=\"nofollow\" class=\"link\" target=\"_blank\" href=\""+url+"\">"+m.Value+"</a>"; } );
		}
#endif
