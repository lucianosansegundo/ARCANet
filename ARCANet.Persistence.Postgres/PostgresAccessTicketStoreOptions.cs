using System.Text.RegularExpressions;

namespace ARCANet.Persistence.Postgres;

public sealed class PostgresAccessTicketStoreOptions
{
    private static readonly Regex IdentifierRegex = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant);

    public string SchemaName { get; init; } = "public";

    public string TableName { get; init; } = "arca_access_tickets";

    internal string GetQualifiedTableName() =>
        $"{QuoteIdentifier(ValidateIdentifier(SchemaName, nameof(SchemaName)))}.{QuoteIdentifier(ValidateIdentifier(TableName, nameof(TableName)))}";

    internal static string ValidateIdentifier(string value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        if (!IdentifierRegex.IsMatch(value))
        {
            throw new ArgumentException(
                $"PostgreSQL identifier '{value}' is invalid. Use only letters, digits, and underscores, and do not start with a digit.",
                paramName);
        }

        return value;
    }

    internal static string QuoteIdentifier(string identifier) =>
        $"\"{identifier}\"";
}
