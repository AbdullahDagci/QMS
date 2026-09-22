FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props Qms.slnx ./
COPY backend/src/Qms.Application/Qms.Application.csproj backend/src/Qms.Application/
COPY backend/src/Qms.Contracts/Qms.Contracts.csproj backend/src/Qms.Contracts/
COPY backend/src/Qms.Domain/Qms.Domain.csproj backend/src/Qms.Domain/
COPY backend/src/Qms.Infrastructure/Qms.Infrastructure.csproj backend/src/Qms.Infrastructure/
COPY backend/src/Qms.Worker/Qms.Worker.csproj backend/src/Qms.Worker/
RUN dotnet restore backend/src/Qms.Worker/Qms.Worker.csproj

COPY backend/src backend/src
RUN dotnet publish backend/src/Qms.Worker/Qms.Worker.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0.10 AS final
RUN mkdir -p /data/qms-files && chown -R $APP_UID:$APP_UID /data/qms-files
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Qms.Worker.dll"]
