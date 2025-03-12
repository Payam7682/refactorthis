using RefactorThis.Application.Interfaces;
using RefactorThis.Domain.Entities;




namespace RefactorThis.Persistence.Repositories
{

    public class InvoiceRepository : IInvoiceRepository
    {
        private Invoice? _invoice;

        public Invoice? GetInvoice(string reference) => _invoice;

        public void SaveInvoice(Invoice invoice)
        {
            // Implementation for saving to database
        }

        public void Add(Invoice invoice)
        {
            _invoice = invoice;
        }
    }

}