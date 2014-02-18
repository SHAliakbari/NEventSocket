﻿namespace NEventSocket.Sockets.Implementation
{
    using System;
    using System.Net.Sockets;
    using System.Reactive.Linq;
    using System.Security;
    using System.Threading.Tasks;

    using Common.Logging;

    using NEventSocket.FreeSwitch;
    using NEventSocket.Messages;
    using NEventSocket.Sockets.Protocol;

    public class InboundSocket : EventSocket
    {
        private static readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly string password;

        public InboundSocket(string host = "localhost", int port = 8021, string password = "ClueCon")
            : base(new TcpClient(host, port))
        {
            this.password = password;

            MessagesReceived
                    .Where(x => x.ContentType == ContentTypes.AuthRequest)
                    .Take(1)
                    .Subscribe(x => Authenticate());
        }

        public event EventHandler Authenticated = (sender, args) => { };

        public Task<EventMessage> Originate(string args)
        {
            var tcs = new TaskCompletionSource<EventMessage>();

            //we'll get an event in the future for this channel and we'll use that to complete the task
            var subscription = EventsReceived.Where(
                x => x.EventType == EventType.CHANNEL_ANSWER || x.EventType == EventType.CHANNEL_DESTROY)
                                             .Take(1) //will auto terminate the subscription
                                             .Subscribe(x =>
                                                 {
                                                     Log.DebugFormat("Originate {0} complete - {1}", args, x.EventHeaders[HeaderNames.AnswerState]);
                                                     tcs.SetResult(x);
                                                 });

            this.BgApi("originate " + args)
                .ContinueWith(
                    t =>
                        {
                            if ((t.IsFaulted && t.Exception != null) || (t.Result != null && !t.Result.Success))
                            {
                                //we're never going to get anevent because we didn't send the command successfully
                                subscription.Dispose();
                                //fail the parent task
                                //tcs.SetException(t.Exception);

                                tcs.SetResult(null);
                            }
                        });

            return tcs.Task;
        }

        private async Task Authenticate()
        {
            var result = await this.Auth(password);
            if (result.Success)
            {
                Log.Debug("Authenticated.");
                Authenticated(this, EventArgs.Empty);
            }
            else
            {
                Log.ErrorFormat("Invalid Password {0}", result.BodyText);
                throw new SecurityException("Invalid Password");
            }
        }
    }
}