FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore backend/ClientFlow.Api/ClientFlow.Api.csproj --locked-mode
RUN dotnet publish backend/ClientFlow.Api/ClientFlow.Api.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /out .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ClientFlow.Api.dll"]
