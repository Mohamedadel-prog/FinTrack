
namespace FinTrack.Domain.Common;

public static class Currencies
{
    public const string Default = "EGP";

    public static readonly IReadOnlyList<string> Supported =
    [
        "EGP",
        "USD",
        "EUR",
        "GBP",
        "SAR",
        "AED",
        "KWD",
        "QAR"
    ];

    public static bool IsSupported(string? code) =>
        code is not null && Supported.Contains(code);
}
