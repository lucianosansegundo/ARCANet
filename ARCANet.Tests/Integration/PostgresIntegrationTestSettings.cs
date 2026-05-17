namespace ARCANet.Tests.Integration;

internal static class PostgresIntegrationTestSettings
{
    public const string RunTestsVariable = "ARCANET_RUN_POSTGRES_INTEGRATION_TESTS";
    public const string ImageVariable = "ARCANET_TEST_POSTGRES_IMAGE";

    public static string? GetSkipReason()
    {
        if (!IsEnabled())
        {
            return $"Set {RunTestsVariable}=true to enable PostgreSQL integration tests.";
        }

        return null;
    }

    public static string GetImage() =>
        Environment.GetEnvironmentVariable(ImageVariable) ?? "postgres:17";

    private static bool IsEnabled() =>
        string.Equals(
            Environment.GetEnvironmentVariable(RunTestsVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);
}
