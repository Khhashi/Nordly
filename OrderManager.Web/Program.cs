using PG3302.Domain.Services;
using PG3302.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using PG3302.Infrastructure.Data;
using PG3302.Infrastructure.Repositories;
using OrderManager.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    var stripeSecretKey = app.Configuration["Stripe:SecretKey"];
    var stripeWebhookSecret = app.Configuration["Stripe:WebhookSecret"];
    if (string.IsNullOrWhiteSpace(stripeSecretKey) || string.IsNullOrWhiteSpace(stripeWebhookSecret))
    {
        throw new InvalidOperationException("Stripe:SecretKey and Stripe:WebhookSecret must be configured in production.");
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.EnsureCreated();
    if (!db.Products.Any())
    {
        db.Products.AddRange(StorefrontCatalog.Products.Select(product =>
            new PG3302.Domain.Entities.Product(product.Name, product.Price)
            {
                Id = product.Id
            }));
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapGet("/health", async (OrderDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok("healthy")
        : Results.Problem("Database is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable));
app.MapGet("/api/products", async (OrderDbContext db) =>
    Results.Ok(await db.Products.AsNoTracking().OrderBy(product => product.Name).ToListAsync()));
app.MapGet("/api/orders/{id:guid}", async (Guid id, OrderDbContext db) =>
{
    var order = await db.Orders
        .AsNoTracking()
        .Include(item => item.OrderLines)
        .ThenInclude(line => line.Product)
        .SingleOrDefaultAsync(item => item.Id == id);
    return order is null ? Results.NotFound() : Results.Ok(order);
});
app.MapPost("/api/orders", async (CreateOrderRequest request, OrderDbContext db) =>
{
    if (request.Items.Count == 0 || request.Items.Any(item => item.Quantity <= 0))
        return Results.BadRequest(new { error = "Order must contain valid items." });

    var productIds = request.Items.Select(item => item.ProductId).ToList();
    var products = await db.Products.Where(product => productIds.Contains(product.Id)).ToDictionaryAsync(product => product.Id);
    if (products.Count != productIds.Distinct().Count())
        return Results.BadRequest(new { error = "One or more products do not exist." });

    var order = new PG3302.Domain.Entities.Order();
    foreach (var item in request.Items)
        order.AddProduct(products[item.ProductId], item.Quantity);

    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/api/orders/{order.Id}", order);
});
app.MapPost("/api/stripe/webhook", async (HttpRequest request, OrderService orderService, IConfiguration configuration) =>
{
    var webhookSecret = configuration["Stripe:WebhookSecret"];
    if (string.IsNullOrWhiteSpace(webhookSecret))
        return Results.Problem("Stripe webhook secret is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);

    var json = await new StreamReader(request.Body).ReadToEndAsync();
    Stripe.Event stripeEvent;
    try
    {
        stripeEvent = Stripe.EventUtility.ConstructEvent(json, request.Headers["Stripe-Signature"], webhookSecret);
    }
    catch (Stripe.StripeException)
    {
        return Results.BadRequest();
    }

    if (stripeEvent.Type == "checkout.session.completed" && stripeEvent.Data.Object is Stripe.Checkout.Session session)
    {
        var order = orderService.GetAllOrders().SingleOrDefault(item => item.StripeCheckoutSessionId == session.Id);
        if (order != null && order.PaymentStatus != "Paid")
        {
            order.PaymentStatus = session.PaymentStatus == "paid" ? "Paid" : "Pending";
            order.StripePaymentIntentId = session.PaymentIntentId;
            orderService.UpdateOrder(order);
        }
    }
    else if (stripeEvent.Type == "payment_intent.succeeded" && stripeEvent.Data.Object is Stripe.PaymentIntent paymentIntent)
    {
        var order = orderService.GetAllOrders().SingleOrDefault(item => item.StripePaymentIntentId == paymentIntent.Id);
        if (order != null && order.PaymentStatus != "Paid")
        {
            order.PaymentStatus = "Paid";
            orderService.UpdateOrder(order);
        }
    }

    return Results.Ok();
});
app.MapRazorPages();

app.Run();

public sealed record CreateOrderRequest(List<CreateOrderItem> Items);
public sealed record CreateOrderItem(Guid ProductId, int Quantity);
