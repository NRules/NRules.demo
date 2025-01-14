using System;
using Serilog;
using ILogger = Grpc.Core.Logging.ILogger;

namespace NRules.Samples.ClaimsExpert.Service.Infrastructure;

public class GrpcLogger : ILogger
{
    public ILogger ForType<T>()
    {
        return this;
    }

    public void Debug(string message)
    {
        Log.Debug(message);
    }

    public void Debug(string format, params object[] formatArgs)
    {
        Log.Debug(format, formatArgs);
    }

    public void Info(string message)
    {
        Log.Information(message);
    }

    public void Info(string format, params object[] formatArgs)
    {
        Log.Information(format, formatArgs);
    }

    public void Warning(string message)
    {
        Log.Warning(message);
    }

    public void Warning(string format, params object[] formatArgs)
    {
        Log.Warning(format, formatArgs);
    }

    public void Warning(Exception exception, string message)
    {
        Log.Warning(exception, message);
    }

    public void Error(string message)
    {
        Log.Error(message);
    }

    public void Error(string format, params object[] formatArgs)
    {
        Log.Error(format, formatArgs);
    }

    public void Error(Exception exception, string message)
    {
        Log.Error(exception, message);
    }
}