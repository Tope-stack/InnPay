# Docker Setup Guide for InnPay

## Prerequisites
- Docker Desktop installed (or Docker Engine + Docker Compose on Linux)
- Docker version 20.10+
- Docker Compose version 1.29+

## Quick Start

### 1. Clone the Repository
```bash
git clone <repository-url>
cd InnPay
```

### 2. Configure Environment (Optional)
```bash
# Copy the example environment file
cp .env.example .env

# Edit .env and change sensitive values like JWT_SECRET_KEY
# Generate a strong 256-bit JWT secret key in production
```

### 3. Build and Run
```bash
# Build and start all services
docker-compose up -d

# View logs
docker-compose logs -f

# To follow only API logs
docker-compose logs -f api
```

## Services

### PostgreSQL Database
- **Container**: innpay_postgres
- **Port**: 5432
- **Username**: innpay_user
- **Password**: (see docker-compose.yml)
- **Database**: innpay_db
- **Volume**: postgres_data (persistent storage)

### InnPay API
- **Container**: innpay_api
- **Port**: 8080
- **Health Check**: Enabled
- **Volumes**: 
  - `./uploads` - File storage
  - `./logs` - Application logs

## Common Commands

### Start Services
```bash
docker-compose up -d
```

### Stop Services
```bash
docker-compose down
```

### Stop and Remove All Data
```bash
docker-compose down -v
```

### View Logs
```bash
docker-compose logs -f
docker-compose logs -f api
docker-compose logs -f postgres
```

### Rebuild Containers
```bash
docker-compose up -d --build
```

### Database Migration
Migrations run automatically when the API starts. If you need to manually apply migrations:
```bash
docker-compose exec api dotnet ef database update -p src/InnPay.Infrastructure/InnPay.Infrastructure.csproj
```

### Access Services

#### API Documentation (Swagger)
```
http://localhost:8080/swagger/index.html
```

#### Database Connection (from host)
```
Host: localhost
Port: 5432
Username: innpay_user
Password: (from .env or docker-compose.yml)
Database: innpay_db
```

## Development vs Production

### Development Environment
The current docker-compose.yml is configured for development with:
- Volumes for code hot-reload
- Health checks enabled
- Detailed logging

### Production Deployment
For production, you should:

1. **Use a separate `docker-compose.prod.yml`** with optimizations
2. **Use environment variables** for sensitive data:
   ```bash
   export JWT_SECRET_KEY="your-strong-secret-key"
   export POSTGRES_PASSWORD="your-secure-password"
   ```

3. **Use managed PostgreSQL** (AWS RDS, Google Cloud SQL, etc.) instead of containerized PostgreSQL

4. **Enable HTTPS** and configure proper SSL certificates

5. **Use a reverse proxy** (Nginx, Traefik) in front of the API

Example production compose file structure:
```yaml
services:
  api:
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: $DATABASE_URL
      Jwt__Key: $JWT_SECRET_KEY
```

## Troubleshooting

### API fails to connect to database
```bash
# Ensure PostgreSQL is healthy
docker-compose ps
docker-compose logs postgres
```

### Port Already in Use
Change port mappings in docker-compose.yml:
```yaml
ports:
  - "8081:8080"  # Change 8081 to available port
```

### Clear Everything and Start Fresh
```bash
docker-compose down -v
docker system prune -a
docker-compose up -d --build
```

### Check API Health
```bash
curl http://localhost:8080/health
```

## Volumes and Data Persistence

- **postgres_data**: PostgreSQL database files (persists between restarts)
- **uploads/**: User-uploaded files
- **logs/**: Application logs

To backup the database:
```bash
docker-compose exec postgres pg_dump -U innpay_user innpay_db > backup.sql
```

To restore:
```bash
docker-compose exec -T postgres psql -U innpay_user innpay_db < backup.sql
```

## Security Notes

⚠️ **Important for Production**:
- Never commit `.env` file with real secrets
- Generate a strong JWT secret key (use a tool like `openssl rand -base64 32`)
- Use environment variables for all sensitive data
- Run database on a private network
- Use HTTPS in production
- Implement proper authentication and authorization
- Regularly update base images (postgres, dotnet)

## Performance Optimization

For production deployments:
- Consider using PostgreSQL connection pooling (PgBouncer)
- Set appropriate resource limits in docker-compose.yml
- Use multi-stage Docker builds (already implemented)
- Cache Docker layers effectively
- Monitor container resource usage

## Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET in Docker](https://docs.microsoft.com/en-us/dotnet/docker/)
- [PostgreSQL Docker Hub](https://hub.docker.com/_/postgres)
