namespace ARCANet.Tests.Integration;

public sealed class HomologationExistingVoucherFactAttribute : FactAttribute
{
    public HomologationExistingVoucherFactAttribute()
    {
        Skip = HomologationTestSettings.GetExistingVoucherSkipReason();
    }
}
