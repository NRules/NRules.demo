using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NRules.Samples.ClaimsExpert.Contract;

namespace NRules.Samples.ClaimsCenter.Cli.Output;

/// <summary>
/// Renders claims as plain aligned text: fixed-width columns, no box drawing and no colour, so
/// the output pipes cleanly into other tools.
/// </summary>
internal static class ClaimFormatter
{
    private const int ColumnGap = 2;

    public static void WriteList(TextWriter writer, IReadOnlyList<ClaimDto> claims)
    {
        if (claims.Count == 0)
        {
            writer.WriteLine("No claims.");
            return;
        }

        var rows = new List<string[]>(claims.Count + 1)
        {
            new[] { "ID", "TYPE", "STATUS", "PATIENT" },
        };

        foreach (var claim in claims)
        {
            rows.Add(new[]
            {
                claim.Id.ToString(CultureInfo.InvariantCulture),
                claim.ClaimType,
                claim.Status.ToString(),
                FormatPatientName(claim),
            });
        }

        WriteRows(writer, rows);
    }

    public static void WriteDetail(TextWriter writer, ClaimDto claim)
    {
        var fields = new (string Label, string Value)[]
        {
            ("Id", claim.Id.ToString(CultureInfo.InvariantCulture)),
            ("Claim Type", claim.ClaimType),
            ("Status", claim.Status.ToString()),
            ("Patient First Name", claim.PatientFirstName),
            ("Patient Middle Name", claim.PatientMiddleName),
            ("Patient Last Name", claim.PatientLastName),
            ("Patient Address Line1", claim.PatientAddressLine1),
            ("Patient Address Line2", claim.PatientAddressLine2),
            ("Patient Address City", claim.PatientAddressCity),
            ("Patient Address State", claim.PatientAddressState),
            ("Patient Address Zip", claim.PatientAddressZip),
        };

        var labelWidth = 0;
        foreach (var field in fields)
        {
            labelWidth = Math.Max(labelWidth, field.Label.Length);
        }

        foreach (var field in fields)
        {
            writer.WriteLine($"{field.Label.PadRight(labelWidth)}  {field.Value}".TrimEnd());
        }

        if (claim.Alerts.Count == 0)
        {
            return;
        }

        writer.WriteLine();

        var rows = new List<string[]>(claim.Alerts.Count + 1)
        {
            new[] { "RULE", "MESSAGE" },
        };

        foreach (var alert in claim.Alerts)
        {
            rows.Add(new[] { alert.RuleName, alert.Message });
        }

        WriteRows(writer, rows);
    }

    private static void WriteRows(TextWriter writer, IReadOnlyList<string[]> rows)
    {
        var columnCount = rows[0].Length;
        var widths = new int[columnCount];
        foreach (var row in rows)
        {
            for (var column = 0; column < columnCount; column++)
            {
                widths[column] = Math.Max(widths[column], row[column].Length);
            }
        }

        var line = new StringBuilder();
        foreach (var row in rows)
        {
            line.Clear();
            for (var column = 0; column < columnCount; column++)
            {
                line.Append(column == columnCount - 1
                    ? row[column]
                    : row[column].PadRight(widths[column] + ColumnGap));
            }

            writer.WriteLine(line.ToString().TrimEnd());
        }
    }

    private static string FormatPatientName(ClaimDto claim)
    {
        var last = claim.PatientLastName;
        var first = claim.PatientFirstName;

        if (last.Length == 0)
        {
            return first;
        }

        return first.Length == 0 ? last : $"{last}, {first}";
    }
}
