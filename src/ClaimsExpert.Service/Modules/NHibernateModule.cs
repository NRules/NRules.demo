using System;
using System.IO;
using Autofac;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.Extensions.Configuration;
using NRules.Samples.ClaimsExpert.Domain.Modules;

namespace NRules.Samples.ClaimsExpert.Service.Modules;

public class NHibernateModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(c => CreateSessionFactory(c.Resolve<IConfiguration>()))
            .As<NHibernate.ISessionFactory>().SingleInstance()
            .AutoActivate();
        builder.Register(c => c.Resolve<NHibernate.ISessionFactory>().OpenSession())
            .As<NHibernate.ISession>().InstancePerDependency();
    }

    private NHibernate.ISessionFactory CreateSessionFactory(IConfiguration config)
    {
        var databaseFile = ResolveDatabaseFile(config["databaseFile"]);
        var configuration = Fluently.Configure()
            .Database(SQLiteConfiguration.Standard.UsingFile(databaseFile))
            .Mappings(m => m.FluentMappings.AddFromAssemblyOf<DomainModule>());
        var sessionFactory = configuration.BuildSessionFactory();
        return sessionFactory;
    }

    /// <summary>
    /// Resolves the configured database file against the application's base directory, so that
    /// the same relative path works regardless of the current working directory.
    /// </summary>
    private static string ResolveDatabaseFile(string? databaseFile)
    {
        if (String.IsNullOrWhiteSpace(databaseFile))
        {
            throw new InvalidOperationException("Configuration setting 'databaseFile' is not set.");
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, databaseFile));
    }
}
