using Microsoft.Extensions.DependencyInjection;
using RefactorThis.Application.Interfaces;
using RefactorThis.Persistence.Repositories;

namespace RefactorThis.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services)
        {
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            return services;
        }
    }
}
