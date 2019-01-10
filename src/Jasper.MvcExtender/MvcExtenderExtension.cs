using System.Net.Http;
using Baseline;
using Baseline.Reflection;
using Jasper;
using Jasper.Configuration;
using Jasper.Http.ContentHandling;
using Jasper.Http.Routing;
using Jasper.MvcExtender;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

[assembly:JasperModule(typeof(MvcExtenderExtension))]

namespace Jasper.MvcExtender
{
    public class MvcExtenderExtension : IJasperExtension
    {
        public void Configure(JasperOptionsBuilder registry)
        {
            registry.HttpRoutes.IncludeTypes(x => x.CanBeCastTo<ControllerBase>());
            registry.HttpRoutes.IncludeMethods(x => x.HasAttribute<HttpMethodAttribute>());

            registry.HttpRoutes.GlobalPolicy<ControllerUsagePolicy>();

            registry.Services.AddSingleton<IWriterRule, ActionResultWriterRule>();

            RouteBuilder.PatternRules.Insert(0, new HttpAttributePatternRule());
        }
    }
}