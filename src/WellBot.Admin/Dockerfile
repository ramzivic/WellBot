# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["WellBot.Admin/WellBot.Admin.csproj", "WellBot.Admin/"]
COPY ["WellBot.Shared/WellBot.Shared.csproj", "WellBot.Shared/"]
RUN dotnet restore "./WellBot.Admin/WellBot.Admin.csproj"
COPY . .
WORKDIR "/src/WellBot.Admin"
RUN dotnet build "./WellBot.Admin.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WellBot.Admin.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for SQLite database with proper permissions
USER root
RUN mkdir -p /app/data && chown app:app /app/data
USER app

ENTRYPOINT ["dotnet", "WellBot.Admin.dll"]
