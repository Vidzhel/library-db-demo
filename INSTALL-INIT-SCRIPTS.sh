#!/bin/bash
# =============================================
# Quick Installer for Docker Init Scripts
# =============================================
# This script copies the initialization files to the correct location
# Run this once before starting Docker for the first time
# =============================================

set -e  # Exit on error

echo "📦 Installing Docker initialization scripts..."
echo ""

# Check if scripts/init directory exists
if [ ! -d "scripts/init" ]; then
    echo "❌ Error: scripts/init directory not found"
    echo "   Are you running this from the project root?"
    exit 1
fi

# Copy SQL script
echo "📄 Copying SQL initialization script..."
cp -v 01-create-app-user.sql scripts/init/

# Copy shell script
echo "📄 Copying shell initialization script..."
cp -v 01-create-app-user.sh scripts/init/

# Make shell script executable
echo "🔧 Making shell script executable..."
chmod +x scripts/init/01-create-app-user.sh

echo ""
echo "✅ Installation complete!"
echo ""
echo "📋 Next steps:"
echo "   1. Start Docker: docker compose up -d"
echo "   2. Wait for initialization (check logs: docker compose logs -f sqlserver)"
echo "   3. Verify user was created:"
echo "      docker exec -it dbdemo-sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U library_app_user -P 'LibraryApp@2024!' -Q 'SELECT DB_NAME()'"
echo ""
echo "📚 For more details, see SETUP-DOCKER-INIT.md"
echo ""
