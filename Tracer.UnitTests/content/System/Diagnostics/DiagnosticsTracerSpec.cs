using Moq;
using Xunit;

namespace System.Diagnostics.UnitTests
{
    public class DiagnosticsTracerSpec
    {
        private TracerManager manager = new TracerManager();

        public DiagnosticsTracerSpec()
        {
            Tracer.Initialize(manager);
        }

        [Fact]
        public void when_tracing_then_succeeds()
        {
            var listener = new Mock<TraceListener>();
            manager.AddListener("Foo", listener.Object);

            manager.SetTracingLevel("Foo", SourceLevels.Information);

            var tracer = Tracer.Get("Foo");

            tracer.Info("Hello");

            listener.Verify(x => x.TraceEvent(It.IsAny<TraceEventCache>(), "Foo", TraceEventType.Information, 0, "Hello", It.IsAny<object[]>()));
        }

        [Fact]
        public void when_tracing_with_dots_then_traces_to_all_listeners()
        {
            var fooListener = new Mock<TraceListener>();
            var fooBarListener = new Mock<TraceListener>();
            manager.AddListener("Foo", fooListener.Object);
            manager.AddListener("Foo.Bar", fooBarListener.Object);

            manager.SetTracingLevel("Foo", SourceLevels.Information);
            manager.SetTracingLevel("Foo.Bar", SourceLevels.Information);

            var tracer = Tracer.Get("Foo.Bar");

            tracer.Info("Hello");

            fooListener.Verify(x => x.TraceEvent(It.IsAny<TraceEventCache>(), "Foo.Bar", TraceEventType.Information, 0, "Hello", It.IsAny<object[]>()));
            fooBarListener.Verify(x => x.TraceEvent(It.IsAny<TraceEventCache>(), "Foo.Bar", TraceEventType.Information, 0, "Hello", It.IsAny<object[]>()));
        }

        [Fact]
        public void when_tracing_activity_then_builds_trace_log()
        {
            var xml = new XmlWriterTraceListener(@"C:\Delete\log.svclog", "Xml");

            manager.AddListener("*", xml);
            manager.SetTracingLevel("*", SourceLevels.All);

            var source = manager.GetSource("*");

            var tracer = Tracer.Get("Foo");

            using (tracer.StartActivity("Outer"))
            {
                tracer.Info("Hello info from outer");
                using (tracer.StartActivity("Inner"))
                {
                    tracer.Warn("Warn from inner");
                    Tracer.Get("Foo.Bar").Error("Something failed on another class!");
                }
            }

            xml.Flush();
            xml.Flush();
            System.Threading.Thread.Sleep(1000);
            xml.Close();
        }

    }
}
