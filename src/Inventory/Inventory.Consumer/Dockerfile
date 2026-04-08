FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/Events/Shared.Events/Shared.Events.csproj shared/Events/Shared.Events/
COPY shared/Contracts/Shared.Contracts/Shared.Contracts.csproj shared/Contracts/Shared.Contracts/
COPY shared/Messaging/Shared.Messaging.Abstractions/Shared.Messaging.Abstractions.csproj shared/Messaging/Shared.Messaging.Abstractions/
COPY shared/Messaging/Shared.Messaging.RabbitMQ/Shared.Messaging.RabbitMQ.csproj shared/Messaging/Shared.Messaging.RabbitMQ/
COPY src/Inventory/Inventory.Consumer/Inventory.Consumer.csproj src/Inventory/Inventory.Consumer/
RUN dotnet restore src/Inventory/Inventory.Consumer/Inventory.Consumer.csproj

COPY shared/Events/Shared.Events/ shared/Events/Shared.Events/
COPY shared/Contracts/Shared.Contracts/ shared/Contracts/Shared.Contracts/
COPY shared/Messaging/Shared.Messaging.Abstractions/ shared/Messaging/Shared.Messaging.Abstractions/
COPY shared/Messaging/Shared.Messaging.RabbitMQ/ shared/Messaging/Shared.Messaging.RabbitMQ/
COPY src/Inventory/Inventory.Consumer/ src/Inventory/Inventory.Consumer/
RUN dotnet publish src/Inventory/Inventory.Consumer/Inventory.Consumer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Inventory.Consumer.dll"]
