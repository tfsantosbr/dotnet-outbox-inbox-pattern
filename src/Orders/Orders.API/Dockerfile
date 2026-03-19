FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/Shared.Contracts/Shared.Contracts.csproj shared/Shared.Contracts/
COPY src/Orders/Orders.API/Orders.API.csproj src/Orders/Orders.API/
RUN dotnet restore src/Orders/Orders.API/Orders.API.csproj

COPY shared/Shared.Contracts/ shared/Shared.Contracts/
COPY src/Orders/Orders.API/ src/Orders/Orders.API/
RUN dotnet publish src/Orders/Orders.API/Orders.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Orders.API.dll"]
