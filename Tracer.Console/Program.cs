namespace TracerHub
{
    using Microsoft.AspNet.SignalR.Client;
    using Realtime;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using TracerHub.Diagnostics;

    class Program
    {
        static readonly ITracer tracer = Tracer.Get(typeof(Program));
        const string TracerHubUrl = "http://tracer.azurewebsites.net/";
        const string HubName = "Tracer";

        static void Main(string[] args)
        {
            Tracer.Initialize(new TracerManager());

            tracer.Info("Starting TracerHub Console");

            if (args.Length != 1)
            {
                Console.WriteLine("Usage: " + typeof(Program).Assembly.ManifestModule.FullyQualifiedName + " groupName");
                Console.WriteLine("Press [Enter] to exit.");
                Console.ReadLine();
                return;
            }

            var data = new Dictionary<string, string>
            {
                { "groupName", args[0] }
            };

            using (var hub = new HubConnection(TracerHubUrl, data))
            {
                IHubProxy proxy = hub.CreateHubProxy(HubName);
                IDisposable handler = proxy.On<TraceEvent>("TraceEvent", trace => Tracer.Get(trace.Source).Trace(trace.EventType, trace.Message));

                hub.Start().Wait();

                Console.WriteLine("Broadcast trace event: [E(rror)|I(nformation)|W(arning)]:[Source]:[Message]");
                Console.WriteLine("Quit: Q|q");
                var line = Console.ReadLine();

                while (!line.Equals("Q", StringComparison.InvariantCultureIgnoreCase))
                {
                    var trace = line.Split(new [] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (trace.Length == 3)
                    {
                        TraceEventType type = TraceEventType.Information;
                        switch (trace[0])
                        {
                            case "E":
                                type = TraceEventType.Error;
                                break;
                            case "W":
                                type = TraceEventType.Warning;
                                break;
                            default:
                                break;
                        }

                        proxy.Invoke("BroadcastTraceEvent", new TraceEvent
                        {
                            EventType = type,
                            Source = trace[1],
                            Message = trace[2],
                        });
                    }

                    line = Console.ReadLine();
                }
            }
        }
    }
}
