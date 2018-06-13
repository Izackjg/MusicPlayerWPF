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
    }
}
