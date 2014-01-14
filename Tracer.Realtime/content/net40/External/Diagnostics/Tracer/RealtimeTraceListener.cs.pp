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

namespace $rootnamespace$.Diagnostics
{
    using Microsoft.AspNet.SignalR.Client;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Provides a <see cref="TraceListener"/> implementation that connects to a 
    /// remote SignalR hub to dispatch calls to its <c>TraceEvent</c> overloads.
    /// </summary>
    public partial class RealtimeTraceListener : TraceListener
    {
        /// <summary>
        /// The SignalR hosted hub. Change to your own host URL if self-hosting.
        /// </summary>
        const string TracerHubUrl = "http://tracer.azurewebsites.net/";
        const string HubName = "Tracer";

        static readonly string hubUrl;
        string groupName;

        /// <summary>
        /// Initializes the Hub URL from the <c>HubUrl</c> appSettings configuration 
        /// value, or sets it to the default tracer hub.
        /// </summary>
        static RealtimeTraceListener()
        {
            hubUrl = ConfigurationManager.AppSettings["HubUrl"];
            if (string.IsNullOrEmpty(hubUrl))
                hubUrl = TracerHubUrl;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RealtimeTraceListener"/> class.
        /// </summary>
        /// <param name="groupName">Name of the group within the hub to broadcast traces to.</param>
        public RealtimeTraceListener(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                throw new ArgumentException("String value for groupName cannot be empty or null.", "groupName");

            this.groupName = groupName;
        }

        ~RealtimeTraceListener()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the SignalR hub, if connected. Null otherwise.
        /// </summary>
        public HubConnection Hub { get; private set; }
        
        /// <summary>
        /// Gets the SignalR hub proxy, if connected. Null otherwise.
        /// </summary>
        public IHubProxy Proxy { get; private set; }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if ((this.Filter == null) || this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                DoTraceEvent(eventCache, source, eventType, id, message);
            }
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if ((this.Filter == null) || this.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
            {
                if (args == null || args.Length == 0)
                    DoTraceEvent(eventCache, source, eventType, id, format);
                else
                    DoTraceEvent(eventCache, source, eventType, id, string.Format(CultureInfo.InvariantCulture, format, args));
            }
        }

        /// <summary>
        /// No-op, since this listener only pushes <c>TraceEvent</c> calls.
        /// </summary>
        public override void Write(string message)
        {
        }

        /// <summary>
        /// No-op, since this listener only pushes <c>TraceEvent</c> calls.
        /// </summary>
        public override void WriteLine(string message)
        {
        }

        /// <summary>
        /// Disposes the underlying SignalR hub, if connected.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Hub != null)
            {
                Hub.Dispose();
                Hub = null;
            }
        }

        /// <summary>
        /// Invoked when the <see cref="Hub"/> and <see cref="Proxy"/> are created 
        /// but before connecting to the remote hub via the <c>Start</c> method 
        /// on the <see cref="Hub"/>.
        /// </summary>
        partial void OnConnecting();

        /// <summary>
        /// Invoked when the <see cref="Hub"/> and <see cref="Proxy"/> are created 
        /// and connected to the remote hub.
        /// </summary>
        partial void OnConnected();

        // NOTE: Tracer.SystemDiagnostics for .NET 4.0+ (required to use this listener)
        // always traces asynchronously, so it's fine to just Wait() here for the connection, 
        // since this would NOT be slowing down the app in any way. Also, Tracer will trace 
        // in a single background thread, so this is automatically "thread-safe" without needing 
        // an explicit lock.
        private void DoTraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (Hub == null)
            {
                var data = new Dictionary<string, string>
                {
                    { "groupName", groupName }
                };

                Hub = new HubConnection(hubUrl, data);
                Proxy = Hub.CreateHubProxy(HubName);
                OnConnecting();
                Hub.Start().Wait();
                OnConnected();

            }

            Proxy.Invoke("BroadcastTraceEvent", new TraceEventInfo
            {
                EventType = eventType,
                Source = source,
                Message = message,
            });
        }

        /// <summary>
        /// Payload data to send to the hub about the traced event.
        /// </summary>
        partial class TraceEventInfo
        {
            /// <summary>
            /// Gets or sets the type of the event trace.
            /// </summary>
            public TraceEventType EventType { get; set; }

            /// <summary>
            /// Gets or sets the trace message.
            /// </summary>
            public string Message { get; set; }

            /// <summary>
            /// Gets or sets the source of the trace event.
            /// </summary>
            public string Source { get; set; }
        }
    }
}
