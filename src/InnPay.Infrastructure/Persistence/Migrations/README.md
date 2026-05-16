## Running EF Core Migrations

After cloning the project and configuring your connection string in appsettings.json,
run the following commands from the solution root:

```bash
# Install EF Core tools (once)
dotnet tool install --global dotnet-ef

# Create the initial migration
dotnet ef migrations add InitialCreate \
  --project src/InnPay.Infrastructure \
  --startup-project src/InnPay.API \
  --output-dir Persistence/Migrations

# Apply migration to the database
dotnet ef database update \
  --project src/InnPay.Infrastructure \
  --startup-project src/InnPay.API
```

The application also auto-migrates on startup in Development and Staging environments
(see Program.cs → `db.Database.MigrateAsync()`).
