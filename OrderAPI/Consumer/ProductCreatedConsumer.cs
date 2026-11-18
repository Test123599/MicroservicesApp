using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderAPI.Data;
using OrderAPI.Models;
using Shared.Messages;

namespace OrderAPI.Consumer;

public class ProductCreatedConsumer : IConsumer<ProductCreated>
{
    private readonly OrderDbContext _context;
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(OrderDbContext context, ILogger<ProductCreatedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreated> context)
    {
        var message = context.Message;

        var alreadyReceived = await _context.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == message.Id, context.CancellationToken);

        if (alreadyReceived)
        {
            _logger.LogInformation("Product {ProductId} was already received and will be skipped.", message.Id);
            return;
        }

        _context.Products.Add(new Product
        {
            Id = message.Id,
            Name = message.Name,
            Description = message.Description,
            Price = message.Price,
            Stock = message.Stock,
            CreatedAt = message.CreatedAt,
            ReceivedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Product {ProductId} was stored locally in OrdersAPI.", message.Id);
    }
}
