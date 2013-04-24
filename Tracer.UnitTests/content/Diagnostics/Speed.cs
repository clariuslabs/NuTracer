
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.UnitTests
{
    public class Speed
    {
        [Fact]
        public void when_run_then_meassures()
        {
            var repeat = 100000;

            Tracer.Initialize(new System.Diagnostics.TracerManager());

            Tracer.Get("A.B.C.D").Info("Warming up");

            Run(Tracer.Get("A.B.C.D"), repeat);
        }

        private void Run(ITracer tracer, long repeat)
        {
            var e = new ManualResetEventSlim();
            var watch = Stopwatch.StartNew();

            for (int i = 1; i <= repeat; i++)
            {
                var current = i;
                Task.Factory.StartNew(() =>
                {
                    tracer.Info(Guid.NewGuid());
                    if (current == repeat)
                        e.Set();
                });
            }

            e.Wait();

            Console.WriteLine("{0}: {1}", tracer.GetType().Name, watch.ElapsedTicks / repeat);
        }
    }
}
