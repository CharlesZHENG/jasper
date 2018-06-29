﻿using System;
using System.Collections.Generic;
using Lamar.Codegen.Frames;
using Lamar.Codegen.Variables;
using Lamar.Compilation;
using Microsoft.Extensions.DependencyInjection;

namespace Lamar.Codegen.ServiceLocation
{
    public class ServiceScopeFactoryCreation : SyncFrame
    {
        private readonly Variable _factory;
        private readonly Variable _scope;

        public ServiceScopeFactoryCreation()
        {

            _scope = new Variable(typeof(IServiceScope), this);
            Provider = new Variable(typeof(IServiceProvider), this);
        }

        public ServiceScopeFactoryCreation(Variable factory) : this()
        {
            _factory = factory;
            uses.Add(factory);
        }

        public Variable Provider { get; }

        public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
        {
            writer.UsingBlock($"var {_scope.Usage} = {_factory.Usage}.{nameof(IServiceScopeFactory.CreateScope)}()", w =>
            {
                w.Write($"var {Provider.Usage} = {_scope.Usage}.{nameof(IServiceScope.ServiceProvider)};");
                Next?.GenerateCode(method, w);
            });
        }

    }
}
