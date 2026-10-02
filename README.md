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
    <td width="50%"><img src="https://github.com/user-attachments/assets/14e054b3-d045-4a3d-a2b2-c3858c033dca" alt="Forsiden til Nordly med hero, produktbilde og kategorier" width="420" /></td>
    <td width="50%"><img src="https://github.com/user-attachments/assets/6dd6c254-75d0-460a-bee6-94c5bab1c7ce" alt="Produktside i Nordly med antallsvelger" width="420" /></td>
  </tr>
  <tr>
    <td width="50%"><img src="https://github.com/user-attachments/assets/2acd3f1e-7811-4077-8e96-4c4094e79a14" alt="Handlekurv med frakt og Stripe-betaling i Nordly" width="420" /></td>
    <td width="50%"><img src="https://github.com/user-attachments/assets/98f19559-a8e0-41e5-b766-d724ce6ea6d5" alt="Ordrebekreftelse etter betaling i Nordly" width="420" /></td>
  </tr>
</table>

## Funksjoner

- Produktkatalog med kategorifiltre og produktsider
- Handlekurv med antallsstyring (maks 10 per produkt)
- Stripe Checkout med signert webhook for betalingsstatus
- Fraktvalg med gratis frakt fra 800 kr
- Ordrebekreftelse på e-post via Brevo sitt API
- Nyhetsbrev lagret i PostgreSQL

## Teknologi

**C#, .NET 8, Razor Pages · PostgreSQL (Neon), Entity Framework Core · Stripe · NUnit · Docker, GitHub Actions, Render**

## Tekniske valg og læring

- **Lagdelt arkitektur:** Løsningen er delt i egne prosjekter for web, domene, infrastruktur og tester. Domenet er uavhengig av web og database, så forretningsreglene kan testes isolert.
- **Betaling bekreftes av Stripe:** Ordren opprettes først når Stripe bekrefter betalingen, enten via en signert webhook eller når bekreftelsessiden henter økten fra Stripe. Begge veier bruker samme kode, og en unik Stripe-økt-ID i databasen sikrer at samme betaling bare gir én ordre og én e-post, selv om kunden lukker nettleseren.
- **Personvern:** Jeg oppdaget at ordre-API-et returnerte kundens navn, e-post og adresse uten innlogging. Først begrenset jeg hva API-et returnerte og fjernet et endepunkt som lot hvem som helst opprette ordrer. Senere fjernet jeg hele ordre-API-et, siden ingen del av nettsiden brukte det.
- **Drift på gratisplan:** Render blokkerer utgående SMTP, så e-post sendes via Brevo sitt HTTP-API. Containeren mister disken ved omstart, så nøklene som krypterer cookies og skjemaer lagres i Postgres.

Se [arkitekturdokumentet](docs/ARCHITECTURE.md) for diagrammer, betalingsflyt og designbeslutninger.

## Tester og CI

47 automatiserte enhets- og integrasjonstester i NUnit dekker ordre-, produkt- og fraktregler, handlekurven, `OrderService`, at samme Stripe-betaling bare gir én ordre, lagring i databasen og at krypteringsnøklene overlever omstart. GitHub Actions bygger løsningen, kjører testene og bygger Docker-imaget på hver pull request.

## Arbeidsflyt

Hver oppgave starter som et issue og utvikles på en egen feature-branch. Endringen går gjennom en pull request og merges til `main` først når bygg og tester er grønne i GitHub Actions.

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
