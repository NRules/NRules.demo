using System;
using System.Configuration;
using Autofac;
using Microsoft.Extensions.Configuration;
using NRules.Samples.ClaimsExpert.Service.Services;

namespace NRules.Samples.ClaimsExpert.Service.Modules
{
    public class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ServiceController>()
                .WithParameter(
                    (pi, c) => pi.Name == "grpcEndpointHostname",
                    (pi, c) => c.Resolve<IConfiguration>()["grpcEndpointHostname"])
                .WithParameter(
                    (pi, c) => pi.Name == "grpcEndpointPort",
                    (pi, c) => Int32.Parse(c.Resolve<IConfiguration>()["grpcEndpointPort"]!))
                .AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<AdjudicationServiceImpl>()
                .AsSelf().InstancePerDependency();
            builder.RegisterType<ClaimServiceImpl>()
                .AsSelf().InstancePerDependency();
            builder.RegisterType<NotificationService>()
                .AsImplementedInterfaces().InstancePerDependency();
        }
    }
}