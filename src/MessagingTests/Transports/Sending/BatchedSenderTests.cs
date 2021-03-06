﻿using System.Threading;
using System.Threading.Tasks;
using Baseline;
using Jasper.Messaging.Logging;
using Jasper.Messaging.Runtime;
using Jasper.Messaging.Transports;
using Jasper.Messaging.Transports.Sending;
using Jasper.Messaging.Transports.Tcp;
using NSubstitute;
using Xunit;

namespace MessagingTests.Transports.Sending
{
    public class BatchedSenderTests
    {
        public BatchedSenderTests()
        {
            theSender = new BatchedSender(TransportConstants.RepliesUri, theProtocol, theCancellation.Token,
                TransportLogger.Empty());
            theSender.Start(theSenderCallback);

            theBatch = new OutgoingMessageBatch(theSender.Destination, new[]
            {
                Envelope.ForPing(TransportConstants.LoopbackUri),
                Envelope.ForPing(TransportConstants.LoopbackUri),
                Envelope.ForPing(TransportConstants.LoopbackUri),
                Envelope.ForPing(TransportConstants.LoopbackUri),
                Envelope.ForPing(TransportConstants.LoopbackUri),
                Envelope.ForPing(TransportConstants.LoopbackUri)
            });

            theBatch.Messages.Each(x => x.Destination = theBatch.Destination);
        }

        private readonly ISenderProtocol theProtocol = Substitute.For<ISenderProtocol>();
        private readonly CancellationTokenSource theCancellation = new CancellationTokenSource();
        private readonly BatchedSender theSender;
        private readonly ISenderCallback theSenderCallback = Substitute.For<ISenderCallback>();
        private readonly OutgoingMessageBatch theBatch;

        [Fact]
        public async Task call_send_batch_if_not_latched_and_not_cancelled()
        {
            await theSender.SendBatch(theBatch);

#pragma warning disable 4014
            theProtocol.Received().SendBatch(theSenderCallback, theBatch);
#pragma warning restore 4014
        }

        [Fact]
        public async Task do_not_call_send_batch_if_cancelled()
        {
            theCancellation.Cancel();

            await theSender.SendBatch(theBatch);

#pragma warning disable 4014
            theProtocol.DidNotReceive().SendBatch(theSenderCallback, theBatch);
#pragma warning restore 4014
        }

        [Fact]
        public async Task do_not_call_send_batch_if_latched()
        {
            await theSender.LatchAndDrain();

            await theSender.SendBatch(theBatch);

#pragma warning disable 4014
            theProtocol.DidNotReceive().SendBatch(theSenderCallback, theBatch);

            theSenderCallback.Received().SenderIsLatched(theBatch);
#pragma warning restore 4014
        }
    }
}
