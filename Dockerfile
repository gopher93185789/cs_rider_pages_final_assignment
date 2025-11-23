# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["cs_rider_pages_final_assignment.csproj", "./"]
COPY ["cache/cache.csproj", "cache/"]
COPY ["core/core.csproj", "core/"]
COPY ["jwt/jwt.csproj", "jwt/"]

# Restore dependencies
RUN dotnet restore "cs_rider_pages_final_assignment.csproj"

# Copy all source files
COPY . .

# Build the application
RUN dotnet build "cs_rider_pages_final_assignment.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "cs_rider_pages_final_assignment.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "cs_rider_pages_final_assignment.dll"]
