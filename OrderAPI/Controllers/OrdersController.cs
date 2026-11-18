using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderAPI.Data;
using OrderAPI.Models;

namespace OrderAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;

    public OrdersController(OrderDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        return await _context.Orders.AsNoTracking().OrderBy(o => o.CustomerName).ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Order>> GetOrder(Guid id)
    {
        var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        return order is null ? NotFound() : order;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
    {
        if (string.IsNullOrWhiteSpace(order.CustomerName))
        {
            return BadRequest("Customer name is required.");
        }

        if (order.Quantity <= 0)
        {
            return BadRequest("Quantity must be greater than zero.");
        }

        if (order.Id == Guid.Empty)
        {
            order.Id = Guid.NewGuid();
        }

        order.CreatedAt = DateTimeOffset.UtcNow;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrder(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("products")]
    public async Task<ActionResult<IEnumerable<Product>>> GetReceivedProducts()
    {
        return await _context.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
    }

    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<Product>> GetReceivedProduct(Guid id)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? NotFound() : product;
    }
}
