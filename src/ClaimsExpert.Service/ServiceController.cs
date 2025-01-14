using System;
using Grpc.Core;
using NRules.Samples.ClaimsExpert.Contract;
using NRules.Samples.ClaimsExpert.Service.Infrastructure;
using NRules.Samples.ClaimsExpert.Service.Services;

namespace NRules.Samples.ClaimsExpert.Service;

public interface IServiceController
{
    void Start();
}

internal class ServiceController : IServiceController, IDisposable
{
    private Server? _server;

    public ServiceController(ClaimServiceImpl claimService, AdjudicationServiceImpl adjudicationService, string grpcEndpointHostname, int grpcEndpointPort)
    {
        GrpcEnvironment.SetLogger(new GrpcLogger());

        _server = new Server
        {
            Services =
            {
                ClaimService.BindService(claimService),
                AdjudicationService.BindService(adjudicationService)
            },
            Ports = {new ServerPort(grpcEndpointHostname, grpcEndpointPort, ServerCredentials.Insecure)}
        };
    }

    public void Start()
    {
        _server?.Start();
    }

    public void Stop()
    {
        _server?.ShutdownAsync().Wait();
        _server = null;
    }

    public void Dispose()
    {
        Stop();
    }
}