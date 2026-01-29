param(
    [string]$Configuration = "Debug"
)

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

$solutionPath = Join-Path $repoRoot "ProjectTFDB.sln"
$projectPath = Join-Path $repoRoot "ProjectTFDB\\ProjectTFDB.csproj"
if (!(Test-Path $projectPath)) {
    $projectPath = Join-Path $repoRoot "docs\\Prototypes\\Prototype1\\Prototype1\\ProjectTFDB.csproj"
}
if (!(Test-Path $projectPath)) {
    throw "Project file not found."
}

if (Test-Path $solutionPath) {
    dotnet test $solutionPath -c $Configuration
} else {
    dotnet test $projectPath -c $Configuration
}
