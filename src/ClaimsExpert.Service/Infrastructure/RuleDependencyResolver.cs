using System;
using Autofac;
using NRules.Extensibility;

namespace NRules.Samples.ClaimsExpert.Service.Infrastructure
{
    public class RuleDependencyResolver : IDependencyResolver
    {
        private readonly ILifetimeScope _scope;

        public RuleDependencyResolver(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public object Resolve(IResolutionContext context, Type serviceType)
        {
            return _scope.Resolve(serviceType);
        }
    }
}