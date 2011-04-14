using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.IO.Compression;

namespace BlopsAutoKicker
{
    public class Program
    {
        const string XDSniperLogUrl = "";
        const string LogRefreshUrl = "http://logs.gameservers.com/timeout";

        const string rconHost = "";
        const int rconPort = 0;
        const string rconPw = "";

        const string UserAgent = "XD Blops Kicker v1.0.0";

        static Dictionary<string, CodPlayer> currentPlayers;
        static RCON rcon;

        static void Main(string[] args)
        {
            int refreshTimeInSeconds = 10;
            var logRefreshString = HttpGet(LogRefreshUrl);

            if (!Int32.TryParse(logRefreshString, out refreshTimeInSeconds))
            {
                Logging.WriteLog("Invalid refresh time. Setting log refresh to 10 seconds. Server response was {0}.", logRefreshString);
            }
            else
            {
                Logging.WriteLog("Current Refresh Time {0}",refreshTimeInSeconds);
            }

            bool shutdown = false;

            double lastTimeStampRead = 0;

            currentPlayers = new Dictionary<string,CodPlayer>();
            rcon = new RCON(rconHost, rconPort, rconPw);

            var messageEvent = new System.Threading.AutoResetEvent(false);

            Thread messageThread = new Thread(new ThreadStart(() =>
            {
                while (!shutdown)
                {
                    Logging.WriteLog("Sending server messages");

                    rcon.Say(Strings.WelcomeMessage);

                    if (messageEvent.WaitOne(new TimeSpan(0, 0, 15)) && shutdown)
                    {
                        return;
                    }
                    rcon.Say(Strings.FriendInviteMessage);

                    if (messageEvent.WaitOne(new TimeSpan(0, 0, 15)) && shutdown)
                    {
                        return;
                    }
                    rcon.Say(Strings.ServerProtection);

                    if (messageEvent.WaitOne(new TimeSpan(0, 0, 15)) && shutdown)
                    {
                        return;
                    }

                    //send the server rules
                    SendRulesToPlayer(null);

                    //rcon.Say(String.Format(Strings.RulesTrigger, CodServerLogLine.RulesTrigger));
                    Logging.WriteLog("finished sending server messages");

                    if (messageEvent.WaitOne(new TimeSpan(0, 0, 60)) && shutdown)
                    {
                        return;
                    }
                }
            }));

            var logEvent = new AutoResetEvent(false);

            Thread logParserThread = new Thread(new ThreadStart(() => 
            {
                while (!shutdown)
                {
                    Logging.WriteLog("Getting Log data...");

                    var logSnippet = HttpGet(XDSniperLogUrl,true); //should be 2MB at most. add range header

                    Logging.WriteLog("Log Data collected...");

                    var lines = new List<string>(logSnippet.Split('\n'));

                    Logging.WriteLog("Recieved {0} log lines.", lines.Count);

                    var parsedLines = FindAllValidLogLines(lines, ref lastTimeStampRead);

                    if (parsedLines != null)
                    {
                        //now go through the stack, and take appropriate action...
                        HandleCurrentLogs(parsedLines, ref lastTimeStampRead);
                    }

                    if (logEvent.WaitOne(new TimeSpan(0, 0, refreshTimeInSeconds)) && shutdown)
                    {
                        // if we're shutting down, or already in this loop, just return.
                        return;
                    }
                }
            }));

            logParserThread.Start();
            messageThread.Start();

            System.Threading.AutoResetEvent wait = new System.Threading.AutoResetEvent(false);


            Console.CancelKeyPress += new ConsoleCancelEventHandler((o,e) => {
                Logging.WriteLog("Shutting down");
                shutdown = true;
                messageEvent.Set();
                logEvent.Set();
                var w = new System.Threading.AutoResetEvent(false);

                //wait 15 seconds for things to shutdown
                w.WaitOne(new TimeSpan(0, 0, 5));

                wait.Set();
            });

            wait.WaitOne();

            //meh assyemtric behavior... :(
            ConnectionPool.DrainPool();

        }

        private static string HttpGet(string url,bool addRange = false)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Timeout = 5000;
                request.UserAgent = UserAgent;
                //request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                if (addRange)
                {
                    request.AddRange(-10000);
                }
                
                
                var response = (HttpWebResponse) request.GetResponse();
                Stream responseStream = response.GetResponseStream();

                //Logging.WriteLog("Recieved {0} bytes from the server", responseStream.Length);

                //if (response.ContentEncoding.ToLower().Contains("gzip"))
                //    responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                //else if (response.ContentEncoding.ToLower().Contains("deflate"))
                //    responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);   


                var reader = new StreamReader(responseStream,Encoding.Default);

                return reader.ReadToEnd().Trim();
            }
            catch (WebException ex)
            {
                Logging.WriteLog("WebException: " + ex.Message);
            }
            return "";
        }

        private static void HandleCurrentLogs(Stack<CodServerLogLine> parsedLines, ref double lastTimeStampRead)
        {
            double lastTime = lastTimeStampRead;

            foreach (var line in parsedLines)
            {
                if (line.TimeStamp <= lastTimeStampRead)
                {
                    continue; //we already read past this line...so skip it
                }

                lastTime = line.TimeStamp;

                switch (line.LineType)
                {
                    case LogLineType.Init:
                        //reset all warnings.
                        Logging.WriteLog("Init Game");
                        ResetWarnings();
                        break;

                    case LogLineType.Blank:
                    case LogLineType.Shutdown:
                        break;

                    case LogLineType.Join:
                        // if it's a new player, display the greet message, then add them to the list of players
                        if (!currentPlayers.ContainsKey(line.PlayerId))
                        {
                            Logging.WriteLog(String.Format("Player {0} Joined with Id {1}", line.PlayerName, line.PlayerId));
                            currentPlayers[line.PlayerId] = CreatePlayer(line.PlayerName, line.PlayerId);
                        }
                        else
                        {
                            if (currentPlayers[line.PlayerId].FullName != line.PlayerName)
                            {
                                Logging.WriteLog("Player {0} renamed to {1}", currentPlayers[line.PlayerId], line.PlayerName);
                                currentPlayers[line.PlayerId] = CreatePlayer(line.PlayerName, line.PlayerId);
                            }
                        }
                        break;

                    case LogLineType.Quit:
                        //player quit, remove them...
                        Logging.WriteLog(String.Format("Player {0} Quit with Id {1}", line.PlayerName, line.PlayerId));
                        currentPlayers.Remove(line.PlayerId);
                        break;

                    case LogLineType.Say:
                        //see if someone typed !rules
                        if (line.RulesRequest)
                        {
                            Logging.WriteLog(String.Format("Player {0} with Id {1} requested rules", line.PlayerName, line.PlayerId));
                                    SendRulesToPlayer(line.PlayerId);
                        }
                        break;

                        // for both damage and kills, verify rules are followed
                    case LogLineType.Kill_Damage:
                            EnforceRules(line);
                        break;

                    default:
                        Logging.WriteLog(String.Concat("Unknown command type: ",line.LineType.ToString()));
                        break;
                }
            }

            lastTimeStampRead = lastTime;
        }

        private static void SendRulesToPlayer(string playerName)
        {
            rcon.Say(Strings.WeaponRules, playerName);
            Thread.Sleep(5000);
            rcon.Say(Strings.KnifingRules, playerName);
            Thread.Sleep(5000);
            rcon.Say(Strings.PerkRules, playerName);
            Thread.Sleep(5000);
            rcon.Say(Strings.GrenadesRules, playerName);
        }

        private static void EnforceRules(CodServerLogLine line)
        {
            var violations = RulesValidator.CheckDamageAndKillsForViolations(line.Weapon);

            if (violations == ViolationType.None)
            {
                Logging.WriteLog("No violations for {0}", line.PlayerName);
                return; //good doggy!
            }

            CodPlayer player;

            lock (currentPlayers)
            {
                if (!currentPlayers.TryGetValue(line.PlayerId, out player))
                {
                    player = CreatePlayer(line.PlayerName, line.PlayerId);
                    currentPlayers[line.PlayerId] = player;
                }
            }

            bool kickPlayer = false;

            if (violations == ViolationType.Knife)
            {
                if (line.IsMelee)
                {
                    lock (player.Warnings)
                    {
                        if (!player.Warnings.Contains(Warning.Knifing))
                        {
                            //warn the player
                            Logging.WriteLog(String.Format("Sending warning to {0} for knifing violation.", player.FullName));
                            rcon.Say(String.Format(Strings.WarningMessage, Strings.KnifeUsage, CodServerLogLine.RulesTrigger), player.Id);
                            player.Warnings.Add(Warning.Knifing);
                        }
                        else
                        {
                            kickPlayer = true;
                        }
                    }
                }
            }
            else
            {
                lock (player.Warnings)
                {
                    if (!player.Warnings.Contains(Warning.Illegal_Weapons))
                    {
                        Logging.WriteLog(String.Format("Sending warning to {0} for weapon violation: {1}", player.FullName, line.Weapon));
                        //warn the player
                        rcon.Say(String.Format(Strings.WarningMessage, Strings.FollowRules, CodServerLogLine.RulesTrigger), player.Id);
                        player.Warnings.Add(Warning.Illegal_Weapons);
                    }
                    else
                    {
                        kickPlayer = true;
                    }
                }
            }

            if (kickPlayer)
            {
                Logging.WriteLog(String.Format("Kicking player {0}. For {1}", player.FullName, line.Weapon));
                //boot this douche, but first message the entire server
                rcon.Say(String.Format(Strings.Kicked, player.FullName));
                Thread.Sleep(1000); //badbad
                rcon.ClientKick(player.Id);
            }
        }

        private static void ResetWarnings()
        {
            foreach (var player in currentPlayers.Values)
            {
                player.Warnings.Clear();
            }
        }

        private static CodPlayer CreatePlayer(string name, string id)
        {
            var player = new CodPlayer(name, id);
            ThreadPool.QueueUserWorkItem((e) =>
            {
                Thread.Sleep(10000);
                Logging.WriteLog("Greeting player {0}", player.FullName);
                rcon.Say(String.Format(Strings.PersonalGreet, player.FullName, CodServerLogLine.RulesTrigger), player.Id);
            });
            player.Greeted = true;
            return player;
        }

        private static Stack<CodServerLogLine> FindAllValidLogLines(List<string> lines, ref double lastTimeStampRead)
        {
            var parsedLines = new Stack<CodServerLogLine>();

            lines.Reverse();

            foreach(var line in lines)
            {
                Logging.WriteLog(line);
                var parsedLine = CodServerLogLine.ParseLine(line);

                if (parsedLine == null)
                {
                    Logging.WriteLog("Skipping null line");
                    continue;
                }

                if (parsedLine.TimeStamp == 0)
                {
                    // time restarted
                    lastTimeStampRead = 0;
                }

                parsedLines.Push(parsedLine);

                if (parsedLine.LineType == LogLineType.Init)
                {
                    break;
                }

                if (parsedLine.LineType == LogLineType.Shutdown)
                {
                    //we ended a round, but haven't started a new one...
                    //just exit the log handler for now.
                    Logging.WriteLog("Skipping parsing logs because we reached the end of a round");
                    return null;
                }
            }

            return parsedLines;
        }
    }
}
