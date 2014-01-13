using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Tracing.Realtime;
using Xunit;

namespace Tracing
{
    public class RealtimeSpec
    {
        const string TracerHubUrl = "http://tracer.azurewebsites.net/";
        const string HubName = "Tracer";

        [Fact]
        public void when_tracing_via_hub_then_client_gets_trace()
        {
            var traces = new List<TraceEvent>();
            var source = new TraceSource("Source", SourceLevels.Information);
            using (var listener = new RealtimeTraceListener("Test"))
            {
                source.Listeners.Add(listener);

                var data = new Dictionary<string, string>
                {
                    { "groupName", "Test" }
                };

                using (var hub = new HubConnection(TracerHubUrl, data))
                {
                    IHubProxy proxy = hub.CreateHubProxy(HubName);
                    proxy.On<TraceEvent>("TraceEvent", trace => traces.Add(trace));

                    hub.Start().Wait();

                    source.TraceInformation("Foo");

                    var watch = Stopwatch.StartNew();
                    var timeout = TimeSpan.FromSeconds(2);
                    while (watch.Elapsed < timeout)
                    {
                        Thread.Sleep(100);
                    }

                    Assert.Equal(1, traces.Count);
                    Assert.Equal(TraceEventType.Information, traces[0].EventType);
                    Assert.Equal("Source", traces[0].Source);
                    Assert.Equal("Foo", traces[0].Message);
                }
            }
        }

        public class TraceEvent
        {
            public TraceEventType EventType { get; set; }
            public string Message { get; set; }
            public string Source { get; set; }
        }
    }
}
