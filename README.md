# PG3302 Software Design

This project is a C# console application for managing orders.

## Project structure

- `PG3302 Software design` - Console application entry point
- `PG3302.Domain` - Domain model and business logic
- `PG3302.Infrastructure` - Repository implementation
- `PG3302.Tests` - Unit tests

## Requirements

- .NET 8 SDK

## Run the application

```bash
dotnet run --project "PG3302 Software design/PG3302 Software design.csproj"
```

## Run tests

```bash
dotnet test "PG3302 Software design.sln" --nologo
```

## Features

- Create orders
- View orders by ID
- List all orders
- Update orders
- Delete orders

## Notes

The application stores order data in a local JSON file generated at runtime.
