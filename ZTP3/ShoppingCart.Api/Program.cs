using ShoppingCart.Application.Handlers;
using ShoppingCart.Domain.Interfaces;
using ShoppingCart.Infrastructure.BackgroundServices;
using ShoppingCart.Infrastructure.Persistence;
using ShoppingCart.Infrastructure.Repositories;
using ShoppingCart.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration["MongoDbSettings:ConnectionString"] ?? "mongodb://localhost:27017";
var databaseName = builder.Configuration["MongoDbSettings:DatabaseName"] ?? "ShoppingCartDb";
builder.Services.AddSingleton(new ShoppingCartDbContext(connectionString, databaseName));

builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateCartCommandHandler>());

builder.Services.AddHttpClient<IProductService, ProductService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ProductServiceUrl"] ?? "http://localhost:5220");
});

builder.Services.AddHttpClient();
builder.Services.AddHostedService<CartCleanupService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
