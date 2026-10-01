FROM mcr.microsoft.com/dotnet/sdk:8.0.404-alpine3.20 AS build
WORKDIR /src
COPY src/ProfanityService/ProfanityService.csproj src/ProfanityService/
COPY src/Observability/Observability.csproj src/Observability/
RUN dotnet restore src/ProfanityService/ProfanityService.csproj
COPY . .
RUN dotnet publish src/ProfanityService/ProfanityService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0.11-alpine3.20
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProfanityService.dll"]
