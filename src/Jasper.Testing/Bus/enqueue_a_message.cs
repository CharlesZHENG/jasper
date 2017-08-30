﻿using System;
using System.Threading.Tasks;
using Baseline;
using Jasper.Bus;
using Jasper.Testing.Bus.Compilation;
using Jasper.Testing.Bus.Runtime;
using Jasper.Testing.Bus.Transports;
using Jasper.Testing.Http;
using NSubstitute.Routing.Handlers;
using Shouldly;
using Xunit;

namespace Jasper.Testing.Bus
{
    public class enqueue_a_message
    {
        [Fact]
        public async Task enqueue_locally()
        {
            var registry = new JasperRegistry();
            registry.Handlers.ConventionalDiscoveryDisabled = false;
            registry.Handlers.IncludeType<RecordCallHandler>();
            registry.Services.ForSingletonOf<IFakeStore>().Use<FakeStore>();
            registry.Services.AddService<IFakeService, FakeService>();
            registry.Services.AddService<IWidget, Widget>();

            var tracker = new MessageTracker();
            registry.Services.For<MessageTracker>().Use(tracker);

            using (var runtime = JasperRuntime.For(registry))
            {
                var waiter = tracker.WaitFor<Message1>();
                var message = new Message1
                {
                    Id = Guid.NewGuid()
                };

                await runtime.Get<IServiceBus>().Enqueue(message);

                var received = await waiter;

                received.Message.As<Message1>().Id.ShouldBe(message.Id);
            }
        }
    }
}
