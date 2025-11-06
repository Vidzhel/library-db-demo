#!/bin/bash

# Script to regenerate the TypeScript API client from Swagger
# Run this whenever the API changes

echo "🔄 Regenerating API client from Swagger..."

# Check if the API is running
if ! curl -s http://localhost:5000/swagger/v1/swagger.json > /dev/null; then
    echo "❌ Error: API is not running at http://localhost:5000"
    echo "Please start the API first:"
    echo "  cd ../DbDemo.WebApi && dotnet run"
    exit 1
fi

# Remove old API client
echo "🗑️  Removing old API client..."
rm -rf ./src/api

# Generate new API client
echo "📦 Generating new API client..."
npx swagger-typescript-api generate \
    -p http://localhost:5000/swagger/v1/swagger.json \
    -o ./src/api \
    -n Api.ts \
    --axios

if [ $? -eq 0 ]; then
    echo "✅ API client generated successfully!"
    echo "📁 Location: ./src/api/Api.ts"
else
    echo "❌ Failed to generate API client"
    exit 1
fi
