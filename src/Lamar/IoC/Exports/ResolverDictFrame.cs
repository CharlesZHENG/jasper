﻿using System;
using System.Collections.Generic;
using System.Linq;
using Lamar.Codegen;
using Lamar.Codegen.Frames;
using Lamar.Compilation;
using Lamar.IoC.Resolvers;
using Lamar.Util;

namespace Lamar.IoC.Exports
{
    public class ResolverDictFrame : SyncFrame
    {
        private readonly Dictionary<string, string> _typenames;

        public ResolverDictFrame(Dictionary<string, string> typenames)
        {
            _typenames = typenames;
        }

        public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
        {
            var values = _typenames.Select(x => $"{{\"{x.Key}\", typeof({x.Value})}}").Join(", ");

            writer.Write($"return new {typeof(Dictionary<string, Type>).FullNameInCode()}{{{values}}};");
        }
    }

}