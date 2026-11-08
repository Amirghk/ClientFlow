FROM mcr.microsoft.com/dotnet/sdk:10.0 AS source
WORKDIR /src
COPY global.json Directory.Build.props NuGet.Config ./
COPY .config/ .config/
COPY backend/ backend/

FROM source AS development
RUN dotnet restore backend/ClientFlow.Api/ClientFlow.Api.csproj --locked-mode
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1
EXPOSE 8080
CMD ["dotnet", "watch", "--non-interactive", "--project", "backend/ClientFlow.Api/ClientFlow.Api.csproj", "run", "--no-launch-profile", "--urls", "http://0.0.0.0:8080"]

FROM source AS build
RUN dotnet restore backend/ClientFlow.Api/ClientFlow.Api.csproj --locked-mode
RUN dotnet publish backend/ClientFlow.Api/ClientFlow.Api.csproj -c Release --no-restore -o /out/api /p:UseAppHost=false
RUN dotnet tool restore
RUN dotnet ef migrations bundle --project backend/ClientFlow.Infrastructure --configuration Release --output /out/migrations/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
USER $APP_UID

FROM runtime AS migrate
COPY --from=build --chmod=755 /out/migrations/efbundle ./efbundle
ENTRYPOINT ["./efbundle"]

FROM runtime AS final
COPY --from=build /out/api .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ClientFlow.Api.dll"]
