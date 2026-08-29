namespace NRules.Samples.ClaimsCenter.Cli;

/// <summary>
/// Process exit codes. <see cref="UsageError"/> is produced by System.CommandLine itself when
/// parsing fails; it is named here so the documented contract lives in one place.
/// </summary>
internal static class ExitCodes
{
    public const int Success = 0;
    public const int UsageError = 1;
    public const int RpcFailure = 2;
}
