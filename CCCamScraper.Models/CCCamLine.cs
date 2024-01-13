using System.Collections.Generic;

namespace CCCamScraper.Models
{
    public class CcCamLine
    {
        public const string CLineIndentifier = @"C:";

        /// <summary>
        /// Instructions where to look for a server
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Which port that server is using
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// Username to connect to that server
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password to connect to that server
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// If you like to receive Emulator shares from keys
        /// (only if set to 1 if set to give emus on the f line from the server you are getting )
        /// </summary>
        public string Wantemus { get; set; } = "no"; //yes or no

        /// <summary>
        /// Limiting what to get from that particular server
        /// </summary>
        public List<Providers> Providers { get; set; }

        /// <summary>
        /// Line comment
        /// </summary>
        public string Cccversion { get; set; } //ex: # v2.0.11-2892
    }
}

/*
 *Syntax : C: <hostname> <port> <username> <password> <wantemus> { caid:id:uphops, caid:id:uphops, ... }

           C: server.noip.com 12000 username password no { 0:0:2, 100:3, 100:4, 100:5, 100:9, 100:A, 100:c }
Examples

A basic C line that most users will use :

Quote:
C: server.noip.com 12000 username password
A C line that is more appropriate if you do not want to recieve key Emulators:

Quote:
C: server.noip.com 12000 username password no
A C line that is more appropriate if you want to recieve key Emulators (Not highly recommended) :

Quote:
C: server.noip.com 12000 username password yes
A Cline that will limit to receive all shares that are only up to 2 hops away (Recommended):

Quote:
C: server.noip.com 12000 username password no { 0:0:2 }
A Cline that will get all shares but does not get a specific provider:

Quote:
C: server.noip.com 12000 username password no { 0:0:2, 093b:0 }
A Cline that will get all shares but does not get specific providers:

Quote:
C: server.noip.com 12000 username password no { 0:0:2, 100:3, 100:4, 100:5, 100:9, 100:A, 100:c }

 *
 */