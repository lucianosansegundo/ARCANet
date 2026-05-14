using ARCANet.Authentication;
using ARCANet.InternalInvoices;
using ARCANet.Invoices;
using ARCANet.Tests.Invoices;
using ARCANet.Wsfev1;

namespace ARCANet.Tests.Wsfev1;

public sealed class WsfeSoapEnvelopeBuilderTests
{
    [Fact]
    public void BuildFeCaeSolicitar_ProducesExpectedCoreFields()
    {
        var builder = new WsfeSoapEnvelopeBuilder(new Wsfev1Options());
        var submission = new InvoiceSubmissionMapper().Map(TestInvoiceFactory.CreateValidFacturaARequest());
        var ticket = new AccessTicket("TOKEN", "SIGN", DateTimeOffset.UtcNow.AddHours(1));

        var xml = builder.BuildFeCaeSolicitar(ticket, submission);

        Assert.Contains("<ar:FECAESolicitar>", xml, StringComparison.Ordinal);
        Assert.Contains("<ar:Token>TOKEN</ar:Token>", xml, StringComparison.Ordinal);
        Assert.Contains("<ar:Sign>SIGN</ar:Sign>", xml, StringComparison.Ordinal);
        Assert.Contains("<ar:Cuit>20304050607</ar:Cuit>", xml, StringComparison.Ordinal);
        Assert.Contains("<ar:PtoVta>5</ar:PtoVta>", xml, StringComparison.Ordinal);
        Assert.Contains("<ar:CbteTipo>1</ar:CbteTipo>", xml, StringComparison.Ordinal);
        Assert.Contains("<ar:DocTipo>80</ar:DocTipo>", xml, StringComparison.Ordinal);
        Assert.Contains("<ar:DocNro>30712345678</ar:DocNro>", xml, StringComparison.Ordinal);
        Assert.Contains("<ar:CondicionIVAReceptorId>1</ar:CondicionIVAReceptorId>", xml, StringComparison.Ordinal);
    }
}
