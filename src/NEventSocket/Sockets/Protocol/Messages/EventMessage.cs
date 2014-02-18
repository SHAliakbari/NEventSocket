﻿namespace NEventSocket.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Common.Logging;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Sockets.Protocol;
    using NEventSocket.Util;

    /// <summary>
    ///     Represents an Event Message received through the EventSocket
    /// </summary>
    [Serializable]
    public class EventMessage : BasicMessage
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        protected EventMessage()
        {
        }

        public EventMessage(BasicMessage basicMessage)
        {
            if (basicMessage.ContentType != ContentTypes.EventPlain)
                throw new ArgumentException(
                    "Expected content type text/plain, got {0} instead.".Fmt(basicMessage.ContentType));

            if (string.IsNullOrEmpty(basicMessage.BodyText))
                throw new ArgumentException("Message did not contain an event body.");

            Headers = basicMessage.Headers;
            BodyText = basicMessage.BodyText;

            try
            {
                if (BodyText.Contains(HeaderNames.ContentLength))
                {
                    // need to parse this as a message with a body eg. BACKGROUND_JOB event
                    var parser = new Parser();
                    foreach (char c in this.BodyText)
                    {
                        parser.Append(c);
                    }

                    BasicMessage payload = parser.ParseMessage();

                    this.EventHeaders = payload.Headers;
                    this.BodyText = payload.BodyText.Trim();
                }
                else
                {
                    // body text consists of event headers only
                    this.EventHeaders = new Dictionary<string, string>(
                        this.BodyText.ParseKeyValuePairs("\n", ": "), StringComparer.OrdinalIgnoreCase);
                    this.BodyText = null;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to parse body of event", ex);
                Log.Error(BodyText);
                throw;
            }
        }

        public EventType EventType
        {
            get
            {
                return (EventType)Enum.Parse(typeof(EventType), EventHeaders[HeaderNames.EventName]);
            }
        }

        public ChannelState ChannelState
        {
            get
            {
                return (ChannelState)Enum.Parse(typeof(ChannelState), EventHeaders[HeaderNames.ChannelState]);
            }
        }

        public string AnswerState
        {
            get
            {
                //possible values: answered, hangup
                return EventHeaders[HeaderNames.AnswerState];
            }
        }

        public IReadOnlyDictionary<string, string> EventHeaders { get; protected set; }

        public string GetVariable(string variable)
        {
            return EventHeaders["variable_" + variable];
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Message Headers:");

            foreach (var h in Headers.OrderBy(x => x.Key))
                sb.AppendLine("\t" + h.Key + " : " + h.Value);

            sb.AppendLine("Event Headers:");

            foreach (var h in EventHeaders.OrderBy(x => x.Key))
                sb.AppendLine("\t" + h.Key + " : " + h.Value);

            if (!string.IsNullOrEmpty(BodyText))
            {
                sb.AppendLine("Body:");
                sb.AppendLine(BodyText);
            }

            return sb.ToString();
        }
    }
}