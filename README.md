# Nordly

[![.NET](https://github.com/Khhashi/Nordly/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Khhashi/Nordly/actions/workflows/dotnet.yml)

Nordly er en nettbutikk bygget i .NET 8 med Razor Pages, PostgreSQL og Stripe Checkout. Kunden kan bla i produkter, legge varer i handlekurven, betale med Stripe og få ordrebekreftelse på e-post.

**[Live demo ↗](https://ordermanager-8ym2.onrender.com)**
Siden kjører på Renders gratisnivå og kan bruke opptil ett minutt på å starte første gang.

<!-- Legg skjermbildene i mappen docs/ og fjern kommentaren rundt linjene under -->
<!--
![Forside](docs/forside.png)
![Produktside](docs/produkt.png)
![Checkout](docs/checkout.png)
-->

## Funksjoner

- Produktkatalog med kategorifiltre og produktsider
- Handlekurv med antallsstyring, lagret i økten
- Checkout med Stripe, der betalingsstatus oppdateres via en signert webhook
- Kunde- og leveringsinformasjon med fraktvalg og gratis frakt over 800 kr
- Ordrebekreftelse på e-post via SMTP etter bekreftet betaling
- Påmelding til nyhetsbrev, lagret i PostgreSQL
- REST-API for produkter, ordre og helsesjekk

## Teknologier

| Område | Teknologi |
| --- | --- |
| Backend og frontend | C#, .NET 8, Razor Pages |
| Database | PostgreSQL (Neon i produksjon), Entity Framework Core |
| Betaling | Stripe Checkout og webhooks |
| Testing | NUnit, enhets- og integrasjonstester |
| Drift | Docker, GitHub Actions, Render |

## Arkitektur

Løsningen er delt i fire prosjekter, slik at forretningslogikken ikke er avhengig av web eller database:

| Prosjekt | Ansvar |
| --- | --- |
| `Nordly.Web` | Razor Pages-nettbutikk, REST-API og Stripe-webhook |
| `PG3302.Domain` | Entiteter, forretningsregler og repository-grensesnitt |
| `PG3302.Infrastructure` | PostgreSQL-tilgang med Entity Framework Core |
| `PG3302.Tests` | Enhets- og integrasjonstester |

## Tester

Løsningen har 22 automatiserte tester i NUnit. De dekker blant annet:

- Forretningsregler for ordre og produkter, som totalsum, ugyldig antall og tomme ordre
- `OrderService` mot et falskt repository
- Integrasjonstester mot databasen, inkludert lagring av Stripe-betalingsdata
- Tolkning av tilkoblingsstrenger for Render og Neon

GitHub Actions bygger løsningen og kjører alle testene ved hver push og pull request til `main` og `develop`.

```bash
dotnet test Nordly.sln --nologo
```

## Kjør lokalt

**Krav:** .NET 8 SDK og Docker, eller en egen PostgreSQL-database.

1. Start PostgreSQL:

   ```bash
   docker compose up -d
   ```

2. Legg inn tilkoblingsstreng og Stripe-testnøkler med User Secrets, slik at ingen hemmeligheter havner i koden:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=nordly;Username=postgres;Password=postgres" --project Nordly.Web
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project Nordly.Web
   dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project Nordly.Web
   ```

3. Start appen:

   ```bash
   dotnet run --project Nordly.Web
   ```

4. Valgfritt: Send Stripe-webhooks til appen lokalt med Stripe CLI:

   ```bash
   stripe listen --forward-to http://localhost:5055/api/stripe/webhook
   ```

Bruk testkortet `4242 4242 4242 4242` med en hvilken som helst fremtidig dato og CVC.

## REST-API

| Metode | Endepunkt | Beskrivelse |
| --- | --- | --- |
| `GET` | `/api/products` | Henter alle produkter |
| `GET` | `/api/orders/{id}` | Henter én ordre |
| `POST` | `/api/orders` | Oppretter en ordre fra produkt-ID-er og antall |
| `POST` | `/api/stripe/webhook` | Tar imot Stripe-hendelser og kontrollerer signaturen |
| `GET` | `/health` | Helsesjekk, inkludert databasetilkobling |

## Drift

Appen kjører som en Docker-tjeneste på Render, definert i `render.yaml`. Databasen ligger hos Neon. Alle hemmeligheter legges inn som private miljøvariabler i Render:

| Variabel | Formål |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Tilkobling til Neon PostgreSQL |
| `Stripe__SecretKey`, `Stripe__WebhookSecret` | Stripe |
| `Email__SmtpHost`, `Email__SmtpPort`, `Email__EnableSsl`, `Email__Username`, `Email__Password`, `Email__FromAddress`, `Email__FromName` | SMTP for ordrebekreftelse |

Uten SMTP-oppsett lagres ordren likevel, men e-posten hoppes over og det skrives en advarsel i loggen.

## Arbeidsflyt

- Oppgaver planlegges i GitHub Issues og Projects.
- Nye endringer utvikles på feature-brancher og merges via pull requests.
- En pull request merges først når bygg og tester er grønne i GitHub Actions.
- `main` er den stabile versjonen og `develop` brukes til videre arbeid.

## Kjente begrensninger

- Stripe kjører i testmodus, så ingen ekte betalinger gjennomføres.
- Renders gratisnivå går i dvale ved inaktivitet.
- En ende-til-ende-test av hele kjøpsflyten står på planen.
