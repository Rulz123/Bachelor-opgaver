FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Demo/Demo.csproj Demo/
RUN dotnet restore Demo/Demo.csproj
COPY Demo/ Demo/
RUN dotnet publish Demo/Demo.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Demo.dll"]