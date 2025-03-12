using System;

namespace RefactorThis.Domain.Exceptions
{

    public class InvoiceException : Exception
    {
        public InvoiceException(string message) : base(message) { }
    }
}
