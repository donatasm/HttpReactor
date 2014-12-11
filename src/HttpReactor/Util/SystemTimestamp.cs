using System.Diagnostics;

namespace HttpReactor.Util
{
    internal static class SystemTimestamp
    {
        private static readonly long TicksPerMicrosecond =
            Stopwatch.Frequency/1000000;

        public static long Current
        {
            get { return Stopwatch.GetTimestamp(); }
        }

        public static int GetElapsedMicros(long startTimestamp)
        {
            var elapsedTicks = Current - startTimestamp;
            return (int)(elapsedTicks/TicksPerMicrosecond);
        }
    }
}
