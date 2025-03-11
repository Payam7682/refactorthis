using RefactorThis.Domain.Entities;

namespace RefactorThis.Application.Interfaces
{
    public interface IInvoiceRepository
    {
        Invoice? GetInvoice(string reference);
        void SaveInvoice(Invoice invoice);
        void Add(Invoice invoice);
    }
}