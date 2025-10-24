# 00 - Docker Setup for SQL Server

## 📖 What You'll Learn

- What Docker is and why we use it for database development
- The difference between containers and virtual machines
- How to set up SQL Server in a Docker container
- Basic Docker Compose commands for managing your database
- How to connect to SQL Server running in Docker

## 🎯 Why This Matters

In modern software development, **Docker** has become the standard way to run databases and other services during
development. Here's why:

- **Consistency**: Everyone on the team uses the exact same database version
- **Isolation**: The database runs in its own isolated environment
- **Easy Setup**: No complex SQL Server installation on your machine
- **Clean Removal**: Delete the container when you're done, no leftover files
- **Version Control**: Database configuration is stored in `docker compose.yml`

## 🔍 Key Concepts

### What is Docker?

**Docker** is a platform that allows you to run applications in **containers**. Think of a container as a lightweight,
standalone package that includes everything needed to run a piece of software.

#### Container vs Virtual Machine

| Container                 | Virtual Machine       |
|---------------------------|-----------------------|
| Shares the host OS kernel | Has its own full OS   |
| Starts in seconds         | Takes minutes to boot |
| Uses minimal resources    | Resource-intensive    |
| Lightweight (MBs)         | Heavy (GBs)           |

**Visual Analogy**:

- Virtual Machine = Entire apartment building with full infrastructure
- Container = Single apartment with shared utilities

### What is Docker Compose?

**Docker Compose** is a tool for defining and running multi-container applications using a YAML configuration file. For
our project, we use it to:

1. Define the SQL Server service
2. Configure environment variables (like passwords)
3. Set up volume mounts for data persistence
4. Configure networking

### Our Docker Compose Configuration

The `docker compose.yml` file contains:

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
```

**What this means**:

- `image`: The pre-built SQL Server 2022 image from Microsoft Container Registry
- `container_name`: A friendly name for our container
- `environment`: Configuration variables (EULA acceptance, password, edition)
- `ports`: Maps container port 1433 to host port 1453 (because 1433 may be already used by local sql servier installation - one app per port mapping)
- `volumes`: Persists data so it survives container restarts
- `healthcheck`: Verifies SQL Server is ready to accept connections

## 🚀 Getting Started

### Prerequisites

1. **Install Docker Desktop** (includes Docker and Docker Compose):
    - **Windows**: [Docker Desktop for Windows](https://docs.docker.com/desktop/install/windows-install/)
    - **Mac**: [Docker Desktop for Mac](https://docs.docker.com/desktop/install/mac-install/)
    - **Linux
      **: [Docker Engine](https://docs.docker.com/engine/install/) + [Docker Compose](https://docs.docker.com/compose/install/)

2. **Verify Installation**:
   ```bash
   docker --version
   docker compose --version
   ```

### Step-by-Step Setup

#### Step 1: Configure Password

1. Copy the example environment file:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` and set a strong password:
   ```
   SA_PASSWORD=YourStrong@Passw0rd
   ```

   **Password Requirements** (SQL Server enforces these):
    - At least 8 characters
    - Contains uppercase letters (A-Z)
    - Contains lowercase letters (a-z)
    - Contains numbers (0-9)
    - Contains symbols (!@#$%^&*)

#### Step 2: Start SQL Server

Run from the project root directory:

```bash
docker compose up -d
```

**What happens**:

- `up`: Starts the services defined in docker compose.yml
- `-d`: Runs in detached mode (background)

**Output you'll see**:

```
Creating network "dbdemo-network" ... done
Creating volume "dbdemo_sqlserver-data" ... done
Creating dbdemo-sqlserver ... done
```

#### Step 3: Verify SQL Server is Running

Check container status:

```bash
docker compose ps
```

**Expected output**:

```
Name                  Command              State           Ports
--------------------------------------------------------------------------------
dbdemo-sqlserver   /opt/mssql/bin/sqlservr   Up (healthy)   0.0.0.0:1453->1433/tcp
```

**Note the "healthy" status** - this means the healthcheck passed and SQL Server is ready.

#### Step 4: View Logs (Optional)

See what's happening inside the container:

```bash
docker compose logs -f sqlserver
```

Press `Ctrl+C` to stop following logs.

### Connecting to SQL Server

**Connection String**:

```
Server=localhost,1453;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
```

** Using JetBrains IDEs**

- Authentication: User & Password
- Host: `localhost`
- Port: `1453`
- User: `sa`
- Password: (your password from .env)
**(For Rider, you can use connection string option and SQL Server and paste the above connection string)** 

**Using SQL Server Management Studio (SSMS)**:

- Server name: `localhost,1453` or just `localhost`
- Authentication: SQL Server Authentication
- Login: `sa`
- Password: (your password from .env)

**Using Azure Data Studio**:

- Connection type: Microsoft SQL Server
- Server: `localhost,1453`
- Authentication type: SQL Login
- User name: `sa`
- Password: (your password from .env)

**Using command-line (sqlcmd)**:

```bash
docker exec -it dbdemo-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Passw0rd'
```

## 🔧 Essential Docker Commands

### Starting and Stopping

```bash
# Start containers (in background)
docker compose up -d

# Stop containers (keeps data)
docker compose stop

# Stop and remove containers (keeps data in volumes)
docker compose down

# Stop and remove everything including volumes (⚠️ DELETES DATA)
docker compose down -v
```

### Viewing Status and Logs

```bash
# Check if containers are running
docker compose ps

# View logs
docker compose logs sqlserver

# Follow logs in real-time
docker compose logs -f sqlserver

# View last 50 lines of logs
docker compose logs --tail=50 sqlserver
```

### Executing Commands in Container

```bash
# Open bash shell in SQL Server container
docker exec -it dbdemo-sqlserver bash

# Run sqlcmd directly
docker exec -it dbdemo-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourPassword'
```

### Inspecting and Cleaning

```bash
# View container details
docker inspect dbdemo-sqlserver

# View volume details
docker volume inspect dbdemo_sqlserver-data

# Remove stopped containers
docker compose rm

# View disk usage
docker system df
```

## 💾 Data Persistence

### How Data Persistence Works

The `docker compose.yml` defines a **named volume**:

```yaml
volumes:
  - sqlserver-data:/var/opt/mssql
```

This means:

- Database files are stored in a Docker volume named `dbdemo_sqlserver-data`
- Data survives when you stop or remove the container
- Data is only deleted if you explicitly remove the volume (`docker compose down -v`)

### Where is My Data?

Docker stores volumes in a location managed by Docker:

- **Windows**: `\\wsl$\docker-desktop-data\data\docker\volumes\`
- **Mac**: `~/Library/Containers/com.docker.docker/Data/`
- **Linux**: `/var/lib/docker/volumes/`

**You don't need to access this directly** - Docker manages it for you.

### Backup and Restore

**Backup** (export volume to tar file):

```bash
docker run --rm -v dbdemo_sqlserver-data:/data -v $(pwd):/backup ubuntu tar czf /backup/sqlserver-backup.tar.gz -C /data .
```

**Restore** (import tar file to volume):

```bash
docker run --rm -v dbdemo_sqlserver-data:/data -v $(pwd):/backup ubuntu tar xzf /backup/sqlserver-backup.tar.gz -C /data
```

## ⚠️ Common Pitfalls

### 1. Password Doesn't Meet Requirements

**Error**: `ERROR: Unable to set system administrator password: Password validation failed.`

**Solution**: Make sure your password in `.env` meets SQL Server requirements (8+ chars, mixed case, numbers, symbols).

### 2. Port 1453 Already in Use

**Error**: `Bind for 0.0.0.0:1453 failed: port is already allocated`

**Solution**:

- Another SQL Server instance is running on port 1453
- Stop the local SQL Server service, or
- Change the port in `docker compose.yml`:
  ```yaml
  ports:
    - "1464:1433"  # Use port 1464 on host instead
  ```

### 3. Container Keeps Restarting

**Check logs**:

```bash
docker compose logs sqlserver
```

**Common causes**:

- Invalid password
- Insufficient memory allocated to Docker
- Corrupted volume data

**Solution**: Remove and recreate:

```bash
docker compose down -v
docker compose up -d
```

### 4. "No such file or directory" for .env

**Error**: You forgot to create `.env` from `.env.example`

**Solution**:

```bash
cp .env.example .env
# Edit .env with your password
```

## 🔒 Security: Principle of Least Privilege

### Why Not Use SA Account for Applications?

The **SA (System Administrator)** account has **unlimited permissions** on the SQL Server instance. Using SA for your application is like giving your house keys to everyone - it's a security risk!

**Problems with using SA**:
- ❌ Can drop any database (accidental or malicious)
- ❌ Can modify server configuration
- ❌ Can create/delete other logins
- ❌ Makes auditing impossible (who did what?)
- ❌ No way to limit blast radius of a security breach

### The Right Way: Dedicated Application User

We'll create a **dedicated database user** with only the permissions needed:

```
✅ Read data (db_datareader)
✅ Write data (db_datawriter)
✅ Execute stored procedures (EXECUTE)
✅ Create/alter tables for migrations (db_ddladmin)
❌ Cannot drop databases
❌ Cannot modify server settings
❌ Cannot access other databases
❌ Cannot create logins
```

### When Will We Create the App User?

Instead of using Docker init scripts (less visible for learning), we'll create the application user in **Migration V000__create_app_user.sql**.

This way:
1. You can see exactly what permissions are granted
2. The setup is version-controlled with your code
3. It's portable (works without Docker)
4. You understand the security model

**You'll see this in action when we get to the migrations phase!**

📘 **For detailed setup instructions**, see: [`SETUP-DOCKER-INIT.md`](../SETUP-DOCKER-INIT.md) in the project root.

### Connection String Strategy

We'll use **two connection strings**:

1. **SA connection** (admin tasks only):
   - Creating the database
   - Creating the application user
   - Running migrations that create users/logins

2. **App user connection** (normal operations):
   - All CRUD operations
   - Running most migrations
   - Production-like security

## ✅ Best Practices

### 1. Never Commit .env File

The `.env` file contains passwords and should **never** be committed to Git. It's already in `.gitignore`.

**Do commit**: `.env.example` (template without real passwords)

### 2. Use Strong Passwords

Even in development, use strong passwords to build good security habits.

**Why this matters**: Many breaches happen because developers use weak passwords in dev environments, then forget to change them in production.

### 3. Stop Containers When Not in Use

Save system resources:

```bash
docker compose stop
```

Restart when needed:

```bash
docker compose start
```

### 4. Regular Cleanup

Remove unused Docker resources:

```bash
# See what's taking space
docker system df

# Clean up stopped containers, unused networks, dangling images
docker system prune

# Clean up everything (⚠️ careful!)
docker system prune -a --volumes
```

### 5. Separate Admin and Application Credentials

**Never use SA/admin accounts in application connection strings!**

- SA is for setup and maintenance only
- Applications should use dedicated users with minimal permissions
- This limits damage from SQL injection or compromised credentials

## 🔗 Learn More

### Official Docker Documentation

- [Docker Overview](https://docs.docker.com/get-started/overview/) - What is Docker?
- [Docker Compose Documentation](https://docs.docker.com/compose/) - Complete guide
- [Dockerfile Reference](https://docs.docker.com/engine/reference/builder/) - For creating custom images
- [Docker CLI Reference](https://docs.docker.com/engine/reference/commandline/cli/) - All commands

### SQL Server in Docker

- [SQL Server Docker Images](https://hub.docker.com/_/microsoft-mssql-server) - Official Microsoft images
- [SQL Server on Linux](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-overview) - How SQL Server runs on
  Linux/Docker
- [Configure SQL Server in Docker](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-docker-container-configure) -
  Advanced configuration

### Docker Compose

- [Compose file version 3 reference](https://docs.docker.com/compose/compose-file/compose-file-v3/) - YAML syntax
- [Environment variables in Compose](https://docs.docker.com/compose/environment-variables/) - Using .env files
- [Networking in Compose](https://docs.docker.com/compose/networking/) - How containers communicate

### Video Tutorials

- [Docker Tutorial for Beginners - TechWorld with Nana](https://www.youtube.com/watch?v=3c-iBn73dDE)
- [SQL Server in Docker - kudvenkat](https://www.youtube.com/watch?v=1HI0HgERyVs)
- [Docker Compose Tutorial - Programming with Mosh](https://www.youtube.com/watch?v=HG6yIjZapSA)

### Interactive Learning

- [Play with Docker](https://labs.play-with-docker.com/) - Free online Docker playground
- [Docker 101 Tutorial](https://www.docker.com/101-tutorial/) - Interactive tutorial

## ❓ Discussion Questions

1. **Why use Docker instead of installing SQL Server directly?**
    - Think about team collaboration, environment consistency, and cleanup

2. **What happens to your data when you run `docker compose down`?**
    - Understand the difference between containers and volumes

3. **When would you use `docker compose up` vs `docker compose start`?**
    - First time setup vs restarting existing containers

4. **How would you run two different SQL Server versions simultaneously?**
    - Hint: Think about ports and container names

5. **What are the security implications of using SA account?**
    - Research principle of least privilege

## 🎯 Next Steps

Now that you have SQL Server running in Docker, you're ready to:

1. **Create the .NET project** (Commit 2)
2. **Connect to the database** from C# code
3. **Run migrations** to create tables
4. **Start building** the library management system

Remember: You can always check if your SQL Server is running with:

```bash
docker compose ps
```

And view logs if something goes wrong:

```bash
docker compose logs sqlserver
```

**Happy coding! 🚀**
