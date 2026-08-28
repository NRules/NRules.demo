using Autofac;
using NRules.Samples.ClaimsExpert.Service.Infrastructure;
using NRules.Samples.ClaimsExpert.Service.Services;

namespace NRules.Samples.ClaimsExpert.Service.Modules;

public class ServiceModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ClaimMapper>()
            .AsImplementedInterfaces().SingleInstance();
        builder.RegisterType<AdjudicationServiceImpl>()
            .AsSelf().InstancePerDependency();
        builder.RegisterType<ClaimServiceImpl>()
            .AsSelf().InstancePerDependency();
        builder.RegisterType<NotificationService>()
            .AsImplementedInterfaces().InstancePerDependency();
    }
}
