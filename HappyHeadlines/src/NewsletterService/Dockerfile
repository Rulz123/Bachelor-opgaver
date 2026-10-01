FROM mcr.microsoft.com/dotnet/sdk:8.0.404-alpine3.20 AS build
WORKDIR /src
COPY src/NewsletterService/NewsletterService.csproj src/NewsletterService/
COPY src/Messaging/Messaging.csproj src/Messaging/
COPY src/Observability/Observability.csproj src/Observability/
RUN dotnet restore src/NewsletterService/NewsletterService.csproj
COPY . .
RUN dotnet publish src/NewsletterService/NewsletterService.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0.11-alpine3.20
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NewsletterService.dll"]
