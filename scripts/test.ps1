param(
    [string]$Configuration = "Debug"
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

dotnet test .\ProjectTFDB.sln -c $Configuration
