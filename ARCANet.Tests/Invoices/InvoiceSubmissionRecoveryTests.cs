using ARCANet.Abstractions;
using ARCANet.Invoices;

namespace ARCANet.Tests.Invoices;

public sealed class InvoiceSubmissionRecoveryTests
{
    [Fact]
    public async Task ReconcileAsync_ReturnsAuthorizedResult_WhenInvoiceIsFound()
    {
        var attempt = BuildAttempt();
        var invoice = BuildAuthorizedInvoice(attempt);
        var client = new FakeInvoiceClient(invoice);
        var recovery = new InvoiceSubmissionRecovery(client);

        var result = await recovery.ReconcileAsync(
            new UnknownInvoiceResult(
                attempt,
                "Invoice submission could not be confirmed. Query before retrying.",
                ShouldQueryBeforeRetry: true));

        var authorized = Assert.IsType<AuthorizedInvoiceReconciliationResult>(result);
        Assert.Same(attempt, authorized.Attempt);
        Assert.Same(invoice, authorized.Invoice);
        Assert.NotNull(client.LastLocator);
        Assert.Equal(attempt.Series, client.LastLocator!.Series);
        Assert.Equal(attempt.VoucherNumber, client.LastLocator.VoucherNumber);
    }

    [Fact]
    public async Task ReconcileAsync_ReturnsUnconfirmedResult_WhenInvoiceIsNotFound()
    {
        var attempt = BuildAttempt();
        var client = new FakeInvoiceClient(null);
        var recovery = new InvoiceSubmissionRecovery(client);

        var result = await recovery.ReconcileAsync(attempt);

        var unconfirmed = Assert.IsType<UnconfirmedInvoiceReconciliationResult>(result);
        Assert.Same(attempt, unconfirmed.Attempt);
        Assert.Contains("could not be confirmed", unconfirmed.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReconcileAsync_PropagatesCancellation()
    {
        var attempt = BuildAttempt();
        var client = new FakeInvoiceClient(null, new OperationCanceledException("cancelled"));
        var recovery = new InvoiceSubmissionRecovery(client);

        await Assert.ThrowsAsync<OperationCanceledException>(() => recovery.ReconcileAsync(attempt));
    }

    private static InvoiceAttempt BuildAttempt() =>
        new()
        {
            IssuerCuit = 20304050607,
            Series = new VoucherSeries(20304050607, 5, new VoucherType(6, "Factura B")),
            VoucherNumber = 1234,
            IssueDate = new DateOnly(2026, 5, 16),
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
            ExternalIdempotencyKey = "attempt-1234"
        };

    private static AuthorizedInvoice BuildAuthorizedInvoice(InvoiceAttempt attempt) =>
        new()
        {
            IssuerCuit = attempt.IssuerCuit,
            Series = attempt.Series,
            VoucherNumber = attempt.VoucherNumber,
            IssueDate = attempt.IssueDate,
            Concept = InvoiceConcept.Products,
            Customer = attempt.Customer,
            ReceiverVatCondition = attempt.ReceiverVatCondition,
            Totals = attempt.Totals,
            Currency = attempt.Currency,
            AuthorizationCodeType = AuthorizationCodeType.Cae,
            AuthorizationCode = "70417054367476",
            AuthorizationDueDate = new DateOnly(2026, 5, 26),
            ProcessedAtUtc = new DateTimeOffset(2026, 5, 16, 12, 0, 0, TimeSpan.Zero),
            QrPayload = new ARCANet.Qr.ArcaQrPayload
            {
                Version = 1,
                IssueDate = attempt.IssueDate,
                IssuerCuit = attempt.IssuerCuit,
                PointOfSale = attempt.Series.PointOfSale,
                VoucherTypeCode = attempt.Series.VoucherType.Code,
                VoucherNumber = attempt.VoucherNumber,
                TotalAmount = attempt.Totals.TotalAmount,
                CurrencyCode = attempt.Currency.Code,
                CurrencyExchangeRate = attempt.Currency.ExchangeRate,
                ReceiverDocumentTypeCode = 99,
                ReceiverDocumentNumber = 0,
                AuthorizationCodeType = "E",
                AuthorizationCode = 70417054367476
            },
            QrUrl = new Uri("https://www.arca.gob.ar/fe/qr/?p=test")
        };

    private sealed class FakeInvoiceClient(AuthorizedInvoice? invoice, Exception? getException = null) : IInvoiceClient
    {
        private readonly AuthorizedInvoice? _invoice = invoice;
        private readonly Exception? _getException = getException;

        public InvoiceLocator? LastLocator { get; private set; }

        public Task<CreateInvoiceResult> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthorizedInvoice?> GetInvoiceAsync(InvoiceLocator locator, CancellationToken cancellationToken = default)
        {
            LastLocator = locator;
            return _getException is null
                ? Task.FromResult(_invoice)
                : Task.FromException<AuthorizedInvoice?>(_getException);
        }

        public Task<long?> GetLastAuthorizedNumberAsync(VoucherSeries series, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<InvoiceValidationResult> ValidateCreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
