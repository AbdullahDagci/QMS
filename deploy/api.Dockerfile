FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props Qms.slnx ./
COPY backend/src/Qms.Api/Qms.Api.csproj backend/src/Qms.Api/
COPY backend/src/Qms.Application/Qms.Application.csproj backend/src/Qms.Application/
COPY backend/src/Qms.Contracts/Qms.Contracts.csproj backend/src/Qms.Contracts/
COPY backend/src/Qms.Domain/Qms.Domain.csproj backend/src/Qms.Domain/
COPY backend/src/Qms.Infrastructure/Qms.Infrastructure.csproj backend/src/Qms.Infrastructure/
RUN dotnet restore backend/src/Qms.Api/Qms.Api.csproj

COPY backend/src backend/src
RUN dotnet publish backend/src/Qms.Api/Qms.Api.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.10 AS final
RUN apt-get update \
    && apt-get install -y --no-install-recommends fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Qms.Api.dll"]
