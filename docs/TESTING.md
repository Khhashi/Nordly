# Nordly – Testguide

**[README](../README.md)** | **[Arkitektur](ARCHITECTURE.md)** | **[Utrulling](DEPLOYMENT.md)**

Denne guiden beskriver hvordan du kjører enhets-, PostgreSQL- og nettlesertestene lokalt.

## Forutsetninger

- .NET 8 SDK
- PostgreSQL for integrasjons- og nettlesertestene
- PowerShell 7 (`pwsh`) for å installere Chromium til Playwright

## Kjør lokalt

Start PostgreSQL og opprett en egen testdatabase én gang:

```bash
docker compose up -d
docker compose exec postgres createdb -U postgres nordly_test
```

Bygg testprosjektet og installer Chromium:

```bash
dotnet build Nordly.sln
pwsh Nordly.Tests/bin/Debug/net8.0/playwright.ps1 install chromium
```

Kjør testene med testdatabasen:

```bash
NORDLY_TEST_MIGRATION_CONNECTION="Host=localhost;Port=5432;Database=nordly_test;Username=postgres;Password=postgres" \
NORDLY_BROWSER_TEST_CONNECTION="Host=localhost;Port=5432;Database=nordly_test;Username=postgres;Password=postgres" \
dotnet test Nordly.sln
```

## Hva testes

- **NUnit:** Domeneregler, handlekurv, ordre, betaling, migreringer og PostgreSQL-integrasjon.
- **Playwright:** Produktsøk og kategori, legge varer i handlekurven og gjenopprette handlekurven etter Checkout-retur.

Playwright starter webappen i Development, kjører migreringene i testdatabasens standardskjema og blokkerer eksterne HTTPS-forespørsler. Ingen ekte betalinger, e-poster eller Stripe-/e-posthemmeligheter brukes.

## Databaseisolering

Bruk bare en dedikert testdatabase, aldri produksjon:

- `NORDLY_TEST_MIGRATION_CONNECTION` brukes av PostgreSQL-integrasjonstestene. Hver test oppretter og sletter sitt eget tilfeldige skjema.
- `NORDLY_BROWSER_TEST_CONNECTION` brukes av Playwright. Appen kjører migreringene i databasens standardskjema; denne testen deler derfor ikke database med utvikling eller produksjon.

Variablene kan peke på samme `nordly_test`-database. PostgreSQL-integrasjonstestene bruker egne skjemaer, mens nettlesertesten bruker standardskjemaet.
