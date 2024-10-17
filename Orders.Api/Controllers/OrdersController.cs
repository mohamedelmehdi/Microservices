using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Orders.Api.Data;
using Orders.Api.Extensions;
using Orders.Api.Models;

namespace Orders.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersApiContext _context;
        private readonly IDistributedCache _cache;

        public OrdersController(OrdersApiContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrder()
        {
            return await _context.Order.ToListAsync();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id, CancellationToken ct)
        {
            // Attempt to retrieve the order from the distributed cache
            var cachedOrder = await _cache.GetStringAsync($"orders-{id}", ct);

            Order order;
            if (!string.IsNullOrEmpty(cachedOrder))
            {
                // Deserialize the cached order
                order = JsonConvert.DeserializeObject<Order>(cachedOrder);
            }
            else
            {
                // If the order is not in the cache, retrieve it from the database
                order = await _context.Order.FindAsync(id);

                if (order != null)
                {
                    // Serialize and store the order in the cache with a defined expiration time
                    var serializedOrder = JsonConvert.SerializeObject(order);
                    await _cache.SetStringAsync($"orders-{id}", serializedOrder, 
                        CacheOptions.DefaultExpiration, 
                        ct
                        );
                }
            }

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        // PUT: api/Orders/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.Id)
            {
                return BadRequest();
            }

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync($"orders-{id}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Orders
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
            _context.Order.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOrder", new { id = order.Id }, order);
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            await _cache.RemoveAsync($"orders-{id}");

            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.Id == id);
        }
    }
}
