using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BlopsAutoKicker
{

    internal enum LogLineType
    {
        Join = 0,
        Quit,
        Kill_Damage,
        Say,
        Init,
        Shutdown,
        Blank
    }


    /// <summary>
    /// Represents all the data that could come from a single line
    /// </summary>
    /// 
    internal class CodServerLogLine
    {
        internal const string RulesTrigger = "!rules";

        internal LogLineType LineType { get; private set; }
        internal string PlayerName { get; private set; }
        internal string PlayerId { get; private set; }
        internal string Weapon { get; private set; }

        internal bool RulesRequest { get; private set; }
        internal bool IsMelee { get; private set; }


        internal double TimeStamp { get; private set; }

        static Regex regex = new Regex(@"(\d+):(\d+) (.*)$",RegexOptions.Compiled);

        private CodServerLogLine()
        {

        }
        
        internal static CodServerLogLine ParseLine(string line)
        {
            CodServerLogLine csl = new CodServerLogLine();

            var m = regex.Match(line);

            if (!m.Success)
            {
                return null;
            }

            double d;

            if (!Double.TryParse(String.Concat(m.Groups[1].Value, ".", m.Groups[2].Value), out d))
            {
                // couldn't parse the time stamp
                return null;
            }

            csl.TimeStamp = d;

            var data = m.Groups[3].Value.Split(';');

            //handle special cases for broken lines, init and shutdown
            if (data.Count() == 1)
            {
                if (data[0].Contains("InitGame:"))
                {
                    csl.LineType = LogLineType.Init;
                    return csl;
                }
                else if (data[0].Contains("ShutdownGame:"))
                {
                    csl.LineType = LogLineType.Shutdown;
                    return csl;
                }

                return null;
            }

            var command = data[0];

            switch (command)
            {
                case "J":
                    if (data.Length < 4)
                    {
                        csl.LineType = LogLineType.Blank;
                        break;
                    }
                    csl.LineType = LogLineType.Join;
                    csl.PlayerId = data[2].Trim();
                    csl.PlayerName = data[3].Trim();
                    break;
                case "Q":
                    if (data.Length < 4)
                    {
                        csl.LineType = LogLineType.Blank;
                        break;
                    }
                    csl.LineType = LogLineType.Quit;
                    csl.PlayerId = data[2].Trim();
                    csl.PlayerName = data[3].Trim();
                    break;
                case "say":
                    if (data.Length < 5)
                    {
                        csl.LineType = LogLineType.Blank;
                        break;
                    }

                    csl.LineType = LogLineType.Say;
                    csl.PlayerId = data[2].Trim();
                    csl.PlayerName = data[3].Trim();
                    if (data.Length > 4 && data[4].Contains(RulesTrigger))
                    {
                        csl.RulesRequest = true;
                    }
                    break;
                case "D":
                case "K":
                    if (data.Length < 12)
                    {
                        csl.LineType = LogLineType.Blank;
                        //broken line
                        break;
                    }
                    csl.LineType = LogLineType.Kill_Damage;
                    csl.PlayerId = data[6].Trim();
                    csl.PlayerName = data[8].Trim();
                    csl.Weapon = data[9].Trim();
                    if(data[11].Contains("MOD_MELEE"))
                    {
                        csl.IsMelee = true;
                    }
                    break;

                default:
                    if (data[0].Contains("InitGame:"))
                    {
                        csl.LineType = LogLineType.Init;
                    }
                    else if (data[0].Contains("ShutdownGame:"))
                    {
                        csl.LineType = LogLineType.Shutdown;
                    }
                    else
                    {
                        csl.LineType = LogLineType.Blank;
                    }
                    break;
            }

            return csl;
        }
    }
}
