# Clean build outputs
Remove-Item -Recurse -Force .\API\bin
Remove-Item -Recurse -Force .\API\obj

# Clean the solution (optional)
dotnet clean

# Restore packages for the API project only
dotnet restore "API/API.csproj"

# Build the API project explicitly for net8.0
dotnet build "API/API.csproj" -f net8.0
