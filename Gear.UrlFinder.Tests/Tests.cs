using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gear.UrlFinder.Tests {
	[TestClass] public class Tests {
		public TestContext TestContext { get; set; }

		static void Check( string input, params string[] expected_matches ) {
			var results = UrlFinder.Search(input).ToArray();
			Assert.AreEqual( expected_matches.Length, results.Length );
			for ( int i=0 ; i<results.Length && i<expected_matches.Length ; ++i ) Assert.AreEqual( expected_matches[i], results[i].Value );
		}

		static readonly string[] Nothing = new string[]{};

		[TestMethod] public void ChatLogsBad1() { Check( "<species> so whats all this &quot;.net framewokr\" stuff MS marketing is so obsessed with", Nothing ); }
		[TestMethod] public void ChatLogsBad2() { Check( "<species> so whats all this \".net framewokr\" stuff MS marketing is so obsessed with"    , Nothing ); }

		[TestMethod] public void ChatLogs1() { Check
			( "<maxx> I think the difference is too noticable, I'll probably revert back to rotation (http://reltru.com/sandbox/BallThrow/20110101_01/bin/play.html)"
			, "http://reltru.com/sandbox/BallThrow/20110101_01/bin/play.html"
			);
		}
		[TestMethod] public void ChatLogs2() { Check
			( "<Zao> http://home.comcast.net/~tom_forsyth/blog.wiki.html#[[Moore's Law vs Duck Typing]]"
			, "http://home.comcast.net/~tom_forsyth/blog.wiki.html#[[Moore's Law vs Duck Typing]]"
			);
		}
		[TestMethod] public void ChatLogs3() { Check
			( "<Zao> http://home.comcast.net/~tom_forsyth/blog.wiki.html#[[Data Oriented Luddites]]"
			, "http://home.comcast.net/~tom_forsyth/blog.wiki.html#[[Data Oriented Luddites]]"
			);
		}
		[TestMethod] public void ChatLogs4() { Check
			( "<Edited> http://home.comcast.net/~tom_forsyth/blog.wiki.html#[[Moore's Law vs Duck Typing]] trailing stuff"
			, "http://home.comcast.net/~tom_forsyth/blog.wiki.html#[[Moore's Law vs Duck Typing]]"
			);
		}
		[TestMethod] public void ChatLogs5() { Check
			( "<Edited> (http://home.comcast.net/~tom_forsyth/blog.wiki.html#[[Data Oriented Luddites]])"
			, "http://home.comcast.net/~tom_forsyth/blog.wiki.html#[[Data Oriented Luddites]]"
			);
		}
		[TestMethod] public void ChatLogs6() { Check
			( "<Oluseyi> it's a cost intensive process, so the options are advertising for the provider (yoursubdomain.dyndns.com, yoursubdomain.no-ip.com) or pay up"
			, "yoursubdomain.dyndns.com"
			, "yoursubdomain.no-ip.com"
			);
		}
		[TestMethod] public void ChatLogs7() { Check
			( "<sinclair|work> there doesnt seem to be a well defined list, the closest i found was here....http://en.wikipedia.org/wiki/List_of_user_agents_for_mobile_phones"
			, "http://en.wikipedia.org/wiki/List_of_user_agents_for_mobile_phones"
			);
		}
		[TestMethod] public void ChatLogs8() { Check
			( "<fabianhjr> Found somethinghttp://alternativeto.net/software/imovie/ :D"
			, "http://alternativeto.net/software/imovie/"
			);
		}
		[TestMethod] public void ChatLogs9() { Check
			( "<sinclair|work> there doesnt seem to be a well defined list, the closest i found was here....http://en.wikipedia.org/wiki/List_of_user_agents_for_mobile_phones"
			, "http://en.wikipedia.org/wiki/List_of_user_agents_for_mobile_phones"
			);
		}
		[TestMethod] public void ChatLogs10() { Check
			( "<ssebelius> hahahhttp://kotaku.com/5712852/come-and-play-everythings-a+ko/gallery/"
			, "http://kotaku.com/5712852/come-and-play-everythings-a+ko/gallery/"
			);
		}

		[TestMethod] public void ChatLogs11() { Check
			( "<Oluseyi> heh >> \"LLVM even has a snazzy new logo, a not-so-subtle homage to a well-known compiler design textbook: http://static.arstechnica.com/20090828/llvm-logo.pnghttp://static.arstechnica.com/20090828/llvm-logo.png \""
			, "http://static.arstechnica.com/20090828/llvm-logo.png"
			, "http://static.arstechnica.com/20090828/llvm-logo.png"
			);
		}

		[TestMethod] public void ChatLogs12() { Check
			( "<EvilFaked> http://web.archive.org/web/*/http://schlockmercenary.comhttp://web.archive.org/web/*/http://schlockmercenary.com"
			, "http://web.archive.org/web/*/http://schlockmercenary.com"
			, "http://web.archive.org/web/*/http://schlockmercenary.com"
			);
		}

		// TODO: ttp:// -> http://
		[TestMethod] public void ChatLogs13() { Check
			( "<Zao> ttp://www.leepoint.net/notes-java/data/expressions/22compareobjects.html"
			, "http://www.leepoint.net/notes-java/data/expressions/22compareobjects.html"
			);
		}

		[TestMethod] public void ChatLogs14() { Check
			( "<dadibom> if(str.comparesTo(\"b\")){do stuff}"
			);
		}
	}
}
