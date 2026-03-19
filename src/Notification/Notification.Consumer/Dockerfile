FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY shared/Shared.Contracts/Shared.Contracts.csproj shared/Shared.Contracts/
COPY src/Notification/Notification.Consumer/Notification.Consumer.csproj src/Notification/Notification.Consumer/
RUN dotnet restore src/Notification/Notification.Consumer/Notification.Consumer.csproj

COPY shared/Shared.Contracts/ shared/Shared.Contracts/
COPY src/Notification/Notification.Consumer/ src/Notification/Notification.Consumer/
RUN dotnet publish src/Notification/Notification.Consumer/Notification.Consumer.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Notification.Consumer.dll"]
