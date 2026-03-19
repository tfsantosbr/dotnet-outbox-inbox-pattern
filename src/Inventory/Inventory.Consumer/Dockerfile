FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/Shared.Contracts/Shared.Contracts.csproj shared/Shared.Contracts/
COPY src/Inventory/Inventory.Consumer/Inventory.Consumer.csproj src/Inventory/Inventory.Consumer/
RUN dotnet restore src/Inventory/Inventory.Consumer/Inventory.Consumer.csproj

COPY shared/Shared.Contracts/ shared/Shared.Contracts/
COPY src/Inventory/Inventory.Consumer/ src/Inventory/Inventory.Consumer/
RUN dotnet publish src/Inventory/Inventory.Consumer/Inventory.Consumer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Inventory.Consumer.dll"]
