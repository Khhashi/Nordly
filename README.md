# OrderManager

**Live demo:** Kommer snart. Vi legger ut lenken når nettbutikken er klar.

> **Status: Under aktiv utvikling**
>
> OrderManager er en norsk nettbutikk under bygging. Kjernefunksjonene er på plass, men prosjektet er ikke ferdig ennå. Vi jobber nå med de siste e-commerce-funksjonene og produksjonsverifisering på Render.

## Hva vi jobber med nå

Hovedfokuset er Stripe og en komplett e-commerce-flyt:

- Stripe testmiljø og sikre miljøvariabler
- Stripe Checkout fra handlekurven
- Webhook for bekreftet betaling
- Lagring av betalingsstatus og kobling til ordre
- Beskyttelse mot dupliserte ordre
- Tester av hele betalingsflyten

## Hva som gjenstår

- Kunde- og leveringsinformasjon
- Frakt og leveringsvalg
- Ordrebekreftelse på e-post
- Full manuell test av kjøpsflyten på Render
- Ekte nyhetsbrev-tjeneste

## Hva som er ferdig

- Norsk storefront med produktkatalog og produktdetaljer
- Handlekurv med antallsstyring og tydelige tilbakemeldinger
- Checkout-flyt med Stripe-integrasjon, webhook og betalingsstatus
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

Render provides the public web service and managed PostgreSQL database through the Blueprint configuration. The application is still under development, so the public deployment is used for verification before the store is considered production-ready.

The web interface is a customer-facing storefront with a curated product catalog, category filters, session-based cart, and Stripe Checkout under active development. The existing order management pages remain available for internal use, but are no longer part of the customer navigation.

## Configure Stripe Checkout

Create a Stripe account and use test-mode keys locally. Store them with .NET User Secrets so they are never committed to the repository.

```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_your_key_here" --project "OrderManager.Web/OrderManager.Web.csproj"
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_your_webhook_secret_here" --project "OrderManager.Web/OrderManager.Web.csproj"
dotnet run --project "OrderManager.Web/OrderManager.Web.csproj"
```

For local webhook testing, forward Stripe events to the application with the Stripe CLI:

```bash
stripe listen --forward-to http://localhost:5088/api/stripe/webhook
```

Use the `whsec_...` signing secret printed by Stripe CLI for `Stripe:WebhookSecret`. For Render, add `Stripe__SecretKey` and `Stripe__WebhookSecret` as private environment variables. Stripe redirects customers back to `/Checkout/Success` only after the Checkout Session reports `paid`.

## Database and REST API

The application uses PostgreSQL through Entity Framework Core. In production, the connection string is supplied by Render through `ConnectionStrings__DefaultConnection`; no database password is stored in the repository.

For local development, provide a private connection string through User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_POSTGRES_CONNECTION_STRING" --project "OrderManager.Web/OrderManager.Web.csproj"
dotnet run --project "OrderManager.Web/OrderManager.Web.csproj"
```

Render configures `ConnectionStrings__DefaultConnection` automatically from the managed `nordly-db` database in `render.yaml`.

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

## Project status

The repository is actively maintained through GitHub Issues, Projects and Pull Requests. New work is developed on a feature branch, checked by GitHub Actions, reviewed in a PR, and merged only after the checks pass.
