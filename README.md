# OrderManager

OrderManager is a .NET 8 C# order management application with a layered architecture, JSON persistence, automated tests, and a Razor Pages web interface.

## Project structure

- `OrderManager` - Console application entry point
- `OrderManager.Web` - Browser-based Razor Pages interface
- `PG3302.Domain` - Domain entities, repository contracts, and business logic
- `PG3302.Infrastructure` - JSON-backed repository implementation
- `PG3302.Tests` - NUnit unit and integration tests

## Requirements

- .NET 8 SDK

## Run the web application

```bash
dotnet run --project "OrderManager.Web/OrderManager.Web.csproj"
```

Open http://localhost:5055 in a browser.

To stop the local server, press `Ctrl+C` in the terminal.

## Deploy to Render

This repository includes a `Dockerfile` and `render.yaml` for deploying the web application as a Render Web Service.

1. Push the repository to GitHub.
2. In Render, choose **New +** and **Blueprint**.
3. Select the `Khhashi/OrderManager` repository.
4. Deploy the detected `ordermanager` web service.

Render will provide a public URL when the deployment finishes. The current JSON repository is suitable for a demo, but data on Render's free service can be lost when the service is redeployed or restarted. A database or persistent disk would be needed for permanent production data.

The web interface supports:

- Creating orders
- Viewing all orders
- Opening a separate order details page
- Updating order information
- Deleting orders

## Run the console application

```bash
dotnet run --project "OrderManager/OrderManager.csproj"
```

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
