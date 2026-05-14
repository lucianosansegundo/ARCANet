namespace ARCANet.Tests.Integration;

public sealed class HomologationFactAttribute : FactAttribute
{
    public HomologationFactAttribute()
    {
        Skip = HomologationTestSettings.GetSkipReason();
    }
}
