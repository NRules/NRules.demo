using System;
using System.IO;
using Autofac;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using NRules.Samples.ClaimsExpert.Domain.Modules;
using NRules.Samples.ClaimsExpert.Service.Data;

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
        // Microsoft.Data.Sqlite pools connections where the provider this replaced did not, so a
        // pooled connection outlives the session that opened it and the service goes on holding a
        // handle to the database file through idle periods. Deleting and regenerating that file
        // underneath a running service, which the data generator invites, would then leave it
        // reading the replaced file and unable to write to it, with no recovery short of a restart.
        var connectionString = new SqliteConnectionStringBuilder { DataSource = databaseFile, Pooling = false }.ToString();
        var configuration = Fluently.Configure()
            .Database(SQLiteConfiguration.Standard
                .ConnectionString(connectionString)
                .Driver<MicrosoftDataSqliteDriver>()
                // NHibernate's default keyword handling opens the session factory by reading the
                // provider's "DataTypes" schema collection. Microsoft.Data.Sqlite exposes only
                // "MetaDataCollections" and "ReservedWords", and throws ArgumentException for
                // anything else, so the import is turned off. Nothing in this schema is a SQLite
                // keyword, so there is nothing for the auto-quoting it drives to do.
                .Raw(NHibernate.Cfg.Environment.Hbm2ddlKeyWords, "none"))
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

        var resolvedFile = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, databaseFile));

        // SQLite creates a missing file on demand, which would start the service against an empty
        // database and fail every query instead of the configuration mistake. Report it up front,
        // naming both the configured value and what it resolved to, since the two rarely match.
        if (!File.Exists(resolvedFile))
        {
            throw new InvalidOperationException(
                $"Database file not found, or not a readable file. Configuration setting 'databaseFile' " +
                $"is '{databaseFile}', which resolves to '{resolvedFile}'. Run the DataGenerator project " +
                "to create the database, or set 'databaseFile' to the path of an existing one.");
        }

        return resolvedFile;
    }
}
