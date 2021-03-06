﻿using System;
using System.Linq;
using System.Reflection;
using Baseline;
using Baseline.Reflection;
using Jasper.Configuration;
using Jasper.Messaging.Configuration;
using Jasper.Messaging.Model;
using Jasper.Persistence;
using LamarCodeGeneration.Frames;
using LamarCodeGeneration.Model;

namespace Jasper.Messaging.Sagas
{
    public class SagaFramePolicy : IHandlerPolicy
    {
        public const string SagaIdPropertyName = "SagaId";
        public const string SagaIdVariableName = "sagaId";
        public const string IdentityMethodName = "Identity";
        public static readonly Type[] ValidSagaIdTypes = {typeof(Guid), typeof(int), typeof(long), typeof(string)};

        public void Apply(HandlerGraph graph, JasperGenerationRules rules)
        {
            foreach (var chain in graph.Chains.Where(IsSagaRelated)) Apply(chain, rules.SagaPersistence);
        }

        public void Apply(HandlerChain chain, ISagaPersistenceFrameProvider sagaPersistenceFrameProvider)
        {
            if (sagaPersistenceFrameProvider == null)
                throw new InvalidOperationException("No saga persistence strategy is registered.");

            var sagaStateType = DetermineSagaStateType(chain);
            var sagaIdType = sagaPersistenceFrameProvider.DetermineSagaIdType(sagaStateType);

            var sagaHandler = chain.Handlers.FirstOrDefault(x => x.HandlerType.Closes(typeof(StatefulSagaOf<>)));

            var existence = DetermineExistence(sagaHandler.As<HandlerCall>());


            Variable sagaIdVariable = null;
            if (existence == SagaStateExistence.Existing)
            {
                var identityMethod = sagaHandler
                    .HandlerType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(x => x.Name == IdentityMethodName && x.ReturnType == sagaIdType)
                    .FirstOrDefault(x => x.GetParameters().Any(p => p.ParameterType == chain.MessageType));

                var sagaId = ChooseSagaIdProperty(chain.MessageType);

                sagaIdVariable = createSagaIdVariable(sagaHandler.HandlerType, chain.MessageType, sagaId,
                    identityMethod, sagaIdType);


                chain.Middleware.Add(sagaIdVariable.Creator);
            }


            var existingState = sagaHandler.Creates.FirstOrDefault(x => x.VariableType == sagaStateType);

            // Tells the handler chain codegen to not use this as a cascading message
            existingState?.Properties.Add(HandlerChain.NotCascading, true);

            var persistenceFrame = sagaPersistenceFrameProvider.DeterminePersistenceFrame(sagaHandler, existence, ref sagaIdVariable,
                sagaStateType, existingState, out existingState);
            if (persistenceFrame != null) chain.Middleware.Add(persistenceFrame);

            if (existence == SagaStateExistence.Existing)
                chain.Middleware.Add(new AssertSagaStateExistsFrame(existingState, sagaIdVariable));

            var enlistInSagaId = MethodCall.For<IMessageContext>(x => x.EnlistInSaga(null));
            enlistInSagaId.Arguments[0] = sagaIdVariable;
            chain.Postprocessors.Add(enlistInSagaId);


            var storeOrDeleteFrame =
                sagaPersistenceFrameProvider.DetermineStoreOrDeleteFrame(sagaHandler, existingState, sagaHandler.HandlerType);
            chain.Postprocessors.Add(storeOrDeleteFrame);
        }

        private Variable createSagaIdVariable(Type handlerType, Type messageType, PropertyInfo sagaId,
            MethodInfo identityMethod, Type sagaIdType)
        {
            if (sagaId != null) return new PullSagaIdFromMessageFrame(messageType, sagaId).SagaId;

            if (identityMethod != null)
            {
                var call = new MethodCall(handlerType, identityMethod);
                call.ReturnVariable.OverrideName(SagaIdVariableName);

                return call.ReturnVariable;
            }

            return new PullSagaIdFromEnvelopeFrame(sagaIdType).SagaId;
        }

        public static SagaStateExistence DetermineExistence(HandlerCall sagaCall)
        {
            if (sagaCall.Method.Name == "Start" || sagaCall.Method.Name == "Starts") return SagaStateExistence.New;

            return SagaStateExistence.Existing;
        }

        public static Type DetermineSagaStateType(HandlerChain chain)
        {
            var handler = chain.Handlers.FirstOrDefault(x => x.HandlerType.Closes(typeof(StatefulSagaOf<>)));
            if (handler == null)
                throw new ArgumentOutOfRangeException(nameof(handler), "This chain is not a stateful saga");

            var handlerType = handler.HandlerType;
            while (handlerType.BaseType != typeof(object))
            {
                handlerType = handlerType.BaseType;
                if (handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(StatefulSagaOf<>))
                    return handlerType.GetGenericArguments().Single();
            }

            throw new ArgumentOutOfRangeException("Unable to determine a SagaState type for handler type " +
                                                  handler.HandlerType.GetFullName());
        }

        public static PropertyInfo ChooseSagaIdProperty(Type messageType)
        {
            var prop = messageType.GetProperties().FirstOrDefault(x => x.HasAttribute<SagaIdentityAttribute>())
                       ?? messageType.GetProperties().FirstOrDefault(x => x.Name == SagaIdPropertyName);

            return prop;
        }

        public static bool IsSagaRelated(HandlerChain chain)
        {
            return chain.Handlers.Any(x => x.HandlerType.Closes(typeof(StatefulSagaOf<>)));
        }
    }
}
