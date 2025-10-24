# Quick Start Guide

## 🚀 Get Up and Running in 5 Minutes

### Prerequisites

- Docker Desktop installed and running
- .NET 8 or 9 SDK installed

### Steps

#### 1. Start SQL Server

```bash
# Create .env file with your password
cp .env.example .env
# Edit .env and set a strong SA password

# Start SQL Server container
docker compose up -d

# Verify it's running (wait for "healthy" status)
docker compose ps
```

#### 2. (Optional) Install App User Init Scripts

```bash
# Run the installer
./INSTALL-INIT-SCRIPTS.sh

# Restart Docker to run init scripts
docker compose down
docker compose up -d

# Verify app user was created
docker exec -it dbdemo-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U library_app_user -P 'LibraryApp@2024!' \
  -Q "SELECT DB_NAME()"
```

#### 3. Configure Application Secrets

```bash
# Navigate to project folder
cd DbDemo

# Set admin (SA) password
dotnet user-secrets set "ConnectionStrings:SqlServerAdmin" \
  "Server=localhost,1453;User Id=sa;Password=YOUR_SA_PASSWORD;TrustServerCertificate=True;"

# Set app user password
dotnet user-secrets set "ConnectionStrings:LibraryDb" \
  "Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;Password=LibraryApp@2024!;TrustServerCertificate=True;"

# Verify secrets
dotnet user-secrets list
```

#### 4. Run the Application

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

## ✅ Verify Everything Works

You should see:

```
===========================================
Library Management System - ADO.NET Demo
===========================================

✅ Configuration loaded (Environment: Development)

📋 Configuration Summary:
─────────────────────────────────────────

🔌 Connection Strings:
   Admin (SA): Server=localhost,1453; Password=********; ...
   App User:   Server=localhost,1453; Database=LibraryDb; ...

🔒 Security Check:
   ✅ Admin password configured
   ✅ App user password configured
```

## 📚 Next Steps

- Read `docs/00-docker-setup.md` for Docker details
- Read `docs/01-project-setup.md` for configuration details
- Explore the commit history to learn incrementally

## 🆘 Troubleshooting

### Docker not healthy

```bash
# Check logs
docker compose logs sqlserver

# Common fix: restart
docker compose down
docker compose up -d
```

### User secrets not found

```bash
# Make sure you're in the DbDemo project folder
cd DbDemo

# Then run user-secrets commands
dotnet user-secrets list
```

### Password warnings in app

```bash
# Make sure you set both connection strings in user secrets
dotnet user-secrets set "ConnectionStrings:SqlServerAdmin" "..."
dotnet user-secrets set "ConnectionStrings:LibraryDb" "..."
```

---

**For detailed documentation, see the `docs/` folder!**
