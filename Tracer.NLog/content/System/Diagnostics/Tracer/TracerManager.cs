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

namespace System.Diagnostics
{
    using NLog;

    /// <summary>
    /// Implements the common tracer interface over NLog. 
    /// </summary>
    /// <remarks>
    /// The following table shows the mapping between <see cref="TraceEventType"/> 
    /// and the corresponding invoked log4net <see cref="Logger"/> members.
    /// <list type="table">
    ///     <listheader>
    ///         <term>TraceEventType</term>
    ///         <description>Logger</description>
    ///     </listheader>
    ///     <item>
    ///         <term><see cref="TraceEventType.Critical"/></term>
    ///         <description><see cref=""/><see cref="Logger.Fatal(object)"/></description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="TraceEventType.Error"/></term>
    ///         <description><see cref=""/><see cref="Logger.Error(object)"/></description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="TraceEventType.Information"/></term>
    ///         <description><see cref=""/><see cref="Logger.Info(object)"/></description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="TraceEventType.Verbose"/></term>
    ///         <description><see cref=""/><see cref="Logger.Debug(object)"/></description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="TraceEventType.Warning"/></term>
    ///         <description><see cref=""/><see cref="Logger.Warn(object)"/></description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="TraceEventType.Resume"/>, <see cref="TraceEventType.Start"/>, 
    ///         <see cref="TraceEventType.Stop"/>, <see cref="TraceEventType.Suspend"/>, <see cref="TraceEventType.Transfer"/></term>
    ///         <description><see cref=""/>Ignored</description>
    ///     </item>
    /// </list>
    /// The corresponding formatted message overload is called as appropriate, as well 
    /// as the overload receiving an <see cref="Exception"/>.
    /// </remarks>
    ///	<nuget id="Tracer.log4net" />
    partial class TracerManager : ITracerManager
    {
        /// <summary>
        /// Gets a tracer instance with the specified name.
        /// </summary>
        public ITracer Get(string name)
        {
            return new NLogAdapter(LogManager.GetLogger(name));
        }

        private class NLogAdapter : ITracer
        {
            private Logger log;

            public NLogAdapter(Logger log)
            {
                this.log = log;
            }

            public void Trace(TraceEventType type, object message)
            {
                switch (type)
                {
                    case TraceEventType.Critical:
                        this.log.Fatal(message);
                        break;
                    case TraceEventType.Error:
                        this.log.Error(message);
                        break;
                    case TraceEventType.Information:
                        this.log.Info(message);
                        break;
                    case TraceEventType.Verbose:
                        this.log.Debug(message);
                        break;
                    case TraceEventType.Warning:
                        this.log.Warn(message);
                        break;
                    case TraceEventType.Resume:
                    case TraceEventType.Start:
                    case TraceEventType.Stop:
                    case TraceEventType.Suspend:
                    case TraceEventType.Transfer:
                    default:
                        break;
                }
            }

            public void Trace(TraceEventType type, string format, params object[] args)
            {
                switch (type)
                {
                    case TraceEventType.Critical:
                        this.log.Fatal(format, args);
                        break;
                    case TraceEventType.Error:
                        this.log.Error(format, args);
                        break;
                    case TraceEventType.Information:
                        this.log.Info(format, args);
                        break;
                    case TraceEventType.Verbose:
                        this.log.Debug(format, args);
                        break;
                    case TraceEventType.Warning:
                        this.log.Warn(format, args);
                        break;
                    case TraceEventType.Resume:
                    case TraceEventType.Start:
                    case TraceEventType.Stop:
                    case TraceEventType.Suspend:
                    case TraceEventType.Transfer:
                    default:
                        break;
                }
            }

            public void Trace(TraceEventType type, Exception exception, object message)
            {
                switch (type)
                {
                    case TraceEventType.Critical:
                        this.log.FatalException(message.ToString(), exception);
                        break;
                    case TraceEventType.Error:
                        this.log.ErrorException(message.ToString(), exception);
                        break;
                    case TraceEventType.Information:
                        this.log.InfoException(message.ToString(), exception);
                        break;
                    case TraceEventType.Verbose:
                        this.log.DebugException(message.ToString(), exception);
                        break;
                    case TraceEventType.Warning:
                        this.log.WarnException(message.ToString(), exception);
                        break;
                    case TraceEventType.Resume:
                    case TraceEventType.Start:
                    case TraceEventType.Stop:
                    case TraceEventType.Suspend:
                    case TraceEventType.Transfer:
                    default:
                        break;
                }
            }

            public void Trace(TraceEventType type, Exception exception, string format, params object[] args)
            {
                switch (type)
                {
                    case TraceEventType.Critical:
                        this.log.FatalException(string.Format(format, args), exception);
                        break;
                    case TraceEventType.Error:
                        this.log.ErrorException(string.Format(format, args), exception);
                        break;
                    case TraceEventType.Information:
                        this.log.InfoException(string.Format(format, args), exception);
                        break;
                    case TraceEventType.Verbose:
                        this.log.DebugException(string.Format(format, args), exception);
                        break;
                    case TraceEventType.Warning:
                        this.log.WarnException(string.Format(format, args), exception);
                        break;
                    case TraceEventType.Resume:
                    case TraceEventType.Start:
                    case TraceEventType.Stop:
                    case TraceEventType.Suspend:
                    case TraceEventType.Transfer:
                    default:
                        break;
                }
            }
        }
    }
}
