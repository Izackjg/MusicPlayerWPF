using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer
{
    static class Extensions
    {
        public static TimeSpan StripMilliseconds(this TimeSpan ts)
        {
            return new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds);
        }

        public static bool IsMatch(this string str, string other)
        {
            string strLower = str.ToLower();
            string otherLower = other.ToLower();

            for (int i = 0; i < strLower.Length - 1; i++)
            {
                if (otherLower.IndexOf(strLower.Substring(i, 2)) >= 0)
                    return true; // match
            }

            return false; // no match
        }
    }
}
