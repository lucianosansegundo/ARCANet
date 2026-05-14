using System.Globalization;
using ARCANet.Abstractions;

namespace ARCANet.Invoices;

public sealed class InvoiceRequestValidator : IInvoiceRequestValidator
{
    private readonly IClock _clock;
    private readonly InvoiceValidationOptions _options;

    public InvoiceRequestValidator(IClock clock)
        : this(clock, new InvoiceValidationOptions())
    {
    }

    public InvoiceRequestValidator(IClock clock, InvoiceValidationOptions options)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public InvoiceValidationResult Validate(CreateInvoiceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<InvoiceValidationError>();

        ValidateRequiredFields(request, errors);
        ValidateIdentifiers(request, errors);
        ValidateDates(request, errors);
        ValidateCurrency(request, errors);
        ValidateTotals(request, errors);
        ValidateAssociatedVouchers(request, errors);

        return errors.Count == 0
            ? InvoiceValidationResult.Success
            : new InvoiceValidationResult { Errors = errors };
    }

    private static void ValidateRequiredFields(CreateInvoiceRequest request, List<InvoiceValidationError> errors)
    {
        if (request.VoucherType is null)
        {
            errors.Add(Error("voucher_type_required", "Voucher type is required.", nameof(request.VoucherType)));
        }

        if (request.Customer is null)
        {
            errors.Add(Error("customer_required", "Customer is required.", nameof(request.Customer)));
        }

        if (request.ReceiverVatCondition is null)
        {
            errors.Add(Error("receiver_vat_condition_required", "Receiver VAT condition is required.", nameof(request.ReceiverVatCondition)));
        }

        if (request.Totals is null)
        {
            errors.Add(Error("totals_required", "Totals are required.", nameof(request.Totals)));
        }

        if (request.Currency is null)
        {
            errors.Add(Error("currency_required", "Currency is required.", nameof(request.Currency)));
        }

        if (request.PointOfSale <= 0)
        {
            errors.Add(Error("invalid_point_of_sale", "Point of sale must be greater than zero.", nameof(request.PointOfSale)));
        }

        if (request.VoucherNumber <= 0)
        {
            errors.Add(Error("invalid_voucher_number", "Voucher number must be greater than zero.", nameof(request.VoucherNumber)));
        }

        if (request.IssueDate == default)
        {
            errors.Add(Error("issue_date_required", "Issue date is required.", nameof(request.IssueDate)));
        }

        if (request.Customer is not null && string.IsNullOrWhiteSpace(request.Customer.Name))
        {
            errors.Add(Error("customer_name_required", "Customer name is required.", $"{nameof(request.Customer)}.{nameof(CustomerIdentity.Name)}"));
        }

        if (request.VoucherType is not null)
        {
            if (request.VoucherType.Code <= 0)
            {
                errors.Add(Error("invalid_voucher_type_code", "Voucher type code must be greater than zero.", $"{nameof(request.VoucherType)}.{nameof(VoucherType.Code)}"));
            }

            if (string.IsNullOrWhiteSpace(request.VoucherType.Name))
            {
                errors.Add(Error("voucher_type_name_required", "Voucher type name is required.", $"{nameof(request.VoucherType)}.{nameof(VoucherType.Name)}"));
            }
        }

        if (request.ReceiverVatCondition is not null)
        {
            if (request.ReceiverVatCondition.Id <= 0)
            {
                errors.Add(Error("invalid_receiver_vat_condition", "Receiver VAT condition id must be greater than zero.", $"{nameof(request.ReceiverVatCondition)}.{nameof(ReceiverVatCondition.Id)}"));
            }

            if (string.IsNullOrWhiteSpace(request.ReceiverVatCondition.Name))
            {
                errors.Add(Error("receiver_vat_condition_name_required", "Receiver VAT condition name is required.", $"{nameof(request.ReceiverVatCondition)}.{nameof(ReceiverVatCondition.Name)}"));
            }
        }
    }

    private static void ValidateIdentifiers(CreateInvoiceRequest request, List<InvoiceValidationError> errors)
    {
        if (!IsValidCuit(request.IssuerCuit))
        {
            errors.Add(Error("invalid_issuer_cuit", "Issuer CUIT must contain exactly 11 digits.", nameof(request.IssuerCuit)));
        }

        if (request.Customer is not null && !string.IsNullOrWhiteSpace(request.Customer.DocumentNumber))
        {
            if (!IsDigitsOnly(request.Customer.DocumentNumber))
            {
                errors.Add(Error("invalid_customer_document", "Customer document number must contain only digits.", $"{nameof(request.Customer)}.{nameof(CustomerIdentity.DocumentNumber)}"));
            }
        }
        else if (request.Customer?.DocumentTypeCode is not null)
        {
            errors.Add(Error("customer_document_number_required", "Customer document number is required when document type is present.", $"{nameof(request.Customer)}.{nameof(CustomerIdentity.DocumentNumber)}"));
        }
    }

    private void ValidateDates(CreateInvoiceRequest request, List<InvoiceValidationError> errors)
    {
        if (request.IssueDate == default)
        {
            return;
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);

        if (request.IssueDate < today.AddDays(-_options.MaxIssueDatePastDays))
        {
            errors.Add(Error("issue_date_too_old", "Issue date is outside the configured past tolerance window.", nameof(request.IssueDate)));
        }

        if (request.IssueDate > today.AddDays(_options.MaxIssueDateFutureDays))
        {
            errors.Add(Error("issue_date_too_far_in_future", "Issue date is outside the configured future tolerance window.", nameof(request.IssueDate)));
        }

        var requiresServiceDates = request.Concept is InvoiceConcept.Services or InvoiceConcept.ProductsAndServices;

        if (requiresServiceDates)
        {
            if (request.ServiceFrom is null)
            {
                errors.Add(Error("service_from_required", "Service start date is required for service concepts.", nameof(request.ServiceFrom)));
            }

            if (request.ServiceTo is null)
            {
                errors.Add(Error("service_to_required", "Service end date is required for service concepts.", nameof(request.ServiceTo)));
            }

            if (_options.RequirePaymentDueDateForServiceConcepts && request.PaymentDueDate is null)
            {
                errors.Add(Error("payment_due_date_required", "Payment due date is required for service concepts by current validation policy.", nameof(request.PaymentDueDate)));
            }
        }

        if (request.ServiceFrom is not null && request.ServiceTo is not null && request.ServiceFrom > request.ServiceTo)
        {
            errors.Add(Error("invalid_service_date_range", "Service start date must be less than or equal to service end date.", nameof(request.ServiceFrom)));
        }

        if (request.PaymentDueDate is not null && request.PaymentDueDate < request.IssueDate)
        {
            errors.Add(Error("invalid_payment_due_date", "Payment due date must be greater than or equal to issue date.", nameof(request.PaymentDueDate)));
        }
    }

    private static void ValidateCurrency(CreateInvoiceRequest request, List<InvoiceValidationError> errors)
    {
        if (request.Currency is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Currency.Code))
        {
            errors.Add(Error("currency_code_required", "Currency code is required.", $"{nameof(request.Currency)}.{nameof(CurrencyAmount.Code)}"));
        }

        if (request.Currency.ExchangeRate <= 0)
        {
            errors.Add(Error("invalid_currency_exchange_rate", "Currency exchange rate must be greater than zero.", $"{nameof(request.Currency)}.{nameof(CurrencyAmount.ExchangeRate)}"));
        }
    }

    private static void ValidateTotals(CreateInvoiceRequest request, List<InvoiceValidationError> errors)
    {
        if (request.Totals is null)
        {
            return;
        }

        ValidateNonNegative(request.Totals.TotalAmount, nameof(MoneyTotals.TotalAmount), errors);
        ValidateNonNegative(request.Totals.TaxableAmount, nameof(MoneyTotals.TaxableAmount), errors);
        ValidateNonNegative(request.Totals.NonTaxedAmount, nameof(MoneyTotals.NonTaxedAmount), errors);
        ValidateNonNegative(request.Totals.ExemptAmount, nameof(MoneyTotals.ExemptAmount), errors);
        ValidateNonNegative(request.Totals.VatAmount, nameof(MoneyTotals.VatAmount), errors);
        ValidateNonNegative(request.Totals.OtherTaxesAmount, nameof(MoneyTotals.OtherTaxesAmount), errors);

        var expectedTotal =
            request.Totals.NonTaxedAmount +
            request.Totals.TaxableAmount +
            request.Totals.ExemptAmount +
            request.Totals.OtherTaxesAmount +
            request.Totals.VatAmount;

        if (expectedTotal != request.Totals.TotalAmount)
        {
            errors.Add(Error("inconsistent_total_amount", "Total amount must equal non-taxed + taxable + exempt + other taxes + VAT.", nameof(request.Totals.TotalAmount)));
        }

        foreach (var vatItem in request.VatItems)
        {
            ValidateNonNegative(vatItem.BaseAmount, $"{nameof(CreateInvoiceRequest.VatItems)}.{nameof(VatItem.BaseAmount)}", errors);
            ValidateNonNegative(vatItem.Rate, $"{nameof(CreateInvoiceRequest.VatItems)}.{nameof(VatItem.Rate)}", errors);
            ValidateNonNegative(vatItem.Amount, $"{nameof(CreateInvoiceRequest.VatItems)}.{nameof(VatItem.Amount)}", errors);

            if (vatItem.Id <= 0)
            {
                errors.Add(Error("invalid_vat_item_id", "VAT item id must be greater than zero.", $"{nameof(CreateInvoiceRequest.VatItems)}.{nameof(VatItem.Id)}"));
            }
        }

        foreach (var tribute in request.Tributes)
        {
            ValidateNonNegative(tribute.BaseAmount, $"{nameof(CreateInvoiceRequest.Tributes)}.{nameof(TributeItem.BaseAmount)}", errors);
            ValidateNonNegative(tribute.Rate, $"{nameof(CreateInvoiceRequest.Tributes)}.{nameof(TributeItem.Rate)}", errors);
            ValidateNonNegative(tribute.Amount, $"{nameof(CreateInvoiceRequest.Tributes)}.{nameof(TributeItem.Amount)}", errors);

            if (tribute.Id <= 0)
            {
                errors.Add(Error("invalid_tribute_id", "Tribute id must be greater than zero.", $"{nameof(CreateInvoiceRequest.Tributes)}.{nameof(TributeItem.Id)}"));
            }
        }

        var expectedVatAmount = request.VatItems.Sum(x => x.Amount);
        if (expectedVatAmount != request.Totals.VatAmount)
        {
            errors.Add(Error("inconsistent_vat_amount", "VAT amount must equal the sum of VAT breakdown lines.", nameof(request.Totals.VatAmount)));
        }

        var expectedTributesAmount = request.Tributes.Sum(x => x.Amount);
        if (expectedTributesAmount != request.Totals.OtherTaxesAmount)
        {
            errors.Add(Error("inconsistent_tribute_amount", "Other taxes amount must equal the sum of tribute lines.", nameof(request.Totals.OtherTaxesAmount)));
        }
    }

    private static void ValidateAssociatedVouchers(CreateInvoiceRequest request, List<InvoiceValidationError> errors)
    {
        if (request.VoucherType is not null &&
            request.VoucherType.Kind is VoucherKind.CreditNote or VoucherKind.DebitNote &&
            request.AssociatedVouchers.Count == 0)
        {
            errors.Add(Error("associated_voucher_required", "Credit and debit notes must include at least one associated voucher.", nameof(request.AssociatedVouchers)));
        }

        for (var i = 0; i < request.AssociatedVouchers.Count; i++)
        {
            var associated = request.AssociatedVouchers[i];
            var prefix = $"{nameof(request.AssociatedVouchers)}[{i}]";

            if (associated.VoucherType is null)
            {
                errors.Add(Error("associated_voucher_type_required", "Associated voucher type is required.", $"{prefix}.{nameof(AssociatedVoucher.VoucherType)}"));
            }
            else
            {
                if (associated.VoucherType.Code <= 0)
                {
                    errors.Add(Error("invalid_associated_voucher_type_code", "Associated voucher type code must be greater than zero.", $"{prefix}.{nameof(AssociatedVoucher.VoucherType)}.{nameof(VoucherType.Code)}"));
                }

                if (string.IsNullOrWhiteSpace(associated.VoucherType.Name))
                {
                    errors.Add(Error("associated_voucher_type_name_required", "Associated voucher type name is required.", $"{prefix}.{nameof(AssociatedVoucher.VoucherType)}.{nameof(VoucherType.Name)}"));
                }
            }

            if (associated.PointOfSale <= 0)
            {
                errors.Add(Error("invalid_associated_point_of_sale", "Associated voucher point of sale must be greater than zero.", $"{prefix}.{nameof(AssociatedVoucher.PointOfSale)}"));
            }

            if (associated.VoucherNumber <= 0)
            {
                errors.Add(Error("invalid_associated_voucher_number", "Associated voucher number must be greater than zero.", $"{prefix}.{nameof(AssociatedVoucher.VoucherNumber)}"));
            }

            if (associated.IssuerCuit is not null && !IsValidCuit(associated.IssuerCuit.Value))
            {
                errors.Add(Error("invalid_associated_issuer_cuit", "Associated voucher issuer CUIT must contain exactly 11 digits.", $"{prefix}.{nameof(AssociatedVoucher.IssuerCuit)}"));
            }
        }
    }

    private static void ValidateNonNegative(decimal value, string field, List<InvoiceValidationError> errors)
    {
        if (value < 0)
        {
            errors.Add(Error("negative_amount_not_allowed", "Monetary values cannot be negative in the current neutral submission model.", field));
        }
    }

    private static bool IsValidCuit(long cuit) => cuit is >= 10000000000 and <= 99999999999;

    private static bool IsDigitsOnly(string value) => value.All(char.IsDigit);

    private static InvoiceValidationError Error(string code, string message, string field) =>
        new(code, message, field);
}
