# OrderManager

**Live demo:** [Open OrderManager](https://ordermanager-8ym2.onrender.com/)

OrderManager is a .NET 10 C# order management application with a layered architecture, JSON persistence, automated tests, and a Razor Pages web interface.

## Project structure

- `OrderManager.Web` - Browser-based Razor Pages interface
- `PG3302.Domain` - Domain entities, repository contracts, and business logic
- `PG3302.Infrastructure` - JSON-backed repository implementation
- `PG3302.Tests` - NUnit unit and integration tests

## Requirements

- .NET 10 SDK

## Run the web application

```bash
dotnet run --project "OrderManager.Web/OrderManager.Web.csproj"
```

To stop the local server, press `Ctrl+C` in the terminal.

## Deploy to Render

This repository includes a `Dockerfile` and `render.yaml` for deploying the web application as a Render Web Service.

1. Push the repository to GitHub.
2. In Render, choose **New +** and **Blueprint**.
3. Select the `Khhashi/OrderManager` repository.
4. Deploy the detected `ordermanager` web service.

Render will provide a public URL when the deployment finishes. The current JSON repository is suitable for a demo, but data on Render's free service can be lost when the service is redeployed or restarted. A database or persistent disk would be needed for permanent production data.

The web interface is a customer-facing storefront with a curated product catalog, search and category filters, session-based cart, and Stripe Checkout. The existing order management pages remain available for internal use, but are no longer part of the customer navigation.

## Configure Stripe Checkout

Create a Stripe account and use a test-mode secret key locally. Never commit the key to the repository.

```bash
export Stripe__SecretKey=sk_test_your_key_here
dotnet run --project "OrderManager.Web/OrderManager.Web.csproj"
```

For Render, add `Stripe__SecretKey` as a private environment variable. Stripe redirects customers back to `/Checkout/Success` only after the Checkout Session reports `paid`.

## Database and REST API

The application uses PostgreSQL through Entity Framework Core. Start a local database with Docker:

```bash
docker compose up -d postgres
dotnet run --project "OrderManager.Web/OrderManager.Web.csproj"
```

The default local connection is `Host=localhost;Port=5432;Database=nordly;Username=postgres;Password=postgres`. In Render, configure `ConnectionStrings__DefaultConnection` as a private environment variable.

Available endpoints:

- `GET /api/products` - list products
- `GET /api/orders/{id}` - read an order
- `POST /api/orders` - create an order from product IDs and quantities
- `POST /api/stripe/webhook` - verify Stripe webhook signatures
- `GET /health` - health check

## Run tests

```bash
dotnet test "OrderManager.sln" --nologo
```

## Features

- Create orders
- View orders by ID
- List all orders
- Update orders
- Delete orders
- Validate products, prices, quantities, and empty orders
- Persist orders to a local JSON file

## Notes

The application stores order data in a local `orders.json` file generated and updated at runtime. The domain and service layers are shared by both the console application and the web application.
