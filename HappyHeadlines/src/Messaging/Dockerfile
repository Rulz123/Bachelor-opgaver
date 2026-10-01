FROM mcr.microsoft.com/dotnet/sdk:8.0.404-alpine3.20 AS build
WORKDIR /src
COPY src/Messaging/Messaging.csproj src/Messaging/
COPY src/Observability/Observability.csproj src/Observability/
RUN dotnet restore src/Messaging/Messaging.csproj
COPY . .
RUN dotnet build src/Messaging/Messaging.csproj -c Release --no-restore
