using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;
using ProductAPI.Models;
using Shared.Messages;

namespace ProductAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        ProductDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<ProductsController> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        return await _context.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetProduct(Guid id)
    {
        var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? NotFound() : product;
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return BadRequest("Product name is required.");
        }

        if (product.Id == Guid.Empty)
        {
            product.Id = Guid.NewGuid();
        }

        product.CreatedAt = DateTimeOffset.UtcNow;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        using var publishTimeout = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
        publishTimeout.CancelAfter(TimeSpan.FromSeconds(2));

        try
        {
            await _publishEndpoint.Publish(new ProductCreated(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Stock,
                product.CreatedAt), publishTimeout.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Product {ProductId} was saved but the ProductCreated event could not be published.", product.Id);
        }

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] Product product)
    {
        if (id != product.Id)
        {
            return BadRequest("The route id and the product id must match.");
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return BadRequest("Product name is required.");
        }

        var exists = await _context.Products.AnyAsync(p => p.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
