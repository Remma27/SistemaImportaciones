Remove-Item -Recurse -Force .\bin
Remove-Item -Recurse -Force .\obj\Debug
Remove-Item -Recurse -Force .\obj\Release
dotnet clean
dotnet build
