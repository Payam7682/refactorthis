using RefactorThis.Application.Interfaces;
using RefactorThis.Domain.Entities;
using RefactorThis.Domain.Exceptions;
using System.Linq;
using System;

namespace RefactorThis.Application.Services
{
    public class InvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public InvoiceService(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public string ProcessPayment(Payment payment)
        {
            var inv = _invoiceRepository.GetInvoice(payment.Reference)
                      ?? throw new InvoiceException("There is no invoice matching this payment");

            // Switch expression on the invoice's Amount to decide the path
            return inv switch
            {
                { Amount: 0 } => HandleZeroAmountInvoice(inv),
                _ => HandleNormalInvoice(inv, payment)
            };
        }

        private string HandleZeroAmountInvoice(Invoice inv)
        {
            // If there are no payments, "no payment needed",
            // else throw an exception as per Snippet A’s “invalid state”
            return inv.Payments.Count switch
            {
                0 => "no payment needed",
                _ => throw new InvoiceException("The invoice is in an invalid state, it has an amount of 0 but has payments")
            };
        }

        private string HandleNormalInvoice(Invoice inv, Payment payment)
        {
            // In Snippet A, logic differs if the invoice already has payments
            // or if this is the first incoming payment.
            return inv.Payments.Any() switch
            {
                true => HandleInvoiceWithExistingPayments(inv, payment),
                false => HandleInvoiceWithNoExistingPayments(inv, payment)
            };
        }

        private string HandleInvoiceWithExistingPayments(Invoice inv, Payment payment)
        {
            // Sum of existing payments
            var sumSoFar = inv.Payments.Sum(x => x.Amount);

            // Some convenience checks from Snippet A:
            //  - Already fully paid if sumSoFar == inv.Amount.
            //  - Overpayment if payment is greater than what's left.
            //  - Exactly final partial if new payment == remaining
            //  - Otherwise, just another partial payment.
            var alreadyFull = sumSoFar == inv.Amount && sumSoFar != 0;
            var remaining = inv.Amount - inv.AmountPaid; // or (inv.Amount - sumSoFar)
            var overPay = payment.Amount > remaining;
            var finalPartial = payment.Amount == remaining;

            return (alreadyFull, overPay, finalPartial) switch
            {
                (true, _, _) => "invoice was already fully paid",
                (false, true, _) => "the payment is greater than the partial amount remaining",
                (false, false, true) => FinalPartialPayment(inv, payment),
                (false, false, false) => AnotherPartialPayment(inv, payment)
            };
        }

        private string HandleInvoiceWithNoExistingPayments(Invoice inv, Payment payment)
        {
            // From Snippet A:
            //  - If payment > invoice amount => "the payment is greater than the invoice amount"
            //  - If payment == invoice amount => "invoice is now fully paid"
            //  - Else => "invoice is now partially paid"
            return (payment.Amount > inv.Amount, payment.Amount == inv.Amount) switch
            {
                (true, _) => "the payment is greater than the invoice amount",
                (false, true) => FirstFullPayment(inv, payment),
                (false, false) => FirstPartialPayment(inv, payment)
            };
        }

        // ---------------------
        //  First Payment logic
        // ---------------------

        private string FirstFullPayment(Invoice inv, Payment payment)
        {
            // In Snippet A, if there's no existing payments and the payment
            // covers the full amount for Standard or Commercial,
            // we set AmountPaid and also set the TaxAmount to 14% (on both).
            inv.AmountPaid = payment.Amount;
            inv.TaxAmount = payment.Amount * 0.14m; // A applies 14% to both Standard/Commercial
            inv.Payments.Add(payment);

            _invoiceRepository.SaveInvoice(inv);
            return "invoice is now fully paid";
        }

        private string FirstPartialPayment(Invoice inv, Payment payment)
        {
            // In Snippet A, if there's no existing payments
            // and the invoice is partially paid for either Standard or Commercial,
            // we also set the TaxAmount = 14% * payment.
            inv.AmountPaid = payment.Amount;
            inv.TaxAmount = payment.Amount * 0.14m; // Snippet A: same for Standard + Commercial
            inv.Payments.Add(payment);

            _invoiceRepository.SaveInvoice(inv);
            return "invoice is now partially paid";
        }

        // ---------------------------
        //  Subsequent Payment logic
        // ---------------------------

        private string AnotherPartialPayment(Invoice inv, Payment payment)
        {
            // Snippet A says:
            //   - Standard: do not add tax for subsequent partial payments
            //   - Commercial: always add partial tax
            // We track them separately in a switch expression.
            return inv.Type switch
            {
                InvoiceType.Standard => AddStandardPartial(inv, payment, "another partial payment received, still not fully paid"),
                InvoiceType.Commercial => AddCommercialPartial(inv, payment, "another partial payment received, still not fully paid"),
                _ => throw new InvoiceException($"Unknown invoice type: {inv.Type}")
            };
        }

        private string FinalPartialPayment(Invoice inv, Payment payment)
        {
            // Snippet A:
            //   - Standard final partial => no extra tax
            //   - Commercial final partial => tax on this final partial
            return inv.Type switch
            {
                InvoiceType.Standard => AddStandardPartial(inv, payment,
                    "final partial payment received, invoice is now fully paid"),

                InvoiceType.Commercial => AddCommercialPartial(inv, payment,
                    "final partial payment received, invoice is now fully paid"),

                _ => throw new InvoiceException($"Unknown invoice type: {inv.Type}")
            };
        }

        // Helpers for partial payments:

        private string AddStandardPartial(Invoice inv, Payment payment, string message)
        {
            inv.AmountPaid += payment.Amount;
            // Snippet A does NOT add tax for standard partial
            inv.Payments.Add(payment);

            _invoiceRepository.SaveInvoice(inv);
            return message;
        }

        private string AddCommercialPartial(Invoice inv, Payment payment, string message)
        {
            inv.AmountPaid += payment.Amount;
            inv.TaxAmount += payment.Amount * 0.14m;
            inv.Payments.Add(payment);

            _invoiceRepository.SaveInvoice(inv);
            return message;
        }
    }
}
