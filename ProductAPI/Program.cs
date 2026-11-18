using MassTransit;
using Microsoft.EntityFrameworkCore;
using ProductAPI.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ProductsDb")
        ?? throw new InvalidOperationException("Connection string 'ProductsDb' is missing.")));

var rabbitMq = builder.Configuration.GetSection("RabbitMQ");
var rabbitHost = rabbitMq["Host"] ?? "localhost";
var rabbitPort = rabbitMq.GetValue<ushort>("Port");
var rabbitVirtualHost = rabbitMq["VirtualHost"] ?? "/";
var rabbitUsername = rabbitMq["Username"] ?? "guest";
var rabbitPassword = rabbitMq["Password"] ?? "guest";

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((_, cfg) =>
    {
        cfg.Host(rabbitHost, rabbitPort, rabbitVirtualHost, h =>
        {
            h.Username(rabbitUsername);
            h.Password(rabbitPassword);
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
