FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/Contracts/Shared.Contracts/Shared.Contracts.csproj shared/Contracts/Shared.Contracts/
COPY shared/Messaging/Shared.Messaging.Abstractions/Shared.Messaging.Abstractions.csproj shared/Messaging/Shared.Messaging.Abstractions/
COPY shared/Messaging/Shared.Messaging.RabbitMQ/Shared.Messaging.RabbitMQ.csproj shared/Messaging/Shared.Messaging.RabbitMQ/
COPY src/Orders/Orders.API/Orders.API.csproj src/Orders/Orders.API/
RUN dotnet restore src/Orders/Orders.API/Orders.API.csproj

COPY shared/Contracts/Shared.Contracts/ shared/Contracts/Shared.Contracts/
COPY shared/Messaging/Shared.Messaging.Abstractions/ shared/Messaging/Shared.Messaging.Abstractions/
COPY shared/Messaging/Shared.Messaging.RabbitMQ/ shared/Messaging/Shared.Messaging.RabbitMQ/
COPY src/Orders/Orders.API/ src/Orders/Orders.API/
RUN dotnet publish src/Orders/Orders.API/Orders.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Orders.API.dll"]
