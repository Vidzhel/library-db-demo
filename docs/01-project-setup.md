# 01 - Project Initialization and Configuration

## 📖 What You'll Learn

- How to structure a .NET console application project
- Managing NuGet packages with PackageReference
- Configuration management in .NET (appsettings.json, User Secrets, environment variables)
- Secure password storage strategies (never commit secrets!)
- The .NET configuration hierarchy and override system
- Connection string management for multiple environments

## 🎯 Why This Matters

Proper project setup and configuration management are **critical foundations** for any application:

- **Security**: Prevents accidentally committing passwords to Git
- **Flexibility**: Easy to change settings without recompiling code
- **Team Collaboration**: Each developer can have their own local settings
- **Environment Support**: Same code runs in Dev, Test, and Production with different configs
- **Maintainability**: Centralized configuration is easier to manage than hardcoded values

## 🔍 Key Concepts

### The .NET Configuration System

.NET provides a powerful, layered configuration system where settings from multiple sources are merged together with a specific priority order.

#### Configuration Sources (Lowest to Highest Priority)

1. **appsettings.json** - Base configuration for all environments
2. **appsettings.{Environment}.json** - Environment-specific overrides
3. **User Secrets** - Developer-specific secrets (Development only)
4. **Environment Variables** - System-level configuration (Production)
5. **Command-line arguments** - Runtime overrides (not used in this project)

**How it works**: Later sources override earlier ones. For example, a password in User Secrets will override the same key in appsettings.json.

### Why This Layered Approach?

```
appsettings.json (committed to Git)
└─ Contains: Server names, timeouts, non-sensitive defaults
   ❌ NEVER contains: Passwords, API keys, connection strings with credentials

User Secrets (local developer machine only)
└─ Contains: Development passwords, local database credentials
   ❌ NEVER committed to Git
   ✅ Each developer has their own

Environment Variables (production servers)
└─ Contains: Production passwords, API keys
   ✅ Set by deployment system (Azure, AWS, Docker, etc.)
   ❌ Not in source code
```

### appsettings.json vs appsettings.Development.json

| File | Purpose | Committed to Git? | Contains Secrets? |
|------|---------|-------------------|-------------------|
| `appsettings.json` | Base configuration for all environments | ✅ Yes | ❌ No |
| `appsettings.Development.json` | Development-specific overrides (like verbose logging) | ✅ Yes | ❌ No |
| `appsettings.Production.json` | Production-specific overrides | ✅ Yes | ❌ No |

### User Secrets

**User Secrets** is a .NET feature that stores secrets in a separate location outside your project folder:

**Location** (not in your project!):
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\secrets.json`
- **Mac/Linux**: `~/.microsoft/usersecrets/{UserSecretsId}/secrets.json`

**How it works**:
1. Your `.csproj` contains a `<UserSecretsId>` (just a GUID)
2. Secrets are stored in the location above (never in source control)
3. The configuration system automatically loads them in Development mode

### Connection Strings

A **connection string** is a formatted string containing information needed to connect to a database:

```
Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;Password=Secret123;TrustServerCertificate=True;
```

**Parts explained**:
- `Server=localhost,1453` - SQL Server address and port
- `Database=LibraryDb` - Which database to connect to
- `User Id=library_app_user` - Username for authentication
- `Password=Secret123` - Password (this should be in secrets!)
- `TrustServerCertificate=True` - Accept self-signed certificates (dev only)

### Why Two Connection Strings?

Our application uses **two different connection strings**:

1. **SqlServerAdmin** (SA account):
   - Used ONLY for creating databases and users
   - Used ONLY for running Migration V000 (creates app user)
   - Never used for normal application operations

2. **LibraryDb** (app user account):
   - Used for all normal database operations
   - Limited permissions (principle of least privilege)
   - This is what would run in production

## 📁 Project Structure

After this commit, your project looks like this:

```
DbDemo/
├── DbDemo.sln                          # Solution file
├── DbDemo/                             # Main project
│   ├── DbDemo.csproj                   # Project file with package references
│   ├── Program.cs                      # Entry point with configuration demo
│   ├── appsettings.json               # Base configuration (no secrets!)
│   ├── appsettings.Development.json   # Dev-specific settings
│   └── secrets.json.example           # Template showing what to put in user secrets
├── docs/
│   ├── 00-docker-setup.md
│   └── 01-project-setup.md            # This file
└── migrations/
    └── V000__create_app_user.sql
```

## 🚀 Setup Steps

### Step 1: Restore NuGet Packages

The project references several NuGet packages. Restore them:

```bash
cd DbDemo  # Navigate to project folder
dotnet restore
```

**What gets installed**:
- `Microsoft.Data.SqlClient` - ADO.NET provider for SQL Server
- `Microsoft.Extensions.Configuration` - Configuration system
- `Microsoft.Extensions.Configuration.Json` - JSON configuration provider
- `Microsoft.Extensions.Configuration.UserSecrets` - User secrets support
- `Microsoft.Extensions.Configuration.EnvironmentVariables` - Environment variable support

### Step 2: Configure User Secrets

**Initialize User Secrets** (if not already done):

The `.csproj` already contains `<UserSecretsId>dbdemo-library-app-2024</UserSecretsId>`, so secrets are ready to use.

**Set your passwords**:

```bash
# Admin (SA) connection string
dotnet user-secrets set "ConnectionStrings:SqlServerAdmin" "Server=localhost,1453;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"

# App user connection string
dotnet user-secrets set "ConnectionStrings:LibraryDb" "Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;Password=LibraryApp@2024!;TrustServerCertificate=True;"
```

**Verify secrets were saved**:

```bash
dotnet user-secrets list
```

Expected output:
```
ConnectionStrings:SqlServerAdmin = Server=localhost,1453;User Id=sa;Password=********...
ConnectionStrings:LibraryDb = Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;Password=********...
```

### Step 3: Run the Application


Run from within the project DbDemo folder, not root (solution folder)
```bash
dotnet run
```

**Expected output**:

```
===========================================
Library Management System - ADO.NET Demo
===========================================

✅ Configuration loaded (Environment: Development)

📋 Configuration Summary:
─────────────────────────────────────────

🔌 Connection Strings:
   Admin (SA): Server=localhost,1453; Password=********; TrustServerCertificate=True
   App User:   Server=localhost,1453; Database=LibraryDb; User Id=library_app_user; Password=********; TrustServerCertificate=True

⚙️  Database Settings:
   Migrations Path: ../../../migrations
   Command Timeout: 30s
   Retry Enabled:   True

🔒 Security Check:
   ✅ Admin password configured
   ✅ App user password configured
─────────────────────────────────────────

Press any key to exit...
```

## 🔐 Security Best Practices Demonstrated

### 1. Never Hardcode Passwords

**❌ Bad**:
```csharp
var connectionString = "Server=localhost;User Id=sa;Password=MyPassword123;";
```

**✅ Good**:
```csharp
var connectionString = configuration.GetConnectionString("LibraryDb");
```

### 2. Use User Secrets in Development

```bash
# Secrets stored outside project folder
dotnet user-secrets set "ConnectionStrings:LibraryDb" "Server=...;Password=...;"
```

### 3. Use Environment Variables in Production

```bash
# In production (Docker, Azure, AWS, etc.)
export ConnectionStrings__LibraryDb="Server=...;Password=...;"
```

Note the **double underscore** `__` which represents the `:` separator in environment variables.

### 4. Don't Log Passwords

Notice how `Program.cs` has a `MaskPassword()` method:

```csharp
private static string MaskPassword(string? connectionString)
{
    // Replaces Password=XYZ with Password=********
}
```

Always mask passwords when displaying connection strings!

### 5. .gitignore Secrets

Our `.gitignore` already includes:

```gitignore
.env
*.env.local
**/secrets.json
```

This prevents accidentally committing secrets.

## ⚙️ Configuration File Deep Dive

### appsettings.json

```json
{
  "ConnectionStrings": {
    "SqlServerAdmin": "Server=localhost,1453;TrustServerCertificate=True;",
    "LibraryDb": "Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;TrustServerCertificate=True;"
  }
}
```

**Notice**: Passwords are intentionally missing! They come from User Secrets or Environment Variables.

### Reading Configuration in Code

```csharp
// Load configuration
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddUserSecrets<Program>()
    .Build();

// Get a connection string
var connStr = configuration.GetConnectionString("LibraryDb");

// Get a nested value
var timeout = configuration["Database:CommandTimeout"];

// Get with type conversion
var timeout = configuration.GetValue<int>("Database:CommandTimeout");
```

## 🧪 Testing Your Configuration

### Test 1: Verify Secrets Are Loaded

Run the application and check the output:
- ✅ Both passwords should show as configured
- ✅ Connection strings should show with masked passwords

### Test 2: Verify Git Doesn't Track Secrets

```bash
git status
```

You should see:
- ✅ appsettings.json (tracked)
- ❌ secrets.json (NOT in the list - it's in a different location)

### Test 3: View Your User Secrets Location

```bash
# List all secrets
dotnet user-secrets list

# See where they're stored
dotnet user-secrets list --verbose
```

## ⚠️ Common Pitfalls

### 1. Forgetting to Set User Secrets

**Error**: "Connection string has no password"

**Solution**: Run `dotnet user-secrets set` commands from Step 2

### 2. Wrong Directory for User Secrets Commands

**Error**: "Could not find UserSecretsId"

**Solution**: Make sure you're in the `DbDemo/DbDemo/` folder (where `.csproj` is), not the solution root

### 3. Committing Secrets to Git

**How to check**:
```bash
git status
```

If you see `secrets.json` or `.env` files, **STOP! Don't commit!**

**Fix**:
```bash
git reset secrets.json  # Remove from staging
```

### 4. Connection String Format Errors

Common mistakes:
- Missing semicolons: `Server=localhost,1453Database=LibraryDb` (wrong!)
- Wrong separator: `Server=localhost:1453` (should be comma for port)
- Missing quotes in bash: Use `"..."` around the whole connection string

### 5. Environment Not Set

If `DOTNET_ENVIRONMENT` is not set, it defaults to "Development".

**Set environment**:
```bash
# Windows (PowerShell)
$env:DOTNET_ENVIRONMENT = "Production"

# Mac/Linux
export DOTNET_ENVIRONMENT=Production
```

## ✅ Checklist

Before moving to the next commit, ensure:

- [ ] Project builds: `dotnet build`
- [ ] Application runs: `dotnet run`
- [ ] User secrets configured: `dotnet user-secrets list` shows both connection strings
- [ ] Passwords are masked in output
- [ ] No secrets in Git: `git status` doesn't show secrets.json
- [ ] Docker SQL Server is running: `docker compose ps`

## 🔗 Learn More

### Official Microsoft Documentation

- [Configuration in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration) - Complete guide
- [User Secrets in Development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) - How to use user secrets
- [Connection Strings](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/connection-strings) - Format and best practices
- [NuGet Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/overview-and-workflow) - How packages work

### Configuration Providers

- [JSON Configuration Provider](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#json-configuration-provider)
- [Environment Variables Provider](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#environment-variable-configuration-provider)
- [Command-line Provider](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers#command-line-configuration-provider)

### Security

- [Secret Management in .NET](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault for Production](https://learn.microsoft.com/en-us/azure/key-vault/general/overview)
- [AWS Secrets Manager](https://aws.amazon.com/secrets-manager/)
- [OWASP Secrets Management](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)

### Video Tutorials

- [Configuration in .NET - IAmTimCorey](https://www.youtube.com/watch?v=GAOCe-2nXqc)
- [User Secrets - dotnet](https://www.youtube.com/watch?v=PkLLP2tcd28)

## ❓ Discussion Questions

1. **Why use multiple configuration sources instead of just one file?**
   - Think about different environments (dev, test, production)
   - Think about team collaboration (different developers)

2. **What would happen if you committed passwords to Git?**
   - Could someone else find them in Git history?
   - What if the repository becomes public?

3. **Why separate Admin and App user connection strings?**
   - What damage could happen if the app used SA account?
   - How does this relate to the "principle of least privilege"?

4. **In production, where would you store database passwords?**
   - Research: Azure Key Vault, AWS Secrets Manager, HashiCorp Vault
   - How do managed identities eliminate passwords entirely?

5. **What's the difference between User Secrets and Environment Variables?**
   - When would you use each one?
   - Can you use both at the same time?

## 🎯 Next Steps

Now that configuration is set up, you're ready for:

1. **Commit 3**: Define domain entities (Book, Author, Member, Loan)
2. Create the `Models/` folder structure
3. Start building the core business logic

The configuration system we set up here will be used throughout the project to:
- Connect to the database
- Run migrations
- Configure application behavior

**Great job! Your project foundation is solid! 🚀**
