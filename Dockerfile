FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Nordly.Web/Nordly.Web.csproj", "Nordly.Web/"]
COPY ["PG3302.Domain/PG3302.Domain.csproj", "PG3302.Domain/"]
COPY ["PG3302.Infrastructure/PG3302.Infrastructure.csproj", "PG3302.Infrastructure/"]
RUN dotnet restore "Nordly.Web/Nordly.Web.csproj"

COPY . .
WORKDIR "/src/Nordly.Web"
RUN dotnet publish "Nordly.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet Nordly.Web.dll --urls http://0.0.0.0:${PORT:-10000}"]
