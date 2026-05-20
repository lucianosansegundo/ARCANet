using ARCANet.Abstractions;
using ARCANet.InternalInvoices;
using ARCANet.Qr;
using ARCANet.Transport;
using ARCANet.Wsfev1;

namespace ARCANet.Invoices;

public sealed class InvoiceClient : IInvoiceClient
{
    private readonly IInvoiceRequestValidator _validator;
    private readonly IInvoiceSubmissionMapper _mapper;
    private readonly IArcaQrGenerator _qrGenerator;
    private readonly Wsfev1Client _wsfev1Client;

    public InvoiceClient(
        IInvoiceRequestValidator validator,
        IArcaQrGenerator qrGenerator,
        IAccessTicketProvider accessTicketProvider,
        IArcaSoapTransport transport,
        Wsfev1Options? options = null)
        : this(validator, new InvoiceSubmissionMapper(), qrGenerator, new Wsfev1Client(accessTicketProvider, transport, options ?? new Wsfev1Options()))
    {
    }

    internal InvoiceClient(
        IInvoiceRequestValidator validator,
        IInvoiceSubmissionMapper mapper,
        IArcaQrGenerator qrGenerator,
        Wsfev1Client wsfev1Client)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _qrGenerator = qrGenerator ?? throw new ArgumentNullException(nameof(qrGenerator));
        _wsfev1Client = wsfev1Client ?? throw new ArgumentNullException(nameof(wsfev1Client));
    }

    public Task<InvoiceValidationResult> ValidateCreateInvoiceAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_validator.Validate(request));
    }

    public async Task<CreateInvoiceResult> CreateInvoiceAsync(
        CreateInvoiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = _validator.Validate(request);
        if (!validation.IsValid)
        {
            return new RejectedInvoiceResult(
                BuildAttempt(request),
                validation.Errors.Select(x => new InvoiceRejection(x.Code, x.Message)).ToArray(),
                []);
        }

        var submission = _mapper.Map(request);

        try
        {
            var response = await _wsfev1Client.AuthorizeAsync(submission, cancellationToken).ConfigureAwait(false);
            var attempt = BuildAttempt(request);

            if (response.DetailResult.Equals("A", StringComparison.OrdinalIgnoreCase))
            {
                var invoice = BuildAuthorizedInvoice(request, response);
                return new AuthorizedInvoiceResult(
                    invoice,
                    response.Observations.Select(MapObservation)
                        .Concat(response.Events.Select(MapObservation))
                        .ToArray());
            }

            return new RejectedInvoiceResult(
                attempt,
                response.Errors.Select(MapRejection).ToArray(),
                response.Observations.Select(MapObservation)
                    .Concat(response.Events.Select(MapObservation))
                    .ToArray());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            return new UnknownInvoiceResult(
                BuildAttempt(request),
                $"Invoice submission timed out or was canceled before confirmation. Query before retrying. Technical detail: {BuildTechnicalErrorMessage(exception)}",
                ShouldQueryBeforeRetry: true);
        }
        catch (ArcaSoapTransportException exception)
        {
            return new UnknownInvoiceResult(
                BuildAttempt(request),
                exception.Message,
                ShouldQueryBeforeRetry: true);
        }
        catch (Exception exception)
        {
            return new UnknownInvoiceResult(
                BuildAttempt(request),
                $"Invoice submission could not be confirmed. Query before retrying. Technical detail: {BuildTechnicalErrorMessage(exception)}",
                ShouldQueryBeforeRetry: true);
        }
    }

    public async Task<AuthorizedInvoice?> GetInvoiceAsync(
        InvoiceLocator locator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(locator);

        var record = await _wsfev1Client.GetInvoiceAsync(locator, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var customer = new CustomerIdentity
        {
            Name = "Pending lookup",
            DocumentTypeCode = record.CustomerDocumentTypeCode,
            DocumentNumber = record.CustomerDocumentNumber,
            IsConsumerFinal = record.CustomerDocumentTypeCode == 99
        };

        var invoice = new AuthorizedInvoice
        {
            IssuerCuit = record.IssuerCuit,
            Series = new VoucherSeries(record.IssuerCuit, record.PointOfSale, locator.Series.VoucherType),
            VoucherNumber = record.VoucherNumber,
            IssueDate = record.IssueDate,
            Concept = record.Concept,
            ServiceFrom = record.ServiceFrom,
            ServiceTo = record.ServiceTo,
            PaymentDueDate = record.PaymentDueDate,
            Customer = customer,
            ReceiverVatCondition = new ReceiverVatCondition(0, "Pending lookup"),
            Totals = record.Totals,
            Currency = record.Currency,
            VatItems = record.VatItems,
            Tributes = record.Tributes,
            AssociatedVouchers = record.AssociatedVouchers,
            AuthorizationCodeType = record.EmissionType.Equals("CAEA", StringComparison.OrdinalIgnoreCase)
                ? AuthorizationCodeType.Caea
                : AuthorizationCodeType.Cae,
            AuthorizationCode = record.AuthorizationCode,
            AuthorizationDueDate = record.AuthorizationDueDate,
            ProcessedAtUtc = record.ProcessedAtUtc
        };

        var payload = _qrGenerator.BuildPayload(invoice);
        return invoice with
        {
            QrPayload = payload,
            QrUrl = _qrGenerator.BuildUrl(payload)
        };
    }

    public Task<long?> GetLastAuthorizedNumberAsync(
        VoucherSeries series,
        CancellationToken cancellationToken = default) =>
        _wsfev1Client.GetLastAuthorizedNumberAsync(series, cancellationToken);

    private AuthorizedInvoice BuildAuthorizedInvoice(CreateInvoiceRequest request, WsfeAuthorizationResponse response)
    {
        var invoice = new AuthorizedInvoice
        {
            IssuerCuit = request.IssuerCuit,
            Series = new VoucherSeries(request.IssuerCuit, request.PointOfSale, request.VoucherType),
            VoucherNumber = request.VoucherNumber,
            IssueDate = request.IssueDate,
            Concept = request.Concept,
            ServiceFrom = request.ServiceFrom,
            ServiceTo = request.ServiceTo,
            PaymentDueDate = request.PaymentDueDate,
            Customer = request.Customer,
            ReceiverVatCondition = request.ReceiverVatCondition,
            Totals = request.Totals,
            Currency = request.Currency,
            VatItems = request.VatItems,
            Tributes = request.Tributes,
            AssociatedVouchers = request.AssociatedVouchers,
            AuthorizationCodeType = AuthorizationCodeType.Cae,
            AuthorizationCode = response.AuthorizationCode ?? throw new InvalidOperationException("Approved response did not include CAE."),
            AuthorizationDueDate = response.AuthorizationDueDate ?? throw new InvalidOperationException("Approved response did not include CAE due date."),
            ProcessedAtUtc = response.ProcessedAtUtc
        };

        var qrPayload = _qrGenerator.BuildPayload(invoice);
        return invoice with
        {
            QrPayload = qrPayload,
            QrUrl = _qrGenerator.BuildUrl(qrPayload)
        };
    }

    private static InvoiceAttempt BuildAttempt(CreateInvoiceRequest request) =>
        new()
        {
            IssuerCuit = request.IssuerCuit,
            Series = new VoucherSeries(request.IssuerCuit, request.PointOfSale, request.VoucherType),
            VoucherNumber = request.VoucherNumber,
            IssueDate = request.IssueDate,
            Customer = request.Customer,
            ReceiverVatCondition = request.ReceiverVatCondition,
            Totals = request.Totals,
            Currency = request.Currency,
            ExternalIdempotencyKey = request.ExternalIdempotencyKey
        };

    private static InvoiceObservation MapObservation(WsfeResultIssue issue) => new(issue.Code, issue.Message);

    private static InvoiceRejection MapRejection(WsfeResultIssue issue) => new(issue.Code, issue.Message);

    private static string BuildTechnicalErrorMessage(Exception exception) =>
        $"{exception.GetType().Name}: {exception.Message}";
}
