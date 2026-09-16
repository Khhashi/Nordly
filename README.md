# OrderManager

**Live demo:** [Åpne OrderManager](https://ordermanager-8ym2.onrender.com/)

> **Status: Under aktiv utvikling**
>
> OrderManager er en norsk nettbutikk under bygging. Vi jobber akkurat nå med å gjøre Stripe-betalingen komplett og klar for en trygg kjøpsflyt.

## Hva vi jobber med nå

Hovedfokuset er Stripe og en komplett e-commerce-flyt:

- Stripe testmiljø og sikre miljøvariabler
- Stripe Checkout fra handlekurven
- Webhook for bekreftet betaling
- Lagring av betalingsstatus og kobling til ordre
- Beskyttelse mot dupliserte ordre
- Tester av hele betalingsflyten

## Hva som er ferdig

- Norsk storefront med produktkatalog og produktdetaljer
- Handlekurv med antallsstyring og tydelige tilbakemeldinger
- Checkout-flyt med Stripe-integrasjon under videre utvikling
- PostgreSQL og Entity Framework Core
- REST-endepunkter for produkter, ordre og helsesjekk
- GitHub Projects med plan, issues og arbeidsflyt
- `main` for stabil versjon og `develop` for videre arbeid

## Project structure

- `OrderManager.Web` - Razor Pages storefront og API
- `PG3302.Domain` - Domeneobjekter, repository-kontrakter og forretningslogikk
- `PG3302.Infrastructure` - PostgreSQL og Entity Framework Core
- `PG3302.Tests` - NUnit-enhetstester og integrasjonstester

## Requirements

- .NET 8 SDK

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

The web interface is a customer-facing storefront with a curated product catalog, category filters, session-based cart, and Stripe Checkout under active development. The existing order management pages remain available for internal use, but are no longer part of the customer navigation.

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
