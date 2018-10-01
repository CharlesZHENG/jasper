using Jasper.Messaging;
using Jasper.Messaging.Logging;
using Jasper.Messaging.Model;
using Jasper.Messaging.Runtime.Serializers;
using Jasper.Messaging.Transports.Configuration;
using Lamar.IoC;
using System.Threading.Tasks;

namespace Jasper.Internal.Resolvers
{
    // START: Jasper_Messaging_IMessagingRoot_messagingRoot
    public class Jasper_Messaging_IMessagingRoot_messagingRoot : Lamar.IoC.Resolvers.SingletonResolver<Jasper.Messaging.IMessagingRoot>
    {
        private readonly Jasper.Messaging.Runtime.Serializers.MessagingSerializationGraph _messagingSerializationGraph;
        private readonly Jasper.Messaging.Transports.Configuration.MessagingSettings _messagingSettings;
        private readonly Jasper.Messaging.Model.HandlerGraph _handlerGraph;
        private readonly Jasper.Messaging.Logging.ITransportLogger _transportLogger1382737944;
        private readonly Jasper.Messaging.IChannelGraph _channelGraph;
        private readonly Jasper.Messaging.Logging.IMessageLogger _messageLogger;
        private readonly Lamar.IoC.Scope _topLevelScope;

        public Jasper_Messaging_IMessagingRoot_messagingRoot(Jasper.Messaging.Runtime.Serializers.MessagingSerializationGraph messagingSerializationGraph, Jasper.Messaging.Transports.Configuration.MessagingSettings messagingSettings, Jasper.Messaging.Model.HandlerGraph handlerGraph, [Lamar.Named("transportLogger2")] Jasper.Messaging.Logging.ITransportLogger transportLogger1382737944, Jasper.Messaging.IChannelGraph channelGraph, Jasper.Messaging.Logging.IMessageLogger messageLogger, Lamar.IoC.Scope topLevelScope) : base(topLevelScope)
        {
            _messagingSerializationGraph = messagingSerializationGraph;
            _messagingSettings = messagingSettings;
            _handlerGraph = handlerGraph;
            _transportLogger1382737944 = transportLogger1382737944;
            _channelGraph = channelGraph;
            _messageLogger = messageLogger;
            _topLevelScope = topLevelScope;
        }



        public override Jasper.Messaging.IMessagingRoot Build(Lamar.IoC.Scope scope)
        {
            var container = (Lamar.IContainer) scope;
            var nulloDurableMessagingFactory = new Jasper.Messaging.Transports.NulloDurableMessagingFactory(_transportLogger1382737944, _messagingSettings);
            return new Jasper.Messaging.MessagingRoot(_messagingSerializationGraph, _messagingSettings, _handlerGraph, nulloDurableMessagingFactory, _channelGraph, _messageLogger, container, _transportLogger1382737944);
        }

    }

    // END: Jasper_Messaging_IMessagingRoot_messagingRoot
    
    
}

