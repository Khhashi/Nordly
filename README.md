# Nordly

[![.NET](https://github.com/Khhashi/Nordly/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Khhashi/Nordly/actions/workflows/dotnet.yml)
![.NET 8](https://img.shields.io/badge/.NET_8-512BD4?logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?logo=postgresql&logoColor=white)
![Stripe](https://img.shields.io/badge/Stripe-635BFF?logo=stripe&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white)

Nettbutikk bygget i .NET 8 med Razor Pages, PostgreSQL og Stripe Checkout. Kunden kan bla i produkter, legge varer i handlekurven, betale med Stripe og få ordrebekreftelse på e-post.

**[Live demo ↗](https://ordermanager-8ym2.onrender.com)**<br>
<sub>Åpner med en gang på hverdager kl. 07–20. Ellers kan første besøk ta opptil ett minutt.</sub>

## Hvorfor jeg bygde det

Jeg bygde Nordly for å bli bedre på backend, og på det som faktisk er vanskelig i en nettbutikk: at én betaling alltid gir nøyaktig én ordre, også når kunden lukker nettleseren underveis. Jeg valgte C# og .NET for å lære et typet backend-rammeverk med lagdelt arkitektur.

## Grensesnitt

Her er grensesnittet til nettsiden, fra forsiden og produktsiden til handlekurven og bekreftelsen etter betaling.

<table>
  <tr>
    <td width="50%"><img src="https://github.com/user-attachments/assets/560c83d7-2a2e-4eed-8974-195f626803af" alt="Forsiden til Nordly" width="420" height="250" /></td>
    <td width="50%"><img src="https://github.com/user-attachments/assets/0c8704e8-188e-479d-8394-beb66aee889f" alt="Produktside i Nordly" width="420" height="250" /></td>
  </tr>
  <tr>
    <td width="50%"><img src="https://github.com/user-attachments/assets/b38e093d-8a52-4797-8d59-ab30505d83ab" alt="Handlekurv med Stripe-betaling i Nordly" width="420" height="250" /></td>
    <td width="50%"><img src="https://github.com/user-attachments/assets/7514ae8e-1c76-4f5b-9215-e99cdca5bba1" alt="Ordrebekreftelse etter betaling i Nordly" width="420" height="250" /></td>
  </tr>
</table>

## Funksjoner

- Produktkatalog med kategorifiltre og produktsider
- Handlekurv med antallsstyring
- Stripe Checkout med signert webhook for betalingsstatus
- Fraktvalg med gratis frakt fra 800 kr
- Ordrebekreftelse på e-post via Brevo sitt API
- Nyhetsbrev lagret i PostgreSQL

## Teknologi

**C#, .NET 8, Razor Pages · PostgreSQL (Neon), Entity Framework Core · Stripe · NUnit · Docker, GitHub Actions, Render**

## Tekniske valg og læring

- **Lagdelt arkitektur:** Løsningen er delt i egne prosjekter for web, domene, infrastruktur og tester. Domenet er uavhengig av web og database, så forretningsreglene kan testes isolert.
- **Betaling bekreftes av Stripe:** Ordren opprettes først når Stripe bekrefter betalingen, enten via en signert webhook eller når bekreftelsessiden henter økten fra Stripe. Begge veier bruker samme kode, og en unik Stripe-økt-ID i databasen sikrer at samme betaling bare gir én ordre og én e-post, selv om kunden lukker nettleseren.
- **Personvern:** Jeg oppdaget at ordre-API-et returnerte kundens navn, e-post og adresse uten innlogging. Jeg fikset det slik at API-et bare returnerer ordrelinjer og status, og fjernet et ubrukt endepunkt som lot hvem som helst opprette ordrer.

Se [arkitekturdokumentet](docs/ARCHITECTURE.md) for diagrammer, betalingsflyt og designbeslutninger.

## Tester og CI

40 automatiserte enhets- og integrasjonstester i NUnit dekker ordre-, produkt- og fraktregler, `OrderService`, at samme Stripe-betaling bare gir én ordre og lagring av betalinger i databasen. GitHub Actions bygger og kjører testene på hver pull request.

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
