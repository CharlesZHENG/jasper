﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Lamar.Codegen;
using Lamar.Compilation;
using Lamar.IoC;
using Lamar.IoC.Enumerables;
using Lamar.IoC.Exports;
using Lamar.IoC.Instances;
using Lamar.IoC.Lazy;
using Lamar.IoC.Resolvers;
using Lamar.Scanning.Conventions;
using Lamar.Util;
using Microsoft.Extensions.DependencyInjection;

namespace Lamar
{
    public class ServiceGraph : IDisposable
    {
        private readonly Scope _rootScope;
        private readonly object _familyLock = new object();
        


        private readonly Dictionary<Type, ServiceFamily> _families = new Dictionary<Type, ServiceFamily>();
        private ImHashMap<Type, Func<Scope, object>> _byType = ImHashMap<Type, Func<Scope, object>>.Empty;


        public static async Task<ServiceGraph> BuildAsync(IServiceCollection services, Scope rootScope)
        {
            var (registry, scanners) = await ScanningExploder.Explode(services);

            return new ServiceGraph(registry, rootScope, scanners);
        }
        
        
        private ServiceGraph(IServiceCollection services, Scope rootScope, AssemblyScanner[] scanners)
        {
            _services = services as ServiceRegistry ?? new ServiceRegistry(services);
            Scanners = scanners;
            _rootScope = rootScope;
            organize(_services);
        }

        public ServiceGraph(IServiceCollection services, Scope rootScope)
        {
            var (registry, scanners) = ScanningExploder.ExplodeSynchronously(services);

            Scanners = scanners;
            
            _services = registry;

            _rootScope = rootScope;

            organize(_services);
        }
        
        internal readonly Dictionary<string, Type> CachedResolverTypes = new Dictionary<string, Type>();

        private void organize(ServiceRegistry services)
        {
            DecoratorPolicies = services.FindAndRemovePolicies<IDecoratorPolicy>();
            
            FamilyPolicies = services.FindAndRemovePolicies<IFamilyPolicy>()
                .Concat(new IFamilyPolicy[]
                {
                    new EnumerablePolicy(),
                    new FuncOrLazyPolicy(),
                    new CloseGenericFamilyPolicy(),
                    new ConcreteFamilyPolicy(),
                    new EmptyFamilyPolicy()
                })
                .ToArray();

            InstancePolicies = services.FindAndRemovePolicies<IInstancePolicy>();

            var policies = services.FindAndRemovePolicies<IRegistrationPolicy>();
            foreach (var policy in policies)
            {
                policy.Apply(services);
            }

            var sets = services.FindAndRemovePolicies<CachedResolverSet>();
            foreach (var resolverSet in sets)
            {
                if (resolverSet.TryLoadResolvers(out var dict))
                {
                    foreach (var pair in dict)
                    {
                        CachedResolverTypes.Add(pair.Key, pair.Value);
                    }
                }
            }


            addScopeResolver<Scope>(services);
            addScopeResolver<IServiceProvider>(services);
            addScopeResolver<IContainer>(services);
            addScopeResolver<IServiceScopeFactory>(services);
        }


        public IDecoratorPolicy[] DecoratorPolicies { get; private set; } = new IDecoratorPolicy[0];

        internal void Inject(Type serviceType, object @object)
        {
            _byType = _byType.AddOrUpdate(serviceType, s => @object);
        }

        public IInstancePolicy[] InstancePolicies { get; set; }

        public IFamilyPolicy[] FamilyPolicies { get; private set; }

        private void addScopeResolver<T>(IServiceCollection services)
        {
            var instance = new ScopeInstance<T>();
            services.Add(instance);
        }

        public void Initialize(PerfTimer timer = null)
        {
            timer = timer ?? new PerfTimer();

            timer.Record("Organize Into Families", () =>
            {
                organizeIntoFamilies(_services);
            });

            timer.Record("Planning Instances", buildOutMissingResolvers);

            
            rebuildReferencedAssemblyArray();
        }


        private void rebuildReferencedAssemblyArray()
        {
            _allAssemblies = AllInstances().SelectMany(x => x.ReferencedAssemblies())
                .Distinct().ToArray();
        }


        private void buildOutMissingResolvers()
        {
            if (_inPlanning) return;

            _inPlanning = true;

            try
            {
                planResolutionStrategies();
            }
            finally
            {
                _inPlanning = false;
            }
        }


        internal GeneratedAssembly ToGeneratedAssembly(string @namespace = null)
        {
            // TODO -- will need to get at the GenerationRules from somewhere
            var generatedAssembly = new GeneratedAssembly(new GenerationRules(@namespace ?? "Jasper.Generated"));

            generatedAssembly.Generation.Assemblies.Fill(_allAssemblies);

            return generatedAssembly;
        }

        private bool _inPlanning = false;

        private void planResolutionStrategies()
        {
            while (AllInstances().Where(x => !x.ServiceType.IsOpenGeneric()).Any(x => !x.HasPlanned))
            {
                foreach (var instance in AllInstances().Where(x => !x.HasPlanned).ToArray())
                {
                    instance.CreatePlan(this);
                }
            }
        }

        internal Instance FindInstance(ParameterInfo parameter)
        {
            if (parameter.HasAttribute<NamedAttribute>())
            {
                var att = parameter.GetAttribute<NamedAttribute>();
                if (att.TypeName.IsNotEmpty())
                {
                    var family = _families.Values.ToArray().FirstOrDefault(x => x.FullNameInCode == att.TypeName);
                    return family.InstanceFor(att.Name);
                }

                return FindInstance(parameter.ParameterType, att.Name);
            }

            return FindDefault(parameter.ParameterType);
        }

        private void organizeIntoFamilies(IServiceCollection services)
        {
            services
                .Where(x => !x.ServiceType.HasAttribute<LamarIgnoreAttribute>())

                .GroupBy(x => x.ServiceType)
                .Select(group => buildFamilyForInstanceGroup(services, @group))
                .Each(family => _families.Add(family.ServiceType, family));


        }

        private ServiceFamily buildFamilyForInstanceGroup(IServiceCollection services, IGrouping<Type, ServiceDescriptor> @group)
        {
            if (@group.Key.IsGenericType && !@group.Key.IsOpenGeneric())
            {
                return buildClosedGenericType(@group.Key, services);
            }

            var instances = @group
                .Select(Instance.For)
                .ToArray();
            
            return new ServiceFamily(@group.Key, DecoratorPolicies, instances);
        }

        private ServiceFamily buildClosedGenericType(Type serviceType, IServiceCollection services)
        {
            var closed = services.Where(x => x.ServiceType == serviceType).Select(Instance.For);

            var templated = services
                .Where(x => x.ServiceType.IsOpenGeneric() && serviceType.Closes(x.ServiceType))
                .Select(Instance.For)
                .Select(instance =>
                {
                    var arguments = serviceType.GetGenericArguments();

                    try
                    {
                        return instance.CloseType(serviceType, arguments);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                })
                .Where(x => x != null);



            var instances = templated.Concat(closed).ToArray();

            return new ServiceFamily(serviceType, DecoratorPolicies, instances);
        }

        public IServiceCollection Services => _services;

        public IEnumerable<Instance> AllInstances()
        {
            return _families.Values.ToArray().SelectMany(x => x.All).ToArray();
        }

        public IReadOnlyDictionary<Type, ServiceFamily> Families => _families;

        public bool HasFamily(Type serviceType)
        {
            return _families.ContainsKey(serviceType);
        }

        public Instance FindInstance(Type serviceType, string name)
        {
            return ResolveFamily(serviceType).InstanceFor(name);
        }

        public ServiceFamily ResolveFamily(Type serviceType)
        {
            if (_families.ContainsKey(serviceType)) return _families[serviceType];

            lock (_familyLock)
            {
                if (_families.ContainsKey(serviceType)) return _families[serviceType];

                return addMissingFamily(serviceType);
            }
        }

        private ServiceFamily addMissingFamily(Type serviceType)
        {
            var family = TryToCreateMissingFamily(serviceType);

            _families.SmartAdd(serviceType, family);

            if (!_inPlanning)
            {
                buildOutMissingResolvers();

                if (family != null)
                {
                    rebuildReferencedAssemblyArray();
                }
            }

            return family;
        }

        public Func<Scope, object> FindResolver(Type serviceType)
        {
            if (_byType.TryFind(serviceType, out Func<Scope, object> resolver))
            {
                return resolver;
            }

            lock (_familyLock)
            {
                if (_byType.TryFind(serviceType, out resolver))
                {
                    return resolver;
                }

                var family = _families.ContainsKey(serviceType)
                    ? _families[serviceType]
                    : addMissingFamily(serviceType);

                var instance = family.Default;
                if (instance == null)
                {
                    resolver = null;
                }
                else if (instance.Lifetime == ServiceLifetime.Singleton)
                {
                    var inner = instance.ToResolver(_rootScope);
                    resolver = s =>
                    {
                        var value = inner(s);
                        Inject(serviceType, value);

                        return value;
                    };
                }
                else
                {
                    resolver = instance.ToResolver(_rootScope);
                }

                _byType = _byType.AddOrUpdate(serviceType, resolver);

                return resolver;
            }
        }

        public Instance FindDefault(Type serviceType)
        {
            if (serviceType.IsSimple()) return null;

            return ResolveFamily(serviceType)?.Default;
        }

        public Instance[] FindAll(Type serviceType)
        {
            return ResolveFamily(serviceType)?.All ?? new Instance[0];
        }

        public bool CouldBuild(Type concreteType)
        {
            var constructorInstance = new ConstructorInstance(concreteType, concreteType, ServiceLifetime.Transient);
            foreach (var policy in InstancePolicies)
            {
                policy.Apply(constructorInstance);
            }
            
            var ctor = constructorInstance.DetermineConstructor(this, out string message);
            
            
            return ctor != null && message.IsEmpty();
        }

        public void Dispose()
        {
            foreach (var instance in AllInstances().OfType<IDisposable>())
            {
                instance.SafeDispose();
            }
        }

        private readonly Stack<Instance> _chain = new Stack<Instance>();
        private Assembly[] _allAssemblies;
        private readonly ServiceRegistry _services;

        internal AssemblyScanner[] Scanners { get; private set; } = new AssemblyScanner[0];

        internal void StartingToPlan(Instance instance)
        {
            if (_chain.Contains(instance))
            {
                throw new InvalidOperationException("Bi-directional dependencies detected:" + Environment.NewLine + _chain.Select(x => x.ToString()).Join(Environment.NewLine));
            }

            _chain.Push(instance);
        }

        internal void FinishedPlanning()
        {
            _chain.Pop();
        }

        public static ServiceGraph Empty()
        {
            return Scope.Empty().ServiceGraph;
        }

        public static ServiceGraph For(Action<ServiceRegistry> configure)
        {
            var registry = new ServiceRegistry();
            configure(registry);

            return new Scope(registry).ServiceGraph;
        }

        public ServiceFamily TryToCreateMissingFamily(Type serviceType)
        {
            // TODO -- will need to make this more formal somehow
            if (serviceType.IsSimple() || serviceType.IsDateTime() || serviceType == typeof(TimeSpan) || serviceType.IsValueType || serviceType == typeof(DateTimeOffset)) return new ServiceFamily(serviceType, DecoratorPolicies);


            return FamilyPolicies.FirstValue(x => x.Build(serviceType, this));
        }

        internal void ClearPlanning()
        {
            _chain.Clear();
        }

        public bool CouldResolve(Type type)
        {
            return FindDefault(type) != null;
        }


        public void AppendServices(IServiceCollection services)
        {
            lock (_familyLock)
            {
                var (registry, scanners) = ScanningExploder.ExplodeSynchronously(services);

                Scanners = Scanners.Union(scanners).ToArray();

                registry
                    .Where(x => !x.ServiceType.HasAttribute<LamarIgnoreAttribute>())

                    .GroupBy(x => x.ServiceType)
                    .Each(group =>
                    {
                        if (_families.ContainsKey(group.Key))
                        {
                            var family = _families[group.Key];
                            if (family.Append(@group, DecoratorPolicies) == AppendState.NewDefault)
                            {
                                _byType = _byType.Remove(group.Key);
                            }
                        
                        }
                        else
                        {
                            var family = buildFamilyForInstanceGroup(services, @group);
                            _families.Add(@group.Key, family);
                        }
                    });

                buildOutMissingResolvers();
            
                rebuildReferencedAssemblyArray();
            }

        }

        internal void Inject(ObjectInstance instance)
        {
            if (_families.ContainsKey(instance.ServiceType))
            {
                if (_families[instance.ServiceType].Append(instance, DecoratorPolicies) == AppendState.NewDefault)
                {
                    _byType = _byType.Remove(instance.ServiceType);
                }
            }
            else
            {
                var family = new ServiceFamily(instance.ServiceType, DecoratorPolicies, instance);
                _families.Add(instance.ServiceType, family);
            }
        }
    }
}
