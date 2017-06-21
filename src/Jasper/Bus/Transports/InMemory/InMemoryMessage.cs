﻿using System;
using System.Collections.Generic;
using Jasper.Bus.Runtime;

namespace Jasper.Bus.Transports.InMemory
{
    public class InMemoryMessage
    {
        public InMemoryMessage(object message, IDictionary<string, string> headers, DateTime sentAt)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Headers = headers;
            SentAt = sentAt;
        }

        public InMemoryMessage(byte[] data, IDictionary<string, string> headers, DateTime sentAt)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Headers = headers;
            SentAt = sentAt;
        }

        public static InMemoryMessage ForEnvelope(Envelope envelope)
        {
            if (envelope.Message != null)
            {
                return new InMemoryMessage(envelope.Message, envelope.Headers, DateTime.UtcNow);
            }
            else if (envelope.Data != null && envelope.Data.Length > 0)
            {
                return new InMemoryMessage(envelope.Data, envelope.Headers, DateTime.UtcNow);
            }

            throw new ArgumentOutOfRangeException($"Either the data or the message have to be supplied on the envelope");
        }


        public object Message { get; }
        public byte[] Data { get; }
        public IDictionary<string, string> Headers { get; }
        public Guid Id { get; private set; } = Guid.NewGuid();
        public DateTime SentAt { get; }

        public void ReplaceId()
        {
            Id = Guid.NewGuid();
        }
    }
}
