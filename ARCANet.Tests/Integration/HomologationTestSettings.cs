using System.Globalization;

namespace ARCANet.Tests.Integration;

internal sealed record HomologationTestSettings
{
    public const string RunTestsVariable = "ARCANET_RUN_HOMOLOGATION_TESTS";
    public const string CuitVariable = "ARCANET_TEST_CUIT";
    public const string CertificatePathVariable = "ARCANET_TEST_CERTIFICATE_PATH";
    public const string CertificatePasswordVariable = "ARCANET_TEST_CERTIFICATE_PASSWORD";
    public const string PointOfSaleVariable = "ARCANET_TEST_POINT_OF_SALE";
    public const string VoucherTypeCodeVariable = "ARCANET_TEST_VOUCHER_TYPE";
    public const string VoucherTypeNameVariable = "ARCANET_TEST_VOUCHER_TYPE_NAME";
    public const string ExistingVoucherNumberVariable = "ARCANET_TEST_EXISTING_VOUCHER_NUMBER";
    public const string HttpTimeoutSecondsVariable = "ARCANET_TEST_HTTP_TIMEOUT_SECONDS";

    public required long Cuit { get; init; }
    public required string CertificatePath { get; init; }
    public string? CertificatePassword { get; init; }
    public required int PointOfSale { get; init; }
    public required int VoucherTypeCode { get; init; }
    public required string VoucherTypeName { get; init; }
    public long? ExistingVoucherNumber { get; init; }
    public required TimeSpan HttpTimeout { get; init; }

    public static string? GetSkipReason()
    {
        if (!IsEnabled())
        {
            return $"Set {RunTestsVariable}=true to enable homologation integration tests.";
        }

        var errors = ValidateConfiguration(includeExistingVoucher: false);
        return errors.Count == 0 ? null : string.Join(" ", errors);
    }

    public static string? GetExistingVoucherSkipReason()
    {
        var baseReason = GetSkipReason();
        if (baseReason is not null)
        {
            return baseReason;
        }

        var errors = ValidateConfiguration(includeExistingVoucher: true);
        return errors.Count == 0 ? null : string.Join(" ", errors);
    }

    public static HomologationTestSettings Load()
    {
        var skipReason = GetSkipReason();
        if (skipReason is not null)
        {
            throw new InvalidOperationException(skipReason);
        }

        return new HomologationTestSettings
        {
            Cuit = ParseLong(GetRequired(CuitVariable), CuitVariable),
            CertificatePath = Path.GetFullPath(GetRequired(CertificatePathVariable)),
            CertificatePassword = Environment.GetEnvironmentVariable(CertificatePasswordVariable),
            PointOfSale = ParseInt(GetRequired(PointOfSaleVariable), PointOfSaleVariable),
            VoucherTypeCode = ParseOptionalInt(Environment.GetEnvironmentVariable(VoucherTypeCodeVariable), 6),
            VoucherTypeName = Environment.GetEnvironmentVariable(VoucherTypeNameVariable) ?? "Factura B",
            ExistingVoucherNumber = ParseOptionalLong(Environment.GetEnvironmentVariable(ExistingVoucherNumberVariable)),
            HttpTimeout = TimeSpan.FromSeconds(ParseOptionalInt(Environment.GetEnvironmentVariable(HttpTimeoutSecondsVariable), 45))
        };
    }

    private static List<string> ValidateConfiguration(bool includeExistingVoucher)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(CuitVariable)))
        {
            errors.Add($"Set {CuitVariable}.");
        }

        var certificatePath = Environment.GetEnvironmentVariable(CertificatePathVariable);
        if (string.IsNullOrWhiteSpace(certificatePath))
        {
            errors.Add($"Set {CertificatePathVariable}.");
        }
        else if (!File.Exists(Path.GetFullPath(certificatePath)))
        {
            errors.Add($"{CertificatePathVariable} does not point to an existing file.");
        }

        var pointOfSale = Environment.GetEnvironmentVariable(PointOfSaleVariable);
        if (string.IsNullOrWhiteSpace(pointOfSale))
        {
            errors.Add($"Set {PointOfSaleVariable}.");
        }
        else if (!int.TryParse(pointOfSale, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPointOfSale) || parsedPointOfSale <= 0)
        {
            errors.Add($"{PointOfSaleVariable} must be a positive integer.");
        }

        var voucherType = Environment.GetEnvironmentVariable(VoucherTypeCodeVariable);
        if (!string.IsNullOrWhiteSpace(voucherType) &&
            (!int.TryParse(voucherType, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedVoucherType) || parsedVoucherType <= 0))
        {
            errors.Add($"{VoucherTypeCodeVariable} must be a positive integer when set.");
        }

        if (includeExistingVoucher)
        {
            var existingVoucher = Environment.GetEnvironmentVariable(ExistingVoucherNumberVariable);
            if (string.IsNullOrWhiteSpace(existingVoucher))
            {
                errors.Add($"Set {ExistingVoucherNumberVariable} to query a known voucher.");
            }
            else if (!long.TryParse(existingVoucher, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedVoucher) || parsedVoucher <= 0)
            {
                errors.Add($"{ExistingVoucherNumberVariable} must be a positive integer.");
            }
        }

        return errors;
    }

    private static bool IsEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable(RunTestsVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);

    private static string GetRequired(string variableName) =>
        Environment.GetEnvironmentVariable(variableName)
        ?? throw new InvalidOperationException($"Missing required environment variable {variableName}.");

    private static long ParseLong(string value, string variableName)
    {
        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"{variableName} must be an integer.");
        }

        return parsed;
    }

    private static int ParseInt(string value, string variableName)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"{variableName} must be an integer.");
        }

        return parsed;
    }

    private static int ParseOptionalInt(string? value, int fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private static long? ParseOptionalLong(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }
}
