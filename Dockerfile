FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["OrderManager.Web/OrderManager.Web.csproj", "OrderManager.Web/"]
COPY ["PG3302.Domain/PG3302.Domain.csproj", "PG3302.Domain/"]
COPY ["PG3302.Infrastructure/PG3302.Infrastructure.csproj", "PG3302.Infrastructure/"]
RUN dotnet restore "OrderManager.Web/OrderManager.Web.csproj"

COPY . .
WORKDIR "/src/OrderManager.Web"
RUN dotnet publish "OrderManager.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet OrderManager.Web.dll --urls http://0.0.0.0:${PORT:-10000}"]
