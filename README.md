# Nordly

[![.NET](https://github.com/Khhashi/Nordly/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Khhashi/Nordly/actions/workflows/dotnet.yml)
![.NET 8](https://img.shields.io/badge/.NET_8-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?logo=postgresql&logoColor=white)
![Stripe](https://img.shields.io/badge/Stripe-635BFF?logo=stripe&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)

Nettbutikk bygget i .NET 8 med Razor Pages, PostgreSQL og Stripe Checkout. Kunden kan bla i produkter, legge varer i handlekurven, betale med Stripe og få ordrebekreftelse på e-post.

**[Live demo ↗](https://ordermanager-8ym2.onrender.com)**<br>
Kan bruke opptil ett minutt på å starte (Render gratisnivå).

## Grensesnitt

Her er grensesnittet til nettsiden, fra forsiden og produktsiden til handlekurven og bekreftelsen etter betaling.

<table>
  <tr>
    <td width="50%"><img src="https://github.com/user-attachments/assets/560c83d7-2a2e-4eed-8974-195f626803af" alt="Forsiden til Nordly" /></td>
    <td width="50%"><img src="https://github.com/user-attachments/assets/eae616b7-d69d-41ae-a0f7-9e5eaca18278" alt="Produktside i Nordly" /></td>
  </tr>
  <tr>
    <td width="50%"><img src="https://github.com/user-attachments/assets/941fecbf-1b17-4102-8df8-68437982fca7" alt="Handlekurv med Stripe-betaling i Nordly" /></td>
    <td width="50%"><img src="https://github.com/user-attachments/assets/7514ae8e-1c76-4f5b-9215-e99cdca5bba1" alt="Ordrebekreftelse etter betaling i Nordly" /></td>
  </tr>
</table>

## Funksjoner

- Produktkatalog med kategorifiltre og produktsider
- Handlekurv med antallsstyring
- Stripe Checkout med signert webhook for betalingsstatus
- Fraktvalg med gratis frakt fra 800 kr
- Ordrebekreftelse på e-post via SMTP
- Nyhetsbrev lagret i PostgreSQL

## Teknologi

**C#, .NET 8, Razor Pages · PostgreSQL, Entity Framework Core · Stripe · NUnit · Docker, GitHub Actions, Render**

Løsningen er delt i egne prosjekter for web, domene, infrastruktur og tester, slik at forretningslogikken ikke er avhengig av web eller database.

## Tester og CI

22 automatiserte enhets- og integrasjonstester i NUnit dekker ordre- og produktregler, `OrderService` og lagring av Stripe-betalinger i databasen. GitHub Actions bygger og kjører testene på hver pull request.

## Arbeidsflyt

Hver oppgave starter som et issue og utvikles på en egen feature-branch. Endringen går gjennom en pull request med code review, og merges til `main` først når bygg og tester er grønne i GitHub Actions.

## Kjør lokalt

Krever .NET 8 SDK og Docker.

```bash
docker compose up -d
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=nordly;Username=postgres;Password=postgres" --project Nordly.Web
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project Nordly.Web
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project Nordly.Web
dotnet run --project Nordly.Web
```

Kjør testene med `dotnet test`. Bruk Stripe-testkortet `4242 4242 4242 4242`.
