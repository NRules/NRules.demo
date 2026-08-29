using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace NRules.Samples.DataGenerator;

internal class Program
{
    private static void Main(string[] args)
    {
        string databaseFile = args.Length == 1
            ? Path.GetFullPath(args[0])
            : ResolvePath("..", "..", "..", "..", "Data", "ClaimsExpert.sqlite");

        if (File.Exists(databaseFile))
        {
            Console.WriteLine("Database file already exists. File={0}", databaseFile);
            Console.WriteLine("Delete the database file if you want it to get regenerated.");
            return;
        }

        EnsureDirectoryExists(databaseFile);

        Console.WriteLine("Creating database. File={0}", databaseFile);

        // Opening the connection creates the file; the provider has no explicit create.
        string connectionString = new SqliteConnectionStringBuilder { DataSource = databaseFile }.ToString();
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var scripts = new[] { "Schema.sql", "Data.sql" };
        foreach (var script in scripts)
        {
            var scriptFile = ResolvePath("Scripts", script);
            Console.WriteLine("Executing script. File={0}", scriptFile);
            string sql = File.ReadAllText(scriptFile);
            var command = new SqliteCommand(sql, connection);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Resolves a path relative to the application's base directory, so that the same
    /// relative path works regardless of the current working directory.
    /// </summary>
    private static string ResolvePath(params string[] parts)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, Path.Combine(parts)));
    }

    private static void EnsureDirectoryExists(string databaseFile)
    {
        var path = Path.GetDirectoryName(databaseFile);
        if (path != null && !Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
