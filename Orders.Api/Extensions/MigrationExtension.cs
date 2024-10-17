using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Extensions
{
    public static class MigrationExtension
    {
        public static void ApplyMigrations(this IApplicationBuilder app)
        {
            using IServiceScope scope = app.ApplicationServices.CreateScope();
            using OrdersApiContext dbContext =
                scope.ServiceProvider.GetRequiredService<OrdersApiContext>();

            dbContext.Database.Migrate();
        }
    }
}
