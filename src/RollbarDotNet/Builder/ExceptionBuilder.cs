﻿namespace RollbarDotNet.Builder
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Payloads;
    using Exception = System.Exception;

    public class ExceptionBuilder : IExceptionBuilder
    {
        public void Execute(Payload payload, Exception exception)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            var traceChain = new List<Payloads.Trace>();
            this.BuildTraceList(exception, traceChain);
            if (traceChain.Count > 0)
            {
                payload.Data.Body.TraceChain = traceChain;
            }
        }

        protected void BuildTraceList(Exception exception, List<Payloads.Trace> traceList)
        {
            var trace = new Payloads.Trace();
            trace.Exception = this.BuildException(exception);
            trace.Frames = this.BuildFrames(exception);
            traceList.Add(trace);
            if (exception.InnerException != null)
            {
                this.BuildTraceList(exception.InnerException, traceList);
            }
        }

        protected List<Frame> BuildFrames(Exception exception)
        {
            var frames = new List<Frame>();
            var stacktrace = new StackTrace(exception, true);
            var stackTraceFrames = stacktrace.GetFrames();
            if (stackTraceFrames == null)
            {
                return frames;
            }
            foreach (var stackTraceFrame in stackTraceFrames)
            {
                var method = stackTraceFrame.GetMethod();
                var methodParameters = method.GetParameters();
                var parameters = methodParameters.Length == 0
                    ? string.Empty
                    : method.GetParameters()
                        .Select(p => $"{p.ParameterType.FullName} {p.Name}")
                        .Aggregate((p1, p2) => $"{p1}, {p2}");
                var methodName = $"{method.DeclaringType?.FullName ?? "(unknown)"}.{method.Name}({parameters})";
                var frame = new Frame
                {
                    Filename = stackTraceFrame.GetFileName(),
                    ColumnNumber = stackTraceFrame.GetFileColumnNumber(),
                    LineNumber = stackTraceFrame.GetFileLineNumber(),
                    Method = methodName
                };
                frames.Add(frame);
            }

            if (exception.InnerException != null)
            {
                frames.AddRange(this.BuildFrames(exception.InnerException));
            }

            return frames;
        }

        protected Payloads.Exception BuildException(Exception exception)
        {
            var payloadException = new Payloads.Exception();
            payloadException.Class = exception.GetType().Name;
            payloadException.Message = exception.Message;
            return payloadException;
        }
    }
}