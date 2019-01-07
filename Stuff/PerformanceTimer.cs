using System;
using System.Diagnostics;

namespace KS.Foundation
{
    public static class PerformanceTimer
    {
        public static void Time(Action action, int count = 1, string caption = null)
        {
            GC.Collect();
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                action();
            }

            sw.Stop();

			if (caption == null)
				caption = action.Method.Name;
			if (count == 1)
				Console.WriteLine("Time for {0}: {1} ms", caption, ((long)sw.ElapsedMilliseconds).ToString("n0"));
			else            	
				Console.WriteLine("Time for {0} with count={1}: {2} ms", caption, count.ToString("n0"), ((long)sw.ElapsedMilliseconds).ToString("n0"));
        }
    }

	public static class PerformanceLogger
	{
		public static void Time(Action action, string caption = null)
		{			
			Stopwatch sw = Stopwatch.StartNew();
			action();
			sw.Stop();

			if (String.IsNullOrEmpty(caption))
				sw.LogVerbose ("{0} ms", sw.ElapsedMilliseconds);
			else
				sw.LogVerbose ("Time for {0}: {1} ms", caption, sw.ElapsedMilliseconds);
		}
	}
}
