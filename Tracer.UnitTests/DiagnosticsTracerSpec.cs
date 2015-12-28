using Moq;
using Xunit;
using System.IO;
using Tracing.SystemDiagnostics;
using System.Diagnostics;
using System;

namespace Tracing
{
    public class DiagnosticsTracerSpec
    {
        const string SvcViewer = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\SvcTraceViewer.exe";
        const string LogFile = "log.xml";

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
            if (File.Exists(LogFile))
                File.Delete(LogFile);

            var xml = new XmlWriterTraceListener(LogFile, "Xml");

            manager.AddListener("*", xml);
            manager.SetTracingLevel("*", SourceLevels.All);

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
            System.Threading.Thread.Sleep(1000);
            xml.Close();

            Process.Start(SvcViewer, new FileInfo(LogFile).FullName);
        }

        [Fact]
        public void when_tracing_activity_then_can_render_activity_as_string()
        {
            var writer = new StringWriter();
            var text = new TextWriterTraceListener(writer);

            manager.AddListener("*", text);
            manager.SetTracingLevel("*", SourceLevels.All);

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

            System.Threading.Thread.Sleep(1000);

            text.Flush();

            Console.WriteLine(writer.ToString());
        }

        [Fact]
        public void when_tracing_activity_with_predefined_entity_in_name_then_succeeds()
        {
            var listener = new Mock<TraceListener>();
            manager.AddListener("Foo", listener.Object);

            manager.SetTracingLevel("Foo", SourceLevels.Information);

            var tracer = Tracer.Get("Foo");

            tracer.StartActivity("&");
        }
    }
}
