using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Models;

namespace Orders.Api.Data
{
    public class OrdersApiContext : DbContext
    {
        public OrdersApiContext (DbContextOptions<OrdersApiContext> options)
            : base(options)
        {
        }

        public DbSet<Orders.Api.Models.Order> Order { get; set; } = default!;
    }
}
