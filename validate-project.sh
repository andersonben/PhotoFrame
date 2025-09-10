#!/bin/bash

# PhotoFrame Project Validation Script
# Checks that all components are properly configured

set -e

echo "🔍 Validating PhotoFrame project structure..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Validation functions
validate_file() {
    if [ -f "$1" ]; then
        echo -e "${GREEN}✅${NC} $1"
        return 0
    else
        echo -e "${RED}❌${NC} $1 (missing)"
        return 1
    fi
}

validate_directory() {
    if [ -d "$1" ]; then
        echo -e "${GREEN}✅${NC} $1/"
        return 0
    else
        echo -e "${RED}❌${NC} $1/ (missing)"
        return 1
    fi
}

check_project_file() {
    local project_file="$1"
    local expected_framework="$2"
    
    if [ -f "$project_file" ]; then
        if grep -q "$expected_framework" "$project_file"; then
            echo -e "${GREEN}✅${NC} $project_file (${expected_framework})"
        else
            echo -e "${YELLOW}⚠️${NC} $project_file (framework version check failed)"
        fi
    else
        echo -e "${RED}❌${NC} $project_file (missing)"
        return 1
    fi
}

# Start validation
echo ""
echo "📁 Project Structure:"

# Root files
validate_file "PhotoFrame.sln"
validate_file "README.md"
validate_file "LICENSE"
validate_file ".gitignore"
validate_file "deploy-to-pi.sh"

echo ""
echo "📦 PhotoFrame.Data Project:"
validate_directory "PhotoFrame.Data"
check_project_file "PhotoFrame.Data/PhotoFrame.Data.csproj" "net8.0"
validate_file "PhotoFrame.Data/PhotoFrameDbContext.cs"
validate_directory "PhotoFrame.Data/Entities"
validate_file "PhotoFrame.Data/Entities/Photo.cs"
validate_file "PhotoFrame.Data/Entities/Setting.cs"
validate_directory "PhotoFrame.Data/Migrations"

echo ""
echo "🌐 PhotoFrame.Web Project:"
validate_directory "PhotoFrame.Web"
check_project_file "PhotoFrame.Web/PhotoFrame.Web.csproj" "net8.0"
validate_file "PhotoFrame.Web/Program.cs"
validate_directory "PhotoFrame.Web/Controllers"
validate_file "PhotoFrame.Web/Controllers/PhotosController.cs"
validate_directory "PhotoFrame.Web/Services"
validate_file "PhotoFrame.Web/Services/ImageProcessingService.cs"
validate_file "PhotoFrame.Web/Services/ImageProcessingTestService.cs"
validate_directory "PhotoFrame.Web/Models"
validate_file "PhotoFrame.Web/Models/PhotoUploadViewModel.cs"
validate_directory "PhotoFrame.Web/Views/Photos"
validate_file "PhotoFrame.Web/Views/Photos/Upload.cshtml"
validate_file "PhotoFrame.Web/Views/Photos/Index.cshtml"

echo ""
echo "🔧 PhotoFrame.Service Project:"
validate_directory "PhotoFrame.Service"
check_project_file "PhotoFrame.Service/PhotoFrame.Service.csproj" "net8.0"
validate_file "PhotoFrame.Service/Program.cs"
validate_file "PhotoFrame.Service/appsettings.json"
validate_file "PhotoFrame.Service/photoframe.service"
validate_directory "PhotoFrame.Service/Services"
validate_file "PhotoFrame.Service/Services/EInkDisplayService.cs"
validate_file "PhotoFrame.Service/Services/WaveshareEInkDisplayService.cs"
validate_file "PhotoFrame.Service/Services/PhotoDisplayService.cs"
validate_directory "PhotoFrame.Service/Configuration"
validate_file "PhotoFrame.Service/Configuration/DisplaySettings.cs"

echo ""
echo "🔍 Configuration Validation:"

# Check key configuration files
echo "Checking appsettings.json..."
if grep -q "DisplaySettings" PhotoFrame.Service/appsettings.json; then
    echo -e "${GREEN}✅${NC} DisplaySettings section found"
else
    echo -e "${RED}❌${NC} DisplaySettings section missing"
fi

if grep -q "1872" PhotoFrame.Service/appsettings.json; then
    echo -e "${GREEN}✅${NC} Screen width configured"
else
    echo -e "${RED}❌${NC} Screen width not configured"
fi

if grep -q "1404" PhotoFrame.Service/appsettings.json; then
    echo -e "${GREEN}✅${NC} Screen height configured"
else
    echo -e "${RED}❌${NC} Screen height not configured"
fi

echo ""
echo "📋 Package Dependencies Check:"

# Check for key packages in project files
echo "PhotoFrame.Data packages:"
if grep -q "Microsoft.EntityFrameworkCore.Sqlite" PhotoFrame.Data/PhotoFrame.Data.csproj; then
    echo -e "${GREEN}✅${NC} Entity Framework SQLite"
else
    echo -e "${RED}❌${NC} Entity Framework SQLite missing"
fi

echo "PhotoFrame.Web packages:"
if grep -q "SixLabors.ImageSharp" PhotoFrame.Web/PhotoFrame.Web.csproj; then
    echo -e "${GREEN}✅${NC} ImageSharp for image processing"
else
    echo -e "${RED}❌${NC} ImageSharp missing"
fi

echo "PhotoFrame.Service packages:"
if grep -q "System.Device.Gpio" PhotoFrame.Service/PhotoFrame.Service.csproj; then
    echo -e "${GREEN}✅${NC} GPIO support"
else
    echo -e "${RED}❌${NC} GPIO support missing"
fi

if grep -q "Microsoft.Extensions.Hosting" PhotoFrame.Service/PhotoFrame.Service.csproj; then
    echo -e "${GREEN}✅${NC} Hosting extensions"
else
    echo -e "${RED}❌${NC} Hosting extensions missing"
fi

echo ""
echo "🎯 Target Framework Check:"
if grep -q "linux-arm64" PhotoFrame.Service/PhotoFrame.Service.csproj; then
    echo -e "${GREEN}✅${NC} ARM64 runtime configured for service"
else
    echo -e "${YELLOW}⚠️${NC} ARM64 runtime not configured for service"
fi

echo ""
echo "🔐 Security & Permissions:"
if [ -x "deploy-to-pi.sh" ]; then
    echo -e "${GREEN}✅${NC} Deployment script is executable"
else
    echo -e "${YELLOW}⚠️${NC} Deployment script not executable (run: chmod +x deploy-to-pi.sh)"
fi

echo ""
echo "📊 Summary:"
echo "Project validation completed!"
echo ""
echo "🚀 Next steps:"
echo "1. Install .NET 8 SDK if not already installed"
echo "2. Run 'dotnet restore' to restore packages"
echo "3. Run 'dotnet build' to build all projects"
echo "4. Use './deploy-to-pi.sh' to deploy to Raspberry Pi"
echo ""
echo "📖 For detailed instructions, see README.md"