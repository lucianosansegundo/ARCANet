namespace ARCANet.Tests.Integration;

public sealed class PostgresIntegrationFactAttribute : FactAttribute
{
    public PostgresIntegrationFactAttribute()
    {
        Skip = PostgresIntegrationTestSettings.GetSkipReason();
    }
}
