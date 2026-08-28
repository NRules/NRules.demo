using Autofac;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using NRules.Samples.ClaimsExpert.Contract;

namespace NRules.Samples.ClaimsCenter.Applications.Modules;

public class ServiceModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(c => GrpcChannel.ForAddress(c.Resolve<IConfiguration>()["grpcEndpointAddress"]!))
            .As<GrpcChannel>().SingleInstance();
        builder.Register(c => new ClaimService.ClaimServiceClient(c.Resolve<GrpcChannel>()))
            .AsSelf().SingleInstance();
        builder.Register(c => new AdjudicationService.AdjudicationServiceClient(c.Resolve<GrpcChannel>()))
            .AsSelf().SingleInstance();
    }
}
