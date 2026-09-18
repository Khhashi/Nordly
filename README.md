# Nordly

**Testside:** Kommer snart. Vi legger ut lenken når nettbutikken er klar.

> **Status: Under aktiv utvikling**
>
> Nordly er en norsk nettbutikk under bygging. Kjernefunksjonene er på plass, men prosjektet er ikke ferdig ennå. Vi jobber nå med de siste funksjonene for netthandel og produksjonskontroll på Render.

## Hva vi jobber med nå

Hovedfokuset er Stripe og en komplett netthandelsflyt:

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

- Norsk nettbutikk med produktkatalog og produktdetaljer
- Handlekurv med antallsstyring og tydelige tilbakemeldinger
- Checkout-flyt med Stripe-integrasjon, webhook og betalingsstatus
- PostgreSQL og Entity Framework Core
- REST-endepunkter for produkter, ordre og helsesjekk
- GitHub Projects med plan, issues og arbeidsflyt
- `main` for stabil versjon og `develop` for videre arbeid

## Prosjektstruktur

- `Nordly.Web` - Razor Pages-nettbutikk og API
- `PG3302.Domain` - Domeneobjekter, repository-kontrakter og forretningslogikk
- `PG3302.Infrastructure` - PostgreSQL og Entity Framework Core
- `PG3302.Tests` - NUnit-enhetstester og integrasjonstester

## Krav

- .NET 8 SDK

## Kjør nettbutikken

```bash
dotnet run --project "Nordly.Web/Nordly.Web.csproj"
```

Stopp den lokale serveren med `Ctrl+C` i terminalen.

## Publiser til Render

Dette prosjektet inneholder `Dockerfile` og `render.yaml` for å publisere nettbutikken som en Render-nettjeneste.

1. Push prosjektet til GitHub.
2. Velg **New +** og **Blueprint** i Render.
3. Velg prosjektet `Khhashi/Nordly`.
4. Publiser den registrerte `nordly`-nettjenesten.

Render leverer den offentlige nettjenesten og den administrerte PostgreSQL-databasen gjennom Blueprint-oppsettet. Appen er fortsatt under utvikling, så den offentlige versjonen brukes til kontroll før nettbutikken regnes som produksjonsklar.

Nettgrensesnittet er en kundevendt nettbutikk med utvalgte produkter, kategorifiltre, øktbasert handlekurv og Stripe Checkout under aktiv utvikling. Eventuelle interne ordresider er fortsatt tilgjengelige i prosjektet, men vises ikke i kundemenyen.

## Konfigurer Stripe Checkout

Opprett en Stripe-konto og bruk testnøkler lokalt. Lagre dem med .NET User Secrets slik at de aldri legges inn i prosjektet.

```bash
dotnet user-secrets set "Stripe:SecretKey" "sk_test_your_key_here" --project "Nordly.Web/Nordly.Web.csproj"
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_your_webhook_secret_here" --project "Nordly.Web/Nordly.Web.csproj"
dotnet run --project "Nordly.Web/Nordly.Web.csproj"
```

For lokal testing av webhook kan du sende Stripe-hendelser til appen med Stripe CLI:

```bash
stripe listen --forward-to http://localhost:5088/api/stripe/webhook
```

Bruk signeringshemmeligheten `whsec_...` som Stripe CLI viser, som verdi for `Stripe:WebhookSecret`. I Render legger du inn `Stripe__SecretKey` og `Stripe__WebhookSecret` som private miljøvariabler. Stripe sender kunden tilbake til `/Checkout/Success` først etter at Checkout Session har status `paid`.

## Database og REST-API

Appen bruker PostgreSQL gjennom Entity Framework Core. I produksjon leverer Render tilkoblingsstrengen gjennom `ConnectionStrings__DefaultConnection`; ingen databasepassord lagres i prosjektet.

For lokal utvikling oppgir du en privat tilkoblingsstreng gjennom User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_POSTGRES_CONNECTION_STRING" --project "Nordly.Web/Nordly.Web.csproj"
dotnet run --project "Nordly.Web/Nordly.Web.csproj"
```

Render konfigurerer `ConnectionStrings__DefaultConnection` automatisk fra den administrerte `nordly-db`-databasen i `render.yaml`.

Tilgjengelige endepunkter:

- `GET /api/products` - hent produkter
- `GET /api/orders/{id}` - hent en ordre
- `POST /api/orders` - opprett en ordre fra produkt-ID-er og antall
- `POST /api/stripe/webhook` - kontroller Stripe-webhooksignaturer
- `GET /health` - helsekontroll

## Kjør tester

```bash
dotnet test "Nordly.sln" --nologo
```

## Prosjektstatus

Prosjektet vedlikeholdes aktivt gjennom GitHub Issues, Projects og Pull Requests. Nye endringer utvikles på en feature-branch, kontrolleres av GitHub Actions, gjennomgås i en PR og merges først når kontrollene er godkjent.
