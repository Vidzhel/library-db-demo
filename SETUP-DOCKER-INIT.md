# Docker Initialization Scripts Setup

## 📋 What This Does

These initialization scripts automatically create a dedicated application user when the SQL Server container first starts, following the **Principle of Least Privilege** (don't use SA for applications!).

## 🚀 Quick Setup

### Option 1: Manual Copy (Recommended for Learning)

1. **Copy the init scripts** to the Docker init directory:

```bash
# Copy SQL script
cp 01-create-app-user.sql scripts/init/

# Copy shell script (makes it executable)
cp 01-create-app-user.sh scripts/init/
chmod +x scripts/init/01-create-app-user.sh
```

2. **Start Docker** (or restart if already running):

```bash
# If container is already running, recreate it to run init scripts
docker compose down
docker compose up -d
```

3. **Verify** the user was created:

```bash
# Check container logs
docker compose logs sqlserver | grep "Application user"

# Or connect and check manually
docker exec -it dbdemo-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U library_app_user -P 'LibraryApp@2024!' -Q "SELECT DB_NAME()"
```

### Option 2: Use Migration Runner (Recommended for Production-like Setup)

Instead of Docker init scripts, you can run `V000__create_app_user.sql` through our migration runner. This approach:

- ✅ Is more visible (you see exactly when it runs)
- ✅ Works without Docker
- ✅ Matches production deployment patterns
- ✅ Integrates with your migration system

**We'll demonstrate this approach in the migrations chapter!**

## 📁 Files Explanation

### `01-create-app-user.sql`
- **What**: SQL script that creates the database, login, user, and grants permissions
- **When**: Runs automatically when SQL Server container **first starts**
- **Note**: If the database already exists, it won't recreate it (idempotent)

### `01-create-app-user.sh`
- **What**: Shell wrapper script (optional, for better logging)
- **When**: Runs before the SQL script
- **Purpose**: Waits for SQL Server to be ready, then executes the SQL

## 🔒 Security Details

### What Gets Created

**Database**: `LibraryDb`
**Login** (server-level): `library_app_user`
**User** (database-level): `library_app_user` in `LibraryDb`

### Permissions Granted

| Permission | What It Allows | Why Needed |
|------------|----------------|------------|
| `db_datareader` | SELECT from all tables | Read operations |
| `db_datawriter` | INSERT, UPDATE, DELETE | Write operations |
| `db_ddladmin` | CREATE, ALTER, DROP tables | Run migrations |
| `EXECUTE` | Run stored procedures/functions | Call procedures |
| `VIEW DEFINITION` | See object definitions | Debugging/development |

### What App User CANNOT Do

❌ Drop the database
❌ Modify server configuration
❌ Create/delete other logins
❌ Access other databases
❌ Grant permissions to other users

## 🔑 Connection Strings

### For SA (Admin tasks only!)
```
Server=localhost,1453;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
```

### For Application (Normal operations)
```
Server=localhost,1453;Database=LibraryDb;User Id=library_app_user;Password=LibraryApp@2024!;TrustServerCertificate=True;
```

## 🧪 Testing the Setup

### Test 1: Connect with App User

```bash
docker exec -it dbdemo-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U library_app_user -P 'LibraryApp@2024!' -d LibraryDb
```

Once connected, try:

```sql
-- This should work (read permission)
SELECT name FROM sys.tables;
GO

-- This should work (create permission for migrations)
CREATE TABLE Test (Id INT PRIMARY KEY, Name NVARCHAR(100));
GO

-- This should work (write permission)
INSERT INTO Test VALUES (1, 'Hello');
SELECT * FROM Test;
GO

-- This should FAIL (cannot drop database)
DROP DATABASE LibraryDb;
GO
```

### Test 2: Verify Security

Try to do something that should fail:

```bash
# Try to connect to master database (should fail or show no tables)
docker exec -it dbdemo-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U library_app_user -P 'LibraryApp@2024!' -d master -Q "SELECT * FROM sys.databases"
```

## ⚠️ Important Notes

### 1. Init Scripts Run Only Once

Docker init scripts run **only when the container is created for the first time**. If you:

- Stop/start the container → Scripts DON'T run again
- Remove/recreate the container BUT keep volumes → Scripts DON'T run (database exists)
- Remove container AND volumes → Scripts run again (fresh start)

**To re-run init scripts:**

```bash
# Delete everything including volumes (⚠️ DELETES ALL DATA!)
docker compose down -v

# Recreate from scratch
docker compose up -d
```

### 2. Change Password in Production!

The password `LibraryApp@2024!` is for **development/learning only**. In production:

- Use strong, randomly generated passwords
- Store in Azure Key Vault, AWS Secrets Manager, etc.
- Rotate passwords regularly
- Use managed identities when possible (Azure SQL, AWS RDS)

### 3. Different Approach for Production

In production, you typically:

1. Create users manually or via infrastructure-as-code (Terraform, ARM templates)
2. Use deployment pipelines with service principals
3. NOT use init scripts in containers
4. Separate migration execution from application deployment

## 🎯 Next Steps

After setting up the app user, you'll:

1. Create the .NET project (Commit 2)
2. Configure **two connection strings** in `appsettings.json`
3. Use SA connection for Migration V000 (creates user)
4. Use app user connection for all subsequent operations
5. Build the migration runner

## 📚 Learn More

- [SQL Server Security Best Practices](https://learn.microsoft.com/en-us/sql/relational-databases/security/security-center-for-sql-server-database-engine-and-azure-sql-database)
- [Logins vs Users](https://learn.microsoft.com/en-us/sql/relational-databases/security/authentication-access/create-a-login)
- [Fixed Database Roles](https://learn.microsoft.com/en-us/sql/relational-databases/security/authentication-access/database-level-roles)
- [Principle of Least Privilege](https://en.wikipedia.org/wiki/Principle_of_least_privilege)

## ❓ Discussion Questions

1. **Why do we need both a LOGIN and a USER?**
   - Hint: Think server-level vs database-level

2. **What happens if the app user tries to drop a database?**
   - Try it! See what error you get

3. **Why grant db_ddladmin to an app user?**
   - This seems powerful - when would you remove this permission?

4. **How would you create a read-only user for reporting?**
   - Which roles would you grant?

---

**Ready to proceed?** Once you've set up the app user, you're ready for the .NET project setup in Commit 2!
