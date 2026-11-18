using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderAPI.Consumer;
using OrderAPI.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("OrdersDb")
        ?? throw new InvalidOperationException("Connection string 'OrdersDb' is missing.")));

var rabbitMq = builder.Configuration.GetSection("RabbitMQ");
var rabbitHost = rabbitMq["Host"] ?? "localhost";
var rabbitPort = rabbitMq.GetValue<ushort>("Port");
var rabbitVirtualHost = rabbitMq["VirtualHost"] ?? "/";
var rabbitUsername = rabbitMq["Username"] ?? "guest";
var rabbitPassword = rabbitMq["Password"] ?? "guest";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProductCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, rabbitPort, rabbitVirtualHost, h =>
        {
            h.Username(rabbitUsername);
            h.Password(rabbitPassword);
        });

        cfg.ReceiveEndpoint("event-listener", e =>
        {
            e.ConfigureConsumer<ProductCreatedConsumer>(context);
        });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
