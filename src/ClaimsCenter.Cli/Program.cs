using System;
using System.CommandLine;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NRules.Samples.ClaimsCenter.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var endpointOption = new Option<string?>("--endpoint")
        {
            Description = "Service address, overriding configuration.",
            Recursive = true,
        };
        var jsonOption = new Option<bool>("--json")
        {
            Description = "Emit JSON instead of formatted text.",
            Recursive = true,
        };

        var root = new RootCommand("Claims Center command line client.");

        var defaultVersionOption = root.Options.FirstOrDefault(option => option.Name == "--version");
        if (defaultVersionOption is not null)
        {
            root.Options.Remove(defaultVersionOption);
        }

        var versionOption = new Option<bool>("--version")
        {
            Description = "Show version information.",
        };

        root.Options.Add(endpointOption);
        root.Options.Add(jsonOption);
        root.Options.Add(versionOption);

        var idArgument = new Argument<long>("id") { Description = "Claim id." };

        var listCommand = new Command("list", "List all claims.");
        listCommand.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            Commands.ListAsync(
                parseResult.GetValue(endpointOption),
                parseResult.GetValue(jsonOption),
                cancellationToken));

        root.Subcommands.Add(listCommand);

        var showCommand = new Command("show", "Show one claim in full.");
        showCommand.Arguments.Add(idArgument);
        showCommand.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            Commands.ShowAsync(
                parseResult.GetValue(endpointOption),
                parseResult.GetValue(idArgument),
                parseResult.GetValue(jsonOption),
                cancellationToken));

        root.Subcommands.Add(showCommand);

        var adjudicateCommand = new Command("adjudicate", "Adjudicate a claim, then show the result.");
        adjudicateCommand.Arguments.Add(idArgument);
        adjudicateCommand.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            Commands.AdjudicateAsync(
                parseResult.GetValue(endpointOption),
                parseResult.GetValue(idArgument),
                parseResult.GetValue(jsonOption),
                cancellationToken));

        root.Subcommands.Add(adjudicateCommand);

        root.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
        {
            if (parseResult.GetValue(versionOption))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "Claims Center CLI";
                var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
                // The SDK appends "+<git-sha>" to the informational version for deterministic builds;
                // drop it so this matches the plain product version shown elsewhere (e.g. the UI's About dialog).
                var version = informationalVersion.Split('+')[0];
                Console.WriteLine($"{product} {version}");
                return Task.FromResult(ExitCodes.Success);
            }

            Console.Error.WriteLine("No command given. Run --help for usage.");
            return Task.FromResult(ExitCodes.UsageError);
        });

        var parseResult = root.Parse(args);

        if (parseResult.Errors.Count > 0)
        {
            // Stock System.CommandLine parse-error presentation writes help to the configured
            // Output stream, which defaults to stdout. Route it to stderr instead so a usage error
            // never pollutes a redirected --json stream with anything but JSON.
            var errorConfiguration = new InvocationConfiguration { Output = Console.Error, Error = Console.Error };
            return await parseResult.InvokeAsync(errorConfiguration);
        }

        return await parseResult.InvokeAsync();
    }
}
