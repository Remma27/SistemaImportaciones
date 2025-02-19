Remove-Item -Recurse -Force .\bin
Remove-Item -Recurse -Force .\obj
dotnet clean
dotnet restore
dotnet build -f net8.0
