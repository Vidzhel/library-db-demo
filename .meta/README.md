# Database Docker Setup

This directory contains the Docker configuration for the SQL Server database used in the DbDemo project.

## Directory Structure

```
.meta/
├── Dockerfile              # Custom SQL Server image with initialization scripts
├── docker-compose.yml      # Docker Compose configuration
├── docker/
│   ├── entrypoint.sh       # Container entrypoint script
│   └── configure-db.sh     # Database initialization script
└── README.md               # This file
```

## Quick Start

### Initialize the Database

Run the helper script from the project root:

```bash
./.meta/db-init.sh
```

This script will:
1. Check if Docker is running
2. Stop and remove any existing container (for fresh initialization)
3. Build the custom SQL Server image
4. Start the container
5. Initialize the database with the V001 migration schema

### Manual Docker Commands

If you prefer to use docker-compose directly:

```bash
# Build and start the container
cd .meta
docker-compose up --build -d

# View logs
docker-compose logs -f

# Stop the container
docker-compose down

# Restart the container
docker-compose restart
```

## Connection Details

Once initialized, connect to the database using:

- **Host**: `localhost`
- **Port**: `1453`
- **Database**: `LibraryDb`
- **Username**: `sa`
- **Password**: `YourStrong@Passw0rd` (or set via `SA_PASSWORD` environment variable)

### Connection String Example

```
Server=localhost,1453;Database=LibraryDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;
```

## How It Works

### Automatic Initialization

The setup uses the Microsoft SQL Server Docker customization pattern:

1. **Dockerfile**: Creates a custom image based on `mcr.microsoft.com/mssql/server:2022-latest`
   - Copies initialization scripts into the container
   - Sets up a custom entrypoint

2. **entrypoint.sh**: Container startup script
   - Starts the database configuration script in the background
   - Launches SQL Server in the foreground

3. **configure-db.sh**: Database initialization script
   - Waits for SQL Server to be fully online (up to 60 seconds)
   - Drops the LibraryDb database if it exists (for clean initialization)
   - Creates a fresh LibraryDb database
   - Runs the V001__initial_schema.sql migration

### Fresh Initialization

Every time the container starts, the database is dropped and recreated with the initial schema. This ensures:
- Clean state for development
- Consistent schema across team members
- Easy testing of migrations

## Database Schema

The initial migration (`migrations/V001__initial_schema.sql`) creates:

1. **Categories** - Book categories with hierarchical support
2. **Authors** - Author information
3. **Books** - Book catalog with inventory tracking
4. **Members** - Library members
5. **Loans** - Book borrowing records
6. **BookAuthors** - Many-to-many relationship between Books and Authors

All tables include:
- Audit timestamps (CreatedAt, UpdatedAt)
- Foreign key relationships
- Business rule constraints
- Proper indexes (when V002 migration is added)

## Customization

### Change Password

Set the `SA_PASSWORD` environment variable:

```bash
export SA_PASSWORD="MySecurePassword123!"
./.meta/db-init.sh
```

Or create a `.env` file in the project root:

```
SA_PASSWORD=MySecurePassword123!
```

### Persist Data Between Restarts

By default, data is persisted in a Docker volume `sqlserver-data`. To completely reset:

```bash
docker-compose -f .meta/docker-compose.yml down -v
```

The `-v` flag removes volumes, ensuring complete cleanup.

## Troubleshooting

### Container Won't Start

1. Check Docker is running: `docker info`
2. Check port 1453 is not in use: `lsof -i :1453` (Linux/Mac) or `netstat -ano | findstr 1453` (Windows)
3. Check logs: `docker logs dbdemo-sqlserver`

### Database Not Initializing

1. View container logs: `docker logs -f dbdemo-sqlserver`
2. Look for errors in the configure-db.sh output
3. Ensure migrations/V001__initial_schema.sql exists and is valid

### Permission Errors

If you see permission errors, ensure the scripts are executable:

```bash
chmod +x .meta/db-init.sh
chmod +x .meta/docker/entrypoint.sh
chmod +x .meta/docker/configure-db.sh
```

## Development Workflow

1. Start the database: `./.meta/db-init.sh`
2. Run your application
3. Make schema changes in a new migration file
4. Update configure-db.sh to run additional migrations
5. Rebuild container: `docker-compose -f .meta/docker-compose.yml up --build -d`

## References

- [Microsoft SQL Server Docker Documentation](https://hub.docker.com/_/microsoft-mssql-server)
- [SQL Server Docker Customization Example](https://github.com/microsoft/mssql-docker/tree/master/linux/preview/examples/mssql-customize)
