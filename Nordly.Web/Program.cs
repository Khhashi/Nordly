using Microsoft.AspNetCore.HttpOverrides;
using Nordly.Domain.Services;
using Nordly.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Nordly.Infrastructure.Data;
using Nordly.Infrastructure.Repositories;
using Nordly.Web;
using Nordly.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                          ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
var connectionString = DatabaseConnectionString.Normalize(rawConnectionString);
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
if (!string.IsNullOrWhiteSpace(builder.Configuration["Email:BrevoApiKey"]))
    builder.Services.AddHttpClient<IOrderConfirmationEmailSender, BrevoOrderConfirmationEmailSender>();
else
    builder.Services.AddScoped<IOrderConfirmationEmailSender, SmtpOrderConfirmationEmailSender>();
builder.Services.AddScoped<CheckoutOrderProcessor>();

var app = builder.Build();

// Render avslutter HTTPS foran appen. Les X-Forwarded-Proto så appen vet at forespørselen var https.
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

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
    db.Database.ExecuteSqlRaw("""
        ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "CustomerName" character varying(200);
        ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "CustomerEmail" character varying(320);
        ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "CustomerPhone" character varying(40);
        ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "ShippingAddress" character varying(1000);
        ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "ShippingCost" numeric(18, 2) NOT NULL DEFAULT 0;
        CREATE TABLE IF NOT EXISTS "NewsletterSubscribers" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "Email" character varying(320) NOT NULL,
            "SubscribedAt" timestamp with time zone NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_NewsletterSubscribers_Email"
            ON "NewsletterSubscribers" ("Email");
        """);
    if (!db.Products.Any())
    {
        db.Products.AddRange(StorefrontCatalog.Products.Select(product =>
            new Nordly.Domain.Entities.Product(product.Name, product.Price)
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
    if (order is null) return Results.NotFound();

    return Results.Ok(new
    {
        order.Id,
        order.CreatedAt,
        order.PaymentStatus,
        order.ShippingCost,
        Lines = order.OrderLines.Select(line => new
        {
            Product = line.Product.Name,
            line.Quantity
        })
    });
});
app.MapPost("/api/stripe/webhook", async (HttpRequest request, CheckoutOrderProcessor processor, IConfiguration configuration) =>
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

    // Ordren opprettes her selv om kunden lukker nettleseren før bekreftelsessiden lastes.
    // CheckoutOrderProcessor sørger for at samme betaling bare gir én ordre og én e-post.
    if (stripeEvent.Type is "checkout.session.completed" or "checkout.session.async_payment_succeeded"
        && stripeEvent.Data.Object is Stripe.Checkout.Session session)
    {
        await processor.ProcessPaidSessionAsync(session);
    }

    return Results.Ok();
});
app.MapRazorPages();

app.Run();
