using System;

namespace HttpReactor.Util
{
    internal static class Units
    {
        public static int TotalMicroseconds(this TimeSpan timeSpan)
        {
            return (int)(timeSpan.TotalMilliseconds * 1000);
        }
    }
}
