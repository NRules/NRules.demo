using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using NRules.Samples.ClaimsExpert.Contract;

namespace NRules.Samples.ClaimsCenter.Cli.Output;

/// <summary>
/// Emits claims as protobuf JSON. Default values are formatted explicitly: without that, proto3
/// omits them, so a claim in the Open state (enum value 0) would carry no Status field at all and
/// empty address lines would vanish.
/// </summary>
internal static class JsonOutput
{
    private static readonly JsonFormatter Formatter = new(
        JsonFormatter.Settings.Default.WithFormatDefaultValues(true).WithIndentation("  "));

    public static void WriteClaim(TextWriter writer, ClaimDto claim)
    {
        writer.WriteLine(NormalizeNewLines(Formatter.Format(claim)));
    }

    public static void WriteClaims(TextWriter writer, IReadOnlyList<ClaimDto> claims)
    {
        if (claims.Count == 0)
        {
            writer.WriteLine("[]");
            return;
        }

        writer.WriteLine("[");
        for (var index = 0; index < claims.Count; index++)
        {
            writer.Write(Indent(Formatter.Format(claims[index])));
            writer.WriteLine(index == claims.Count - 1 ? string.Empty : ",");
        }

        writer.WriteLine("]");
    }

    private static string Indent(string json)
    {
        var lines = json.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            lines[index] = "  " + lines[index].TrimEnd('\r');
        }

        return string.Join(Environment.NewLine, lines);
    }

    // WriteClaims normalises \n to Environment.NewLine as part of indenting each element; WriteClaim
    // has nothing to indent but needs the same normalisation, so that on Windows show --json and
    // list --json use the same line endings rather than list --json alone using CRLF.
    private static string NormalizeNewLines(string json)
    {
        var lines = json.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            lines[index] = lines[index].TrimEnd('\r');
        }

        return string.Join(Environment.NewLine, lines);
    }
}
