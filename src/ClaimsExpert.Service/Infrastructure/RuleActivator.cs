using System;
using System.Collections.Generic;
using Autofac;
using NRules.Fluent;
using NRules.Fluent.Dsl;

namespace NRules.Samples.ClaimsExpert.Service.Infrastructure
{
    public class RuleActivator : IRuleActivator
    {
        private readonly ILifetimeScope _scope;

        public RuleActivator(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public IEnumerable<Rule> Activate(Type type)
        {
            yield return (Rule)_scope.Resolve(type);
        }
    }
}