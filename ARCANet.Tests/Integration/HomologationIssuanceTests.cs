using ARCANet.Invoices;

namespace ARCANet.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class HomologationIssuanceTests(HomologationFixture fixture) : IClassFixture<HomologationFixture>
{
    private readonly HomologationFixture _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));

    [HomologationIssuanceFact]
    public async Task InvoiceClient_CreateInvoiceAsync_AuthorizesNewFacturaB()
    {
        var settings = _fixture.Settings;
        var series = new VoucherSeries(
            settings.Cuit,
            settings.PointOfSale,
            new VoucherType(6, "Factura B"));

        var lastAuthorized = await _fixture.InvoiceClient.GetLastAuthorizedNumberAsync(series);
        var nextVoucherNumber = (lastAuthorized ?? 0) + 1;

        var request = new CreateInvoiceRequest
        {
            IssuerCuit = settings.Cuit,
            VoucherType = series.VoucherType,
            PointOfSale = settings.PointOfSale,
            VoucherNumber = nextVoucherNumber,
            Concept = InvoiceConcept.Products,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Customer = new CustomerIdentity
            {
                Name = "Consumidor Final",
                IsConsumerFinal = true
            },
            ReceiverVatCondition = new ReceiverVatCondition(5, "Consumidor Final"),
            Totals = new MoneyTotals
            {
                TotalAmount = 1210.00m,
                TaxableAmount = 1000.00m,
                VatAmount = 210.00m
            },
            Currency = new CurrencyAmount("PES", 1.00m),
            VatItems =
            [
                new VatItem
                {
                    Id = 5,
                    BaseAmount = 1000.00m,
                    Rate = 21.00m,
                    Amount = 210.00m
                }
            ],
            ExternalIdempotencyKey = $"homologation-factura-b-{settings.PointOfSale}-{nextVoucherNumber}"
        };

        var result = await _fixture.InvoiceClient.CreateInvoiceAsync(request);
        AssertAuthorizedResult(result, settings.Cuit, settings.PointOfSale, nextVoucherNumber);
    }

    [HomologationIssuanceFact]
    public async Task InvoiceClient_CreateInvoiceAsync_AuthorizesNewFacturaA()
    {
        var settings = _fixture.Settings;
        var series = new VoucherSeries(
            settings.Cuit,
            settings.PointOfSale,
            new VoucherType(1, "Factura A"));

        var lastAuthorized = await _fixture.InvoiceClient.GetLastAuthorizedNumberAsync(series);
        var nextVoucherNumber = (lastAuthorized ?? 0) + 1;

        var request = new CreateInvoiceRequest
        {
            IssuerCuit = settings.Cuit,
            VoucherType = series.VoucherType,
            PointOfSale = settings.PointOfSale,
            VoucherNumber = nextVoucherNumber,
            Concept = InvoiceConcept.Products,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Customer = new CustomerIdentity
            {
                Name = "Cliente SA",
                DocumentTypeCode = 80,
                DocumentNumber = "30712345678"
            },
            ReceiverVatCondition = new ReceiverVatCondition(1, "IVA Responsable Inscripto"),
            Totals = new MoneyTotals
            {
                TotalAmount = 1210.00m,
                TaxableAmount = 1000.00m,
                VatAmount = 210.00m
            },
            Currency = new CurrencyAmount("PES", 1.00m),
            VatItems =
            [
                new VatItem
                {
                    Id = 5,
                    BaseAmount = 1000.00m,
                    Rate = 21.00m,
                    Amount = 210.00m
                }
            ],
            ExternalIdempotencyKey = $"homologation-factura-a-{settings.PointOfSale}-{nextVoucherNumber}"
        };

        var result = await _fixture.InvoiceClient.CreateInvoiceAsync(request);
        AssertAuthorizedResult(result, settings.Cuit, settings.PointOfSale, nextVoucherNumber);
    }

    [HomologationIssuanceFact]
    public async Task InvoiceClient_CreateInvoiceAsync_AuthorizesNotaDeCreditoBAssociatedToFacturaB()
    {
        var settings = _fixture.Settings;
        var facturaBSeries = new VoucherSeries(
            settings.Cuit,
            settings.PointOfSale,
            new VoucherType(6, "Factura B"));

        var lastFacturaB = await _fixture.InvoiceClient.GetLastAuthorizedNumberAsync(facturaBSeries);
        var nextFacturaBNumber = (lastFacturaB ?? 0) + 1;

        var facturaBRequest = new CreateInvoiceRequest
        {
            IssuerCuit = settings.Cuit,
            VoucherType = facturaBSeries.VoucherType,
            PointOfSale = settings.PointOfSale,
            VoucherNumber = nextFacturaBNumber,
            Concept = InvoiceConcept.Products,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Customer = new CustomerIdentity
            {
                Name = "Consumidor Final",
                IsConsumerFinal = true
            },
            ReceiverVatCondition = new ReceiverVatCondition(5, "Consumidor Final"),
            Totals = new MoneyTotals
            {
                TotalAmount = 1210.00m,
                TaxableAmount = 1000.00m,
                VatAmount = 210.00m
            },
            Currency = new CurrencyAmount("PES", 1.00m),
            VatItems =
            [
                new VatItem
                {
                    Id = 5,
                    BaseAmount = 1000.00m,
                    Rate = 21.00m,
                    Amount = 210.00m
                }
            ],
            ExternalIdempotencyKey = $"homologation-factura-b-base-{settings.PointOfSale}-{nextFacturaBNumber}"
        };

        var facturaBResult = await _fixture.InvoiceClient.CreateInvoiceAsync(facturaBRequest);
        var authorizedFacturaB = AssertAuthorizedResult(
            facturaBResult,
            settings.Cuit,
            settings.PointOfSale,
            nextFacturaBNumber);

        var notaBSeries = new VoucherSeries(
            settings.Cuit,
            settings.PointOfSale,
            new VoucherType(8, "Nota de Credito B", VoucherKind.CreditNote));

        var lastNotaB = await _fixture.InvoiceClient.GetLastAuthorizedNumberAsync(notaBSeries);
        var nextNotaBNumber = (lastNotaB ?? 0) + 1;

        var notaBRequest = new CreateInvoiceRequest
        {
            IssuerCuit = settings.Cuit,
            VoucherType = notaBSeries.VoucherType,
            PointOfSale = settings.PointOfSale,
            VoucherNumber = nextNotaBNumber,
            Concept = InvoiceConcept.Products,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Customer = new CustomerIdentity
            {
                Name = "Consumidor Final",
                IsConsumerFinal = true
            },
            ReceiverVatCondition = new ReceiverVatCondition(5, "Consumidor Final"),
            Totals = new MoneyTotals
            {
                TotalAmount = 1210.00m,
                TaxableAmount = 1000.00m,
                VatAmount = 210.00m
            },
            Currency = new CurrencyAmount("PES", 1.00m),
            VatItems =
            [
                new VatItem
                {
                    Id = 5,
                    BaseAmount = 1000.00m,
                    Rate = 21.00m,
                    Amount = 210.00m
                }
            ],
            AssociatedVouchers =
            [
                new AssociatedVoucher
                {
                    VoucherType = facturaBSeries.VoucherType,
                    PointOfSale = settings.PointOfSale,
                    VoucherNumber = authorizedFacturaB.Invoice.VoucherNumber,
                    IssuerCuit = settings.Cuit,
                    IssuedOn = authorizedFacturaB.Invoice.IssueDate
                }
            ],
            ExternalIdempotencyKey = $"homologation-nota-credito-b-{settings.PointOfSale}-{nextNotaBNumber}"
        };

        var notaBResult = await _fixture.InvoiceClient.CreateInvoiceAsync(notaBRequest);
        var authorizedNotaB = AssertAuthorizedResult(
            notaBResult,
            settings.Cuit,
            settings.PointOfSale,
            nextNotaBNumber);

        Assert.Single(authorizedNotaB.Invoice.AssociatedVouchers);
        var associated = authorizedNotaB.Invoice.AssociatedVouchers[0];
        Assert.Equal(facturaBSeries.VoucherType.Code, associated.VoucherType.Code);
        Assert.Equal(settings.PointOfSale, associated.PointOfSale);
        Assert.Equal(authorizedFacturaB.Invoice.VoucherNumber, associated.VoucherNumber);
    }

    [HomologationIssuanceFact]
    public async Task InvoiceClient_CreateInvoiceAsync_AuthorizesNotaDeCreditoAAssociatedToFacturaA()
    {
        var settings = _fixture.Settings;
        var facturaASeries = new VoucherSeries(
            settings.Cuit,
            settings.PointOfSale,
            new VoucherType(1, "Factura A"));

        var lastFacturaA = await _fixture.InvoiceClient.GetLastAuthorizedNumberAsync(facturaASeries);
        var nextFacturaANumber = (lastFacturaA ?? 0) + 1;

        var facturaARequest = new CreateInvoiceRequest
        {
            IssuerCuit = settings.Cuit,
            VoucherType = facturaASeries.VoucherType,
            PointOfSale = settings.PointOfSale,
            VoucherNumber = nextFacturaANumber,
            Concept = InvoiceConcept.Products,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Customer = new CustomerIdentity
            {
                Name = "Cliente SA",
                DocumentTypeCode = 80,
                DocumentNumber = "30712345678"
            },
            ReceiverVatCondition = new ReceiverVatCondition(1, "IVA Responsable Inscripto"),
            Totals = new MoneyTotals
            {
                TotalAmount = 1210.00m,
                TaxableAmount = 1000.00m,
                VatAmount = 210.00m
            },
            Currency = new CurrencyAmount("PES", 1.00m),
            VatItems =
            [
                new VatItem
                {
                    Id = 5,
                    BaseAmount = 1000.00m,
                    Rate = 21.00m,
                    Amount = 210.00m
                }
            ],
            ExternalIdempotencyKey = $"homologation-factura-a-base-{settings.PointOfSale}-{nextFacturaANumber}"
        };

        var facturaAResult = await _fixture.InvoiceClient.CreateInvoiceAsync(facturaARequest);
        var authorizedFacturaA = AssertAuthorizedResult(
            facturaAResult,
            settings.Cuit,
            settings.PointOfSale,
            nextFacturaANumber);

        var notaASeries = new VoucherSeries(
            settings.Cuit,
            settings.PointOfSale,
            new VoucherType(3, "Nota de Credito A", VoucherKind.CreditNote));

        var lastNotaA = await _fixture.InvoiceClient.GetLastAuthorizedNumberAsync(notaASeries);
        var nextNotaANumber = (lastNotaA ?? 0) + 1;

        var notaARequest = new CreateInvoiceRequest
        {
            IssuerCuit = settings.Cuit,
            VoucherType = notaASeries.VoucherType,
            PointOfSale = settings.PointOfSale,
            VoucherNumber = nextNotaANumber,
            Concept = InvoiceConcept.Products,
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            Customer = new CustomerIdentity
            {
                Name = "Cliente SA",
                DocumentTypeCode = 80,
                DocumentNumber = "30712345678"
            },
            ReceiverVatCondition = new ReceiverVatCondition(1, "IVA Responsable Inscripto"),
            Totals = new MoneyTotals
            {
                TotalAmount = 1210.00m,
                TaxableAmount = 1000.00m,
                VatAmount = 210.00m
            },
            Currency = new CurrencyAmount("PES", 1.00m),
            VatItems =
            [
                new VatItem
                {
                    Id = 5,
                    BaseAmount = 1000.00m,
                    Rate = 21.00m,
                    Amount = 210.00m
                }
            ],
            AssociatedVouchers =
            [
                new AssociatedVoucher
                {
                    VoucherType = facturaASeries.VoucherType,
                    PointOfSale = settings.PointOfSale,
                    VoucherNumber = authorizedFacturaA.Invoice.VoucherNumber,
                    IssuerCuit = settings.Cuit,
                    IssuedOn = authorizedFacturaA.Invoice.IssueDate
                }
            ],
            ExternalIdempotencyKey = $"homologation-nota-credito-a-{settings.PointOfSale}-{nextNotaANumber}"
        };

        var notaAResult = await _fixture.InvoiceClient.CreateInvoiceAsync(notaARequest);
        var authorizedNotaA = AssertAuthorizedResult(
            notaAResult,
            settings.Cuit,
            settings.PointOfSale,
            nextNotaANumber);

        Assert.Single(authorizedNotaA.Invoice.AssociatedVouchers);
        var associated = authorizedNotaA.Invoice.AssociatedVouchers[0];
        Assert.Equal(facturaASeries.VoucherType.Code, associated.VoucherType.Code);
        Assert.Equal(settings.PointOfSale, associated.PointOfSale);
        Assert.Equal(authorizedFacturaA.Invoice.VoucherNumber, associated.VoucherNumber);
    }

    private AuthorizedInvoiceResult AssertAuthorizedResult(
        CreateInvoiceResult result,
        long expectedIssuerCuit,
        int expectedPointOfSale,
        long expectedVoucherNumber)
    {
        if (result is UnknownInvoiceResult unknown)
        {
            var transport = _fixture.Transport;
            var lastAction = transport.LastRequest?.SoapAction ?? "(none)";
            var lastResponse = transport.LastResponseBody ?? "(none)";
            var lastException = transport.LastException?.ToString() ?? "(none)";

            throw new Xunit.Sdk.XunitException(
                $"Homologation issuance returned UnknownInvoiceResult: {unknown.Reason}{Environment.NewLine}" +
                $"Last SOAP action: {lastAction}{Environment.NewLine}" +
                $"Last transport exception: {lastException}{Environment.NewLine}" +
                $"Last SOAP response: {lastResponse}");
        }

        if (result is RejectedInvoiceResult rejected)
        {
            var transport = _fixture.Transport;
            var lastAction = transport.LastRequest?.SoapAction ?? "(none)";
            var lastResponse = transport.LastResponseBody ?? "(none)";
            var rejectionText = rejected.Rejections.Count == 0
                ? "(none)"
                : string.Join(
                    Environment.NewLine,
                    rejected.Rejections.Select(x => $"{x.Code}: {x.Message}"));
            var observationText = rejected.Observations.Count == 0
                ? "(none)"
                : string.Join(
                    Environment.NewLine,
                    rejected.Observations.Select(x => $"{x.Code}: {x.Message}"));

            throw new Xunit.Sdk.XunitException(
                $"Homologation issuance returned RejectedInvoiceResult.{Environment.NewLine}" +
                $"Last SOAP action: {lastAction}{Environment.NewLine}" +
                $"Rejections:{Environment.NewLine}{rejectionText}{Environment.NewLine}" +
                $"Observations:{Environment.NewLine}{observationText}{Environment.NewLine}" +
                $"Last SOAP response: {lastResponse}");
        }

        var authorized = Assert.IsType<AuthorizedInvoiceResult>(result);
        Assert.Equal(expectedIssuerCuit, authorized.Invoice.IssuerCuit);
        Assert.Equal(expectedPointOfSale, authorized.Invoice.Series.PointOfSale);
        Assert.Equal(expectedVoucherNumber, authorized.Invoice.VoucherNumber);
        Assert.Equal(AuthorizationCodeType.Cae, authorized.Invoice.AuthorizationCodeType);
        Assert.False(string.IsNullOrWhiteSpace(authorized.Invoice.AuthorizationCode));
        Assert.True(authorized.Invoice.AuthorizationDueDate > DateOnly.MinValue);
        Assert.NotNull(authorized.Invoice.QrUrl);
        return authorized;
    }
}
