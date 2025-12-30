using ShoppingCart.Application.Handlers;
using ShoppingCart.Domain.Interfaces;
using ShoppingCart.Infrastructure.BackgroundServices;
using ShoppingCart.Infrastructure.Persistence;
using ShoppingCart.Infrastructure.Repositories;
using ShoppingCart.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure MongoDB
var connectionString = builder.Configuration["MongoDbSettings:ConnectionString"] ?? "mongodb://localhost:27017";
var databaseName = builder.Configuration["MongoDbSettings:DatabaseName"] ?? "ShoppingCartDb";
builder.Services.AddSingleton(new ShoppingCartDbContext(connectionString, databaseName));

// Register repositories
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Configure MediatR - scan Application assembly
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateCartCommandHandler>());

// Configure HttpClient for Product Service
builder.Services.AddHttpClient<IProductService, ProductService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ProductServiceUrl"] ?? "http://localhost:5220");
});

// Add Background Service for cart cleanup
builder.Services.AddHostedService<CartCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
