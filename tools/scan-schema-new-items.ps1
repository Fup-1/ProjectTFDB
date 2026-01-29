param(
  # Existing (cached) schema_items.json. If missing/empty, everything in -NewSchema is treated as "new".
  [Parameter(Mandatory = $false)]
  [string] $ExistingSchema,

  # New schema_items.json to compare against the existing cache.
  [Parameter(Mandatory = $true)]
  [string] $NewSchema,

  # Optional: write a JSON file containing only the newly-added schema entries (by defindex).
  [Parameter(Mandatory = $false)]
  [string] $NewOnlyOutputPath,

  # Optional: icons folder next to NewSchema (e.g. tf2_schema_dump/icons).
  [Parameter(Mandatory = $false)]
  [string] $NewIconsDir,

  # Optional: destination icons folder (cache). Missing icons are copied by defindex filename.
  [Parameter(Mandatory = $false)]
  [string] $CacheIconsDir
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Read-JsonFile {
  param([Parameter(Mandatory = $true)][string] $Path)
  if (-not (Test-Path -LiteralPath $Path)) { return $null }
  $raw = Get-Content -LiteralPath $Path -Raw
  if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
  return ($raw | ConvertFrom-Json)
}

function Get-Defindex {
  param([Parameter(Mandatory = $true)] $Item)
  if ($null -eq $Item) { return $null }
  $di = $Item.defindex
  if ($di -is [int]) { return $di }
  if ($di -is [long]) { return [int]$di }
  if ($di -is [string] -and $di -match "^\d+$") { return [int]$di }
  return $null
}

if (-not (Test-Path -LiteralPath $NewSchema)) {
  throw "NewSchema not found: $NewSchema"
}

$existingArr = @()
if ($ExistingSchema -and (Test-Path -LiteralPath $ExistingSchema)) {
  $existing = Read-JsonFile -Path $ExistingSchema
  if ($existing -is [System.Collections.IEnumerable]) { $existingArr = @($existing) }
}

$new = Read-JsonFile -Path $NewSchema
if (-not ($new -is [System.Collections.IEnumerable])) {
  throw "NewSchema JSON is not an array: $NewSchema"
}
$newArr = @($new)

$oldSet = [System.Collections.Generic.HashSet[int]]::new()
foreach ($it in $existingArr) {
  $di = Get-Defindex -Item $it
  if ($null -ne $di) { [void]$oldSet.Add($di) }
}

$newOnly = New-Object System.Collections.Generic.List[object]
$newOnlyDefindexes = New-Object System.Collections.Generic.List[int]

foreach ($it in $newArr) {
  $di = Get-Defindex -Item $it
  if ($null -eq $di) { continue }
  if (-not $oldSet.Contains($di)) {
    $newOnly.Add($it)
    $newOnlyDefindexes.Add($di)
  }
}

$sortedDefindexes = @($newOnlyDefindexes | Sort-Object)

Write-Host "Existing items: $($oldSet.Count)"
Write-Host "New schema items: $($newArr.Count)"
Write-Host "New-only (by defindex): $($sortedDefindexes.Count)"
if ($sortedDefindexes.Count -gt 0) {
  Write-Host ("First 25 new defindexes: " + (($sortedDefindexes | Select-Object -First 25) -join ", "))
}

if ($NewOnlyOutputPath) {
  $outDir = Split-Path -Parent $NewOnlyOutputPath
  if ($outDir -and -not (Test-Path -LiteralPath $outDir)) {
    New-Item -ItemType Directory -Path $outDir | Out-Null
  }
  $json = $newOnly | ConvertTo-Json -Depth 50
  Set-Content -LiteralPath $NewOnlyOutputPath -Value $json -Encoding UTF8
  Write-Host "Wrote: $NewOnlyOutputPath"
}

function Ensure-Dir {
  param([Parameter(Mandatory = $true)][string] $Path)
  if (-not (Test-Path -LiteralPath $Path)) {
    New-Item -ItemType Directory -Path $Path | Out-Null
  }
}

if ($NewIconsDir -and $CacheIconsDir) {
  if (-not (Test-Path -LiteralPath $NewIconsDir)) { throw "NewIconsDir not found: $NewIconsDir" }
  Ensure-Dir -Path $CacheIconsDir

  $exts = @(".png", ".webp", ".jpg", ".jpeg")
  $copied = 0
  $missing = 0

  foreach ($di in $sortedDefindexes) {
    $foundSource = $null
    foreach ($ext in $exts) {
      $candidate = Join-Path $NewIconsDir ("$di$ext")
      if (Test-Path -LiteralPath $candidate) { $foundSource = $candidate; break }
    }
    if (-not $foundSource) { $missing++; continue }

    $dest = Join-Path $CacheIconsDir (Split-Path -Leaf $foundSource)
    if (-not (Test-Path -LiteralPath $dest)) {
      Copy-Item -LiteralPath $foundSource -Destination $dest
      $copied++
    }
  }

  Write-Host "Icons copied: $copied"
  if ($missing -gt 0) { Write-Host "Icons missing in NewIconsDir for new defindexes: $missing" }
}
