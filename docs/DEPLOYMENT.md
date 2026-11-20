# Nordly – Utrulling

**[README](../README.md)** | **[Arkitektur](ARCHITECTURE.md)** | **[Testguide](TESTING.md)**

## Databaseskjema

EF Core-migreringene ligger i `Nordly.Infrastructure/Migrations`. Appen kjører ikke migreringer ved oppstart og stopper hvis databasen har ventende endringer. Migrer databasen før appversjonen startes.

Den første migreringen er en additiv baseline. Den oppretter manglende tabeller og kolonner uten å slette eksisterende data, og kan brukes både på en tom database og på databasen som tidligere ble opprettet med `EnsureCreated` og oppstarts-SQL.

## Produksjonssetting

1. Kjør GitHub Actions-workflowen [Production database migrations](../.github/workflows/production-migrations.yml) fra `main`.
2. Bekreft produksjonsmigreringen og legg `NORDLY_PRODUCTION_CONNECTION_STRING` i GitHub-miljøet `production`. Obligatorisk godkjenning anbefales.
3. Workflowen bygger valgt `main`-commit og kjører migreringene. Den ruller ikke ut appen.
4. Når migreringene er fullført, rull ut den samme commiten i Render.

For første oppsett kjøres workflowen mot en tom database. Ikke legg tilkoblingsstrengen i repoet, byggelogger eller chat.
