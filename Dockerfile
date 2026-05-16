# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy solution and project files
COPY InnPay.sln .
COPY src/InnPay.API/InnPay.API.csproj src/InnPay.API/
COPY src/InnPay.Application/InnPay.Application.csproj src/InnPay.Application/
COPY src/InnPay.Domain/InnPay.Domain.csproj src/InnPay.Domain/
COPY src/InnPay.Infrastructure/InnPay.Infrastructure.csproj src/InnPay.Infrastructure/

# Restore dependencies
RUN dotnet restore InnPay.sln

# Copy all source code
COPY . .

# Build the application
RUN dotnet build InnPay.sln -c Release -o /app/build

# Publish stage
FROM build AS publish

RUN dotnet publish src/InnPay.API/InnPay.API.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create uploads directory for file storage
RUN mkdir -p /app/uploads

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "InnPay.API.dll"]
