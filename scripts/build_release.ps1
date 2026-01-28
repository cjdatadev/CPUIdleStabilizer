# CPUIdleStabilizer Release Build Script
# usage: .\scripts\build_release.ps1

$scriptDir = $PSScriptRoot
$rootDir = (Get-Item $scriptDir).Parent.FullName
Set-Location $rootDir

$version = "v1.0.4"
$zipsDir = ".\zips"
$publishDir = ".\publish"
$docsDir = ".\docs"

Write-Host "Starting Release Build [$version]..." -ForegroundColor Cyan

# 0. Clean & Prepare Documentation
Write-Host "Preparing Documentation..." -ForegroundColor Gray
# Ensure LICENSE.txt is up to date in docs folder
Copy-Item ".\LICENSE" -Destination "$docsDir\LICENSE.txt" -Force

$docs = Get-ChildItem -Path $docsDir -File

# 1. Kill running instances
$proc = Get-Process -Name "CPUIdleStabilizer" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Stopping running instance..." -ForegroundColor Yellow
    Stop-Process -Name "CPUIdleStabilizer" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

# 2. Clean Directories
Write-Host "Cleaning directories..." -ForegroundColor Gray
if (Test-Path $publishDir) { Remove-Item -Path $publishDir -Recurse -Force }
if (Test-Path $zipsDir) { Remove-Item -Path $zipsDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $zipsDir | Out-Null

# 3. Build Standalone (Robust: Self-Contained, Partial Trim, No R2R)
Write-Host "Building Standalone Version..." -ForegroundColor Cyan
$standaloneDir = "$publishDir\standalone"
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=false -p:TrimMode=partial -p:BuiltInComInteropSupport=true -p:_SuppressWinFormsTrimError=true -o $standaloneDir

if ($LASTEXITCODE -ne 0) { Write-Error "Standalone build failed!"; exit 1 }

# 4. Build Lightweight (Framework Dependent)
Write-Host "Building Lightweight Version..." -ForegroundColor Cyan
$lightweightDir = "$publishDir\lightweight"
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o $lightweightDir

if ($LASTEXITCODE -ne 0) { Write-Error "Lightweight build failed!"; exit 1 }

# 5. Cleanup PDBs (Debug Symbols)
Write-Host "Removing PDB files..." -ForegroundColor Gray
Get-ChildItem -Path $publishDir -Recurse -Filter "*.pdb" | Remove-Item -Force

# 6. Copy Documentation
Write-Host "Copying Documentation..." -ForegroundColor Gray
foreach ($doc in $docs) {
    Copy-Item $doc.FullName -Destination $standaloneDir
    Copy-Item $doc.FullName -Destination $lightweightDir
}

# 7. Package Zips
Write-Host "Packaging ZIPs..." -ForegroundColor Cyan
$standaloneZip = "$zipsDir\CPUIdleStabilizer_Standalone_$version.zip"
$lightweightZip = "$zipsDir\CPUIdleStabilizer_Lightweight_$version.zip"

Compress-Archive -Path "$standaloneDir\*" -DestinationPath $standaloneZip
Compress-Archive -Path "$lightweightDir\*" -DestinationPath $lightweightZip

Write-Host "------------------------------------------------" -ForegroundColor Green
Write-Host "Release Complete!" -ForegroundColor Green
Write-Host "Artifacts:"
Write-Host "1. $standaloneZip" -ForegroundColor White
Write-Host "2. $lightweightZip" -ForegroundColor White
Write-Host "------------------------------------------------" -ForegroundColor Green
