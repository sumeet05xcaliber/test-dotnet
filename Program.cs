var builder = WebApplication.CreateBuilder(args);

// === Swagger / OpenAPI setup ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger UI in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// No HTTPS redirect to avoid warnings for now
// app.UseHttpsRedirection();

// --- In-memory data stores (no database) ---
var products = new List<Product>
{
    new Product(1, "Laptop", 1000m),
    new Product(2, "Mouse", 25m),
    new Product(3, "Keyboard", 45m)
};

var orders = new List<Order>();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// ---------- APIs (10+ endpoints) ----------

// Root (so http://localhost:5280/ works)
app.MapGet("/", () => "Root OK");

// Weather (from template)
app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// 1) Health check
app.MapGet("/health", () => Results.Ok("OK"));

// 2) App info
app.MapGet("/info", () =>
    Results.Ok(new { app = "TestDotnetApi", version = "1.0.0" }));

// 3) Get all products
app.MapGet("/api/products", () => Results.Ok(products));

// 4) Get product by id
app.MapGet("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

// 5) Create product
app.MapPost("/api/products", (Product product) =>
{
    if (products.Any(p => p.Id == product.Id))
    {
        return Results.Conflict($"Product with id {product.Id} already exists.");
    }

    products.Add(product);
    return Results.Created($"/api/products/{product.Id}", product);
});

// 6) Update product
app.MapPut("/api/products/{id:int}", (int id, Product updated) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    if (product is null)
    {
        return Results.NotFound();
    }

    product.Name = updated.Name;
    product.Price = updated.Price;
    return Results.Ok(product);
});

// 7) Delete product
app.MapDelete("/api/products/{id:int}", (int id) =>
{
    var product = products.FirstOrDefault(p => p.Id == id);
    if (product is null)
    {
        return Results.NotFound();
    }

    products.Remove(product);
    return Results.NoContent();
});

// 8) Get all orders
app.MapGet("/api/orders", () => Results.Ok(orders));

// 9) Get order by id
app.MapGet("/api/orders/{id:int}", (int id) =>
{
    var order = orders.FirstOrDefault(o => o.Id == id);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

// 10) Create order
app.MapPost("/api/orders", (Order order) =>
{
    var productExists = products.Any(p => p.Id == order.ProductId);
    if (!productExists)
    {
        return Results.BadRequest($"Product {order.ProductId} does not exist.");
    }

    if (orders.Any(o => o.Id == order.Id))
    {
        return Results.Conflict($"Order with id {order.Id} already exists.");
    }

    orders.Add(order);
    return Results.Created($"/api/orders/{order.Id}", order);
});

app.Run();

// === Models ===
public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }

    public Product() { }

    public Product(int id, string name, decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
    }
}

public class Order
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    public Order() { }

    public Order(int id, int productId, int quantity)
    {
        Id = id;
        ProductId = productId;
        Quantity = quantity;
    }
}
