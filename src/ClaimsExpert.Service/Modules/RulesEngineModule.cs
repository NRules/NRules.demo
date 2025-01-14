using Autofac;
using NRules.Fluent;
using NRules.RuleModel;
using NRules.Samples.ClaimsExpert.Rules;
using NRules.Samples.ClaimsExpert.Service.Infrastructure;

namespace NRules.Samples.ClaimsExpert.Service.Modules;

public class RulesEngineModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<RuleActivator>()
            .AsImplementedInterfaces().InstancePerDependency();
        builder.RegisterType<RuleDependencyResolver>()
            .AsImplementedInterfaces().InstancePerDependency();

        var scanner = new RuleTypeScanner();
        scanner.AssemblyOf(typeof(DslExtensions));
        var types = scanner.GetRuleTypes();

        builder.RegisterTypes(types).AsSelf();

        builder.RegisterType<RuleRepository>()
            .AsImplementedInterfaces().SingleInstance()
            .OnActivating(e => e.Instance.Load(x => x.From(types)))
            .PropertiesAutowired();

        builder.Register(c => c.Resolve<IRuleRepository>().Compile())
            .As<ISessionFactory>().SingleInstance()
            .OnActivating(e => new RulesEngineLogger(e.Instance))
            .PropertiesAutowired()
            .AutoActivate();
    }
}