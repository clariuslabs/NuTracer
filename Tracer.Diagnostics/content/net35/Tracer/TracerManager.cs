﻿

namespace System.Diagnostics
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Implements the common tracer interface using <see cref="TraceSource"/> instances. 
    /// </summary>
    ///	<nuget id="Tracer.SystemDiagnostics" />
    partial class TracerManager : ITracerManager, IDisposable
    {
        /// <summary>
        /// Implicit default trace source name which can be used to setup 
        /// global tracing and listeners.
        /// </summary>
        public const string DefaultSourceName = "*";

        /// <summary>
        /// Gets a tracer instance with the specified name.
        /// </summary>
        public ITracer Get(string name)
        {
            return new AggregateTracer(name, CompositeFor(name)
                .Select(tracerName => new DiagnosticsTracer(
                    this.GetOrAdd(tracerName, sourceName => new TraceSource(sourceName)))));
        }

        /// <summary>
        /// Adds a listener to the source with the given <paramref name="sourceName"/>.
        /// </summary>
        public void AddListener(string sourceName, TraceListener listener)
        {
            this.GetOrAdd(sourceName, name => new TraceSource(name)).Listeners.Add(listener);
        }

        /// <summary>
        /// Removes a listener from the source with the given <paramref name="sourceName"/>.
        /// </summary>
        public void RemoveListener(string sourceName, TraceListener listener)
        {
            this.GetOrAdd(sourceName, name => new TraceSource(name)).Listeners.Remove(listener);
        }

        /// <summary>
        /// Removes a listener from the source with the given <paramref name="sourceName"/>.
        /// </summary>
        public void RemoveListener(string sourceName, string listenerName)
        {
            this.GetOrAdd(sourceName, name => new TraceSource(name)).Listeners.Remove(listenerName);
        }

        /// <summary>
        /// Sets the tracing level for the source with the given <paramref name="sourceName"/>
        /// </summary>
        public void SetTracingLevel(string sourceName, SourceLevels level)
        {
            this.GetOrAdd(sourceName, name => new TraceSource(name)).Switch.Level = level;
        }

        /// <summary>
        /// Cleans up the manager.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Gets the list of trace source names that are used to inherit trace source logging for the given <paramref name="type"/>.
        /// </summary>
        private static IEnumerable<string> CompositeFor(string name)
        {
            yield return DefaultSourceName;

            var indexOfGeneric = name.IndexOf('<');
            var indexOfLastDot = name.LastIndexOf('.');

            if (indexOfGeneric == -1 && indexOfLastDot == -1)
            {
                yield return name;
                yield break;
            }

            var parts = default(string[]);

            if (indexOfGeneric == -1)
                parts = name
                    .Substring(0, name.LastIndexOf('.'))
                    .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            else
                parts = name
                    .Substring(0, indexOfGeneric)
                    .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i <= parts.Length; i++)
            {
                yield return string.Join(".", parts, 0, i);
            }

            yield return name;
        }

        /// <summary>
        /// Gets an AppDomain-cached trace source of the given name, or creates it. 
        /// This means that even if multiple libraries are using their own 
        /// trace manager instance, they will all still share the same 
        /// underlying sources.
        /// </summary>
        private TraceSource GetOrAdd(string sourceName, Func<string, TraceSource> factory)
        {
            var cachedSources = AppDomain.CurrentDomain.GetData<Dictionary<string, TraceSource>>();
            if (cachedSources == null)
            {
                // This lock guarantees that throughout the current 
                // app domain, only a single root trace source is 
                // created ever.
                lock (AppDomain.CurrentDomain)
                {
                    cachedSources = AppDomain.CurrentDomain.GetData<Dictionary<string, TraceSource>>();
                    if (cachedSources == null)
                    {
                        cachedSources = new Dictionary<string, TraceSource>();
                        AppDomain.CurrentDomain.SetData(cachedSources);
                    }
                }
            }

            return cachedSources.GetOrAdd(sourceName, factory);
        }

        /// <summary>
        /// Logs to multiple tracers simulateously. Used for the 
        /// source "inheritance"
        /// </summary>
        private class AggregateTracer : ITracer
        {
            private List<DiagnosticsTracer> tracers;
            private string name;

            public AggregateTracer(string name, IEnumerable<DiagnosticsTracer> tracers)
            {
                this.name = name;
                this.tracers = tracers.ToList();
            }

            /// <summary>
            /// Traces the specified message with the given <see cref="TraceEventType"/>.
            /// </summary>
            public void Trace(TraceEventType type, object message)
            {
                tracers.ForEach(tracer => tracer.Trace(name, type, message));
            }

            /// <summary>
            /// Traces the specified formatted message with the given <see cref="TraceEventType"/>.
            /// </summary>
            public void Trace(TraceEventType type, string format, params object[] args)
            {
                tracers.ForEach(tracer => tracer.Trace(name, type, format, args));
            }

            /// <summary>
            /// Traces an exception with the specified message and <see cref="TraceEventType"/>.
            /// </summary>
            public void Trace(TraceEventType type, Exception exception, object message)
            {
                tracers.ForEach(tracer => tracer.Trace(name, type, exception, message));
            }

            /// <summary>
            /// Traces an exception with the specified formatted message and <see cref="TraceEventType"/>.
            /// </summary>
            public void Trace(TraceEventType type, Exception exception, string format, params object[] args)
            {
                tracers.ForEach(tracer => tracer.Trace(name, type, exception, format, args));
            }

            public override string ToString()
            {
                return "Aggregate sources for " + this.name;
            }
        }

        /// <summary>
        /// Implements the <see cref="ITracer"/> interface on top of 
        /// <see cref="TraceSource"/>.
        /// </summary>
        private class DiagnosticsTracer
        {
            private TraceSource source;

            public DiagnosticsTracer(TraceSource source)
            {
                this.source = source;
            }

            public void Trace(string sourceName, TraceEventType type, object message)
            {
                lock (source)
                {
                    using (new SourceNameReplacer(source, sourceName))
                    {
                        source.TraceEvent(type, 0, message.ToString());
                    }
                }
            }

            public void Trace(string sourceName, TraceEventType type, string format, params object[] args)
            {
                lock (source)
                {
                    using (new SourceNameReplacer(source, sourceName))
                    {
                        source.TraceEvent(type, 0, format, args);
                    }
                }
            }

            public void Trace(string sourceName, TraceEventType type, Exception exception, object message)
            {
                lock (source)
                {
                    using (new SourceNameReplacer(source, sourceName))
                    {
                        source.TraceEvent(type, 0, message.ToString() + Environment.NewLine + exception);
                    }
                }
            }

            public void Trace(string sourceName, TraceEventType type, Exception exception, string format, params object[] args)
            {
                lock (source)
                {
                    using (new SourceNameReplacer(source, sourceName))
                    {
                        source.TraceEvent(type, 0, string.Format(format, args) + Environment.NewLine + exception);
                    }
                }
            }

            /// <summary>
            /// The TraceSource instance name matches the name of each of the "segments" 
            /// we built the aggregate source from. This means that when we trace, we issue 
            /// multiple trace statements, one for each. If a listener is added to (say) "*" 
            /// source name, all traces done through it will appear as coming from the source 
            /// "*", rather than (say) "Foo.Bar" which might be the actual source class. 
            /// This diminishes the usefulness of hierarchical loggers significantly, since 
            /// it now means that you need to add listeners too all trace sources you're 
            /// interested in receiving messages from, and all its "children" potentially, 
            /// some of them which might not have been created even yet. This is not feasible.
            /// Instead, since we issue the trace call to each trace source (which is what 
            /// enables the configurability of all those sources in the app.config file), 
            /// we need to fix the source name right before tracing, so that a configured 
            /// listener at "*" still receives as the source name the original (aggregate) one, 
            /// and not "*". This requires some private reflection, and a lock to guarantee 
            /// proper logging, but this decreases its performance.
            /// </summary>
            private class SourceNameReplacer : IDisposable
            {
                // Private reflection needed here in order to make the inherited source names still 
                // log as if the original source name was the one logging, so as not to lose the 
                // originating class name.
                private static readonly FieldInfo sourceNameField = typeof(TraceSource).GetField("sourceName", BindingFlags.Instance | BindingFlags.NonPublic);

                private TraceSource source;
                private string originalName;

                public SourceNameReplacer(TraceSource source, string sourceName)
                {
                    this.source = source;
                    this.originalName = source.Name;
                    // Transient change of the source name while the trace call 
                    // is issued. Multi-threading might still cause messages to come 
                    // out with wrong source names :(
                    sourceNameField.SetValue(source, sourceName);
                }

                public void Dispose()
                {
                    sourceNameField.SetValue(source, originalName);
                }
            }
        }
    }
}
