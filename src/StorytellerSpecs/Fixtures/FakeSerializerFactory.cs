﻿using System;
using System.IO;
using System.Threading.Tasks;
using Jasper.Conneg;
using Microsoft.AspNetCore.Http;

namespace StorytellerSpecs.Fixtures
{
    internal class FakeSerializerFactory : ISerializerFactory, IMessageDeserializer, IMessageSerializer
    {
        public FakeSerializerFactory(string contentType)
        {
            ContentType = contentType;
        }

        public string MessageType { get; }

        Type IMessageDeserializer.DotNetType => throw new NotImplementedException();

        public object ReadFromData(byte[] data)
        {
            throw new NotImplementedException();
        }

        public Task<T> ReadFromRequest<T>(HttpRequest request)
        {
            throw new NotImplementedException();
        }

        Type IMessageSerializer.DotNetType => throw new NotImplementedException();

        public byte[] Write(object model)
        {
            throw new NotImplementedException();
        }

        public Task WriteToStream(object model, HttpResponse response)
        {
            throw new NotImplementedException();
        }

        public object Deserialize(Stream message)
        {
            throw new NotImplementedException();
        }

        public string ContentType { get; }

        public IMessageDeserializer ReaderFor(Type messageType)
        {
            return null;
        }

        public IMessageSerializer WriterFor(Type messageType)
        {
            return new FakeWriter(messageType, ContentType);
        }

        public void Serialize(object message, Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
