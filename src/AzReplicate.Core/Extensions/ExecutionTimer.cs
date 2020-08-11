using System;
using System.Diagnostics;

namespace AzReplicate.Core.Extensions
{
    public class ExecutionTimer : IDisposable
    {
        private readonly Stopwatch _stopWatch = new Stopwatch();

        public TimeSpan Elapsed
        {
            get 
            {
                return _stopWatch.Elapsed;
            }
        }

        public TimeSpan CalculateElapsedAndStopMeasuring()
        {
            _stopWatch.Stop();
            return _stopWatch.Elapsed;
        }

        public ExecutionTimer()
        {
            _stopWatch.Restart();
        }

        public void Dispose()
        {
            _stopWatch.Stop();
        }
    }
}
