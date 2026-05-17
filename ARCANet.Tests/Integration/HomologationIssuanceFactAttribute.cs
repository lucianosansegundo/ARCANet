namespace ARCANet.Tests.Integration;

public sealed class HomologationIssuanceFactAttribute : FactAttribute
{
    public HomologationIssuanceFactAttribute()
    {
        Skip = HomologationTestSettings.GetIssuanceSkipReason();
    }
}
