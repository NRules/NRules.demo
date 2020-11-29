using Autofac;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using NRules.Samples.ClaimsExpert.Contract;

namespace NRules.Samples.ClaimsCenter.Applications.Modules
{
    public class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => new Channel(c.Resolve<IConfiguration>()["grpcEndpointAddress"], ChannelCredentials.Insecure))
                .As<Channel>().SingleInstance();
            builder.Register(c => new ClaimService.ClaimServiceClient(c.Resolve<Channel>()))
                .AsSelf().SingleInstance();
            builder.Register(c => new AdjudicationService.AdjudicationServiceClient(c.Resolve<Channel>()))
                .AsSelf().SingleInstance();
        }
    }
}