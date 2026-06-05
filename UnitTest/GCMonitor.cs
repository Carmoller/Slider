using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UnitTest
{
    internal class GCMonitor : IDisposable
    {
        public class GCStatistics
        {
            public int gen0Collections { get; set; }
            public int gen1Collections { get; set; }
            public int gen2Collections { get; set; }
            public long memoryGrowth { get; set; }
        }
        private bool disposedValue;
        private long gen0Before = GC.GetTotalMemory(false);
        private int gen0CountBefore = GC.CollectionCount(0);
        private int gen1CountBefore = GC.CollectionCount(1);
        private int gen2CountBefore = GC.CollectionCount(2);
        private GCStatistics? _stats;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    long gen0After = GC.GetTotalMemory(false);
                    int gen0CountAfter = GC.CollectionCount(0);
                    int gen1CountAfter = GC.CollectionCount(1);
                    int gen2CountAfter = GC.CollectionCount(2);

                    int gen0Collections = gen0CountAfter - gen0CountBefore;
                    int gen1Collections = gen1CountAfter - gen1CountBefore;
                    int gen2Collections = gen2CountAfter - gen2CountBefore;
                    long memoryGrowth = gen0After - gen0Before;

                    if (_stats != null)
                    {
                        _stats.memoryGrowth = memoryGrowth;
                        _stats.gen0Collections = gen0Collections;
                        _stats.gen1Collections = gen1Collections;
                        _stats.gen2Collections = gen2Collections;
                    }
                    else
                    {
                        Debug.WriteLine($"\n--- GC Statistics ---");
                        Debug.WriteLine($"Gen0 Collections: {gen0Collections}");
                        Debug.WriteLine($"Gen1 Collections: {gen1Collections}");
                        Debug.WriteLine($"Gen2 Collections: {gen2Collections}");
                        Debug.WriteLine($"Memory Growth: {memoryGrowth:N0} bytes");
                    }

                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public GCMonitor(GCStatistics? stats)
        {
            if (stats != null)
                _stats = stats;
        }
    }
}
