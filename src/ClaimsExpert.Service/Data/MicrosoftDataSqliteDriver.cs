using NHibernate.Driver;

namespace NRules.Samples.ClaimsExpert.Service.Data;

/// <summary>
/// Connects NHibernate to Microsoft.Data.Sqlite. NHibernate ships SQLite drivers for
/// System.Data.SQLite and CsharpSqlite only; the first has no arm64 build and the second is
/// unmaintained, so the provider is bound here by reflection instead. Microsoft.Data.Sqlite
/// carries native builds for x64 and arm64 on Linux, macOS and Windows.
/// </summary>
public class MicrosoftDataSqliteDriver : ReflectionBasedDriver
{
    public MicrosoftDataSqliteDriver()
        : base("Microsoft.Data.Sqlite",
               "Microsoft.Data.Sqlite.SqliteConnection",
               "Microsoft.Data.Sqlite.SqliteCommand")
    {
    }

    public override bool UseNamedPrefixInSql => true;

    public override bool UseNamedPrefixInParameter => true;

    public override string NamedPrefix => "@";

    // NHibernate's own SQLite20Driver declares this false as well, and NHibernate wraps
    // readers in NHybridDataReader either way, so nothing is gained by claiming support.
    public override bool SupportsMultipleOpenReaders => false;
}
