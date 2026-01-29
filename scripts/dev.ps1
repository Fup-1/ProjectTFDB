param(
    [string]$Configuration = "Debug"
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

dotnet build .\ProjectTFDB.sln -c $Configuration
dotnet run --project .\ProjectTFDB\ProjectTFDB.csproj -c $Configuration
