using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BlopsAutoKicker
{
    /// <summary>
    /// Represents a CodPlayer that connected to the server
    /// </summary>
    internal class CodPlayer
    {
        internal string Name { get; set; }
        internal string ClanTag { get; set; }
        internal string FullName { get; set; }
        internal HashSet<Warning> Warnings { get; set; } 
        internal bool Greeted { get; set; }
        internal string Id { get; set; }
        internal Guid Guid { get; set; }
        internal bool IsXD { get; set; }

        private static Regex NameRegex = new Regex(@"^\[(.*?)\](.*?)$",RegexOptions.Compiled);

        internal CodPlayer(string fullName, string id)
        {
            Warnings = new HashSet<Warning>();
            Greeted = false;
            Id = id;
            IsXD = false;

            var m = NameRegex.Match(fullName);
            FullName = fullName;

            if (m.Success)
            {
                ClanTag = m.Groups[1].Value;
                Name = m.Groups[2].Value;
            }
            else
            {
                Name = fullName;
            }
        }
    }
}
