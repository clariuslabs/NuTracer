#region BSD License
/* 
Copyright (c) 2011, Clarius Consulting
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, 
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list 
  of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this 
  list of conditions and the following disclaimer in the documentation and/or other 
  materials provided with the distribution.

* Neither the name of Clarius Consulting nor the names of its contributors may be 
  used to endorse or promote products derived from this software without specific 
  prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY 
EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES 
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT 
SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, 
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR 
BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN 
ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH 
DAMAGE.
*/
#endregion

namespace TracerHub
{
    using Microsoft.AspNet.SignalR.Client;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Tracing;
    using Tracing.SystemDiagnostics;

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
                Console.WriteLine("Press 'Q' to exit.");
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

#if DEBUG
                //hub.TraceLevel = TraceLevels.All;
                //hub.TraceWriter = Console.Out;
#endif

                hub.Start().Wait();

                Console.WriteLine("Send trace event:  [E(rror)|I(nformation)|W(arning)]:[Source]:[Message]");
                Console.WriteLine("Set tracing level: [Source]=[Off|Critical|Error|Warning|Information|Verbose|All]");
                Console.WriteLine("Press 'Q' to exit.");
                var line = Console.ReadLine();

                while (!line.Equals("Q", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (line.IndexOf(':') != -1)
                    {
                        var trace = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (trace.Length == 3)
                        {
                            var type = TraceEventType.Information;
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

                            proxy.Invoke("TraceEvent", new TraceEvent
                            {
                                EventType = type,
                                Source = trace[1],
                                Message = trace[2],
                            });
                        }
                        else
                        {
                            Console.WriteLine("Send trace event:  [E(rror)|I(nformation)|W(arning)]:[Source]:[Message]");
                        }
                    }
                    else if (line.IndexOf('=') != -1)
                    {
                        var trace = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        SourceLevels level;
                        if (trace.Length == 2 && Enum.TryParse<SourceLevels>(trace[1], out level))
                        {
                            proxy.Invoke("SetTracingLevel", trace[0], level);
                        }
                        else
                        {
                            Console.WriteLine("Set tracing level: [Source]=[Off|Critical|Error|Warning|Information|Verbose|All]");
                        }
                    }

                    line = Console.ReadLine();
                }
            }
        }
    }
}
