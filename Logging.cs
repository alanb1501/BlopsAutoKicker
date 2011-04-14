using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BlopsAutoKicker
{
    internal class Logging
    {
        internal static void WriteLog(string message)
        {
            Console.WriteLine(String.Concat(DateTime.Now.ToString("hh:mm:ss"), " ", message));
        }

        internal static void WriteLog(string message, params object[] args)
        {
            Console.WriteLine(String.Concat(DateTime.Now.ToString("hh:mm:ss"), " ", String.Format(message, args)));
        }
    }
}
