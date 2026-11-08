# SilkThread - Automated Release & Installer Publishing Script

[CmdletBinding()]
param(
    [string]$Version,
    [string]$Title,
    [string]$Notes,
    [string]$GithubUser = "slavaamericefsb-lang",
    [switch]$BuildOnly,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# Refresh environment PATH to ensure git and dotnet are available
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
$vsGit = "C:\Program Files\Microsoft Visual Studio\18\Insiders\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd"
if (Test-Path $vsGit) {
    $env:Path = "$vsGit;" + $env:Path
}

$repoRoot = (Get-Item $PSScriptRoot).Parent.FullName
Set-Location $repoRoot

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  SilkThread - Release Publication & Installer Pipeline   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Repository root: $repoRoot" -ForegroundColor Gray

# 1. Resolve Version and Manifest
$versionFile = Join-Path $repoRoot "version.json"
$manifest = $null
if (Test-Path $versionFile) {
    try {
        $raw = Get-Content $versionFile -Raw -Encoding UTF8
        $manifest = ConvertFrom-Json $raw
    } catch {
        Write-Warning "Could not parse version.json. A new one will be generated."
    }
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    if ($manifest -and $manifest.version) {
        $Version = [string]$manifest.version
    } else {
        $Version = "1.0.0"
    }
}

if ([string]::IsNullOrWhiteSpace($Title)) {
    if ($manifest -and $manifest.title) {
        $Title = [string]$manifest.title
    } else {
        $Title = "SilkThread v$Version - Minimal Dark UI Release"
    }
}

if ([string]::IsNullOrWhiteSpace($Notes)) {
    if ($manifest -and $manifest.description) {
        $Notes = [string]$manifest.description
    } else {
        $Notes = "Первый стабильный релиз SilkThread с поддержкой IOST, Bento Grid и оптимизаций задержек Windows 11."
    }
}

$releaseDate = (Get-Date).ToString("yyyy-MM-dd")

# Determine GitHub user if not provided
if ([string]::IsNullOrWhiteSpace($GithubUser)) {
    $remoteUrl = ""
    try {
        $remoteUrl = (& git config --get remote.origin.url) 2>$null
    } catch {}
    
    if ($remoteUrl -and ($remoteUrl -match 'github\.com[:/]([^/]+)/SilkThread')) {
        $GithubUser = $matches[1]
    } else {
        $GithubUser = "slavaamericefsb-lang"
    }
}

$downloadUrl = "https://github.com/$GithubUser/SilkThread/releases/latest/download/SilkThread_Setup.exe"
$patchUrl = "https://github.com/$GithubUser/SilkThread/releases/latest/download/SilkThread_Patch.zip"
$patchSize = if ($manifest -and $manifest.patchSize) { [long]$manifest.patchSize } else { $null }
$downloadSize = if ($manifest -and $manifest.downloadSize) { [long]$manifest.downloadSize } else { $null }

# Update version.json initially
$newManifest = [ordered]@{
    version      = $Version
    title        = $Title
    description  = $Notes
    releaseDate  = $releaseDate
    patchUrl     = $patchUrl
    patchSize    = $patchSize
    downloadUrl  = $downloadUrl
    downloadSize = $downloadSize
}
$jsonString = ConvertTo-Json $newManifest -Depth 4
Set-Content -Path $versionFile -Value $jsonString -Encoding UTF8
Write-Host "[OK] Prepared version.json (v$Version, $releaseDate)" -ForegroundColor Green

# Synchronize project file versions and installer engine constant
$verParts = $Version.Split('.')
$major = if ($verParts.Length -gt 0) { $verParts[0] } else { "1" }
$minor = if ($verParts.Length -gt 1) { $verParts[1] } else { "0" }
$build = if ($verParts.Length -gt 2) { $verParts[2] } else { "0" }
$rev   = if ($verParts.Length -gt 3) { $verParts[3] } else { "0" }
$quadVersion = "$major.$minor.$build.$rev"

Write-Host ">>> Synchronizing project versions to $Version ($quadVersion)..." -ForegroundColor Yellow

$appCsproj = Join-Path $repoRoot "src\SilkThread.App\SilkThread.App.csproj"
$installerCsproj = Join-Path $repoRoot "src\SilkThread.Installer\SilkThread.Installer.csproj"
$installEngineCs = Join-Path $repoRoot "src\SilkThread.Installer\InstallEngine.cs"

foreach ($projFile in @($appCsproj, $installerCsproj)) {
    if (Test-Path $projFile) {
        $content = Get-Content $projFile -Raw -Encoding UTF8
        $content = [regex]::Replace($content, '<Version>[^<]+</Version>', "<Version>$Version</Version>")
        $content = [regex]::Replace($content, '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$quadVersion</AssemblyVersion>")
        $content = [regex]::Replace($content, '<FileVersion>[^<]+</FileVersion>', "<FileVersion>$quadVersion</FileVersion>")
        Set-Content -Path $projFile -Value $content -Encoding UTF8
        Write-Host "  [OK] Synchronized $projFile -> $Version ($quadVersion)" -ForegroundColor Gray
    }
}

if (Test-Path $installEngineCs) {
    $content = Get-Content $installEngineCs -Raw -Encoding UTF8
    $content = [regex]::Replace($content, 'public const string AppVersion = "[^"]+";', "public const string AppVersion = `"$Version`";")
    Set-Content -Path $installEngineCs -Value $content -Encoding UTF8
    Write-Host "  [OK] Synchronized $installEngineCs -> $Version" -ForegroundColor Gray
}

# 2. Build and Package (unless SkipBuild is specified)
$distDir = Join-Path $repoRoot "dist"
$setupExe = Join-Path $distDir "SilkThread_Setup.exe"
$patchZip = Join-Path $distDir "SilkThread_Patch.zip"

if (-not $SkipBuild) {
    Write-Host ""
    Write-Host ">>> [1/5] Running automated tests..." -ForegroundColor Yellow
    dotnet test "$repoRoot\tests\SilkThread.Tests\SilkThread.Tests.csproj" -c Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Tests failed. Aborting release publication."
        exit 1
    }
    Write-Host "[OK] All tests passed!" -ForegroundColor Green

    Write-Host ""
    Write-Host ">>> [2/5] Publishing SilkThread.App (win-x64 Standalone)..." -ForegroundColor Yellow
    $appDistDir = Join-Path $distDir "SilkThread"
    dotnet publish "$repoRoot\src\SilkThread.App\SilkThread.App.csproj" -c Release -r win-x64 --self-contained true -o $appDistDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "SilkThread.App build failed."
        exit 1
    }

    Write-Host ""
    Write-Host ">>> [3/5] Packaging Lightweight In-Place Patch (SilkThread_Patch.zip)..." -ForegroundColor Yellow
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    if (Test-Path $patchZip) {
        Remove-Item $patchZip -Force
    }

    $patchStagingDir = Join-Path $distDir "patch_staging"
    if (Test-Path $patchStagingDir) {
        Remove-Item $patchStagingDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $patchStagingDir -Force | Out-Null

    $patchItems = Get-ChildItem -Path $appDistDir -Recurse | Where-Object {
        -not $_.PSIsContainer -and
        -not ($_.Extension -eq ".pdb") -and
        ($_.Name -like "SilkThread.*" -or $_.Name -like "*.pri" -or $_.Name -like "*.xbf" -or $_.Name -like "workloads*.json" -or $_.FullName -like "*\Assets\*")
    }

    $appDistPath = (Resolve-Path $appDistDir).Path
    foreach ($item in $patchItems) {
        $relative = $item.FullName.Substring($appDistPath.Length).TrimStart('\', '/')
        $targetPath = Join-Path $patchStagingDir $relative
        $targetParent = Split-Path $targetPath
        if (-not (Test-Path $targetParent)) {
            New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        }
        Copy-Item $item.FullName $targetPath -Force
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory($patchStagingDir, $patchZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    Remove-Item $patchStagingDir -Recurse -Force

    $patchItem = Get-Item $patchZip
    $patchSize = $patchItem.Length
    $patchMB = [math]::Round($patchItem.Length / 1MB, 2)
    Write-Host "[OK] Lightweight patch created: $patchZip ($patchMB MB, $patchSize bytes)" -ForegroundColor Green

    Write-Host ""
    Write-Host ">>> [4/5] Packaging payload into full installer..." -ForegroundColor Yellow
    $payloadDir = Join-Path $repoRoot "src\SilkThread.Installer\Payload"
    if (-not (Test-Path $payloadDir)) {
        New-Item -ItemType Directory -Path $payloadDir -Force | Out-Null
    }
    $zipPath = Join-Path $payloadDir "installer_payload.zip"
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    [System.IO.Compression.ZipFile]::CreateFromDirectory($appDistDir, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
    $zipFileItem = Get-Item $zipPath
    $zipMB = [math]::Round($zipFileItem.Length / 1MB, 2)
    Write-Host "[OK] Payload archived: $zipPath ($zipMB MB)" -ForegroundColor Green

    Write-Host ""
    Write-Host ">>> [5/5] Compiling Single-File Installer (SilkThread_Setup.exe)..." -ForegroundColor Yellow
    $installerBuildDir = Join-Path $distDir "installer_build"
    dotnet publish "$repoRoot\src\SilkThread.Installer\SilkThread.Installer.csproj" -c Release -r win-x64 --self-contained true -o $installerBuildDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "SilkThread.Installer build failed."
        exit 1
    }

    Copy-Item (Join-Path $installerBuildDir "SilkThread_Setup.exe") $setupExe -Force
}

if (-not (Test-Path $setupExe)) {
    Write-Error "Installer executable not found at: $setupExe"
    exit 1
}

$exeItem = Get-Item $setupExe
$exeMB = [math]::Round($exeItem.Length / 1MB, 2)
Write-Host "[OK] Installer ready: $setupExe ($exeMB MB)" -ForegroundColor Green

# 3. Compute SHA256 & finalize version.json
$hash = (Get-FileHash -Path $setupExe -Algorithm SHA256).Hash
$checksumContent = "$hash  SilkThread_Setup.exe"

if (Test-Path $patchZip) {
    $patchItem = Get-Item $patchZip
    $patchSize = $patchItem.Length
    $patchHash = (Get-FileHash -Path $patchZip -Algorithm SHA256).Hash
    $checksumContent += "`r`n$patchHash  SilkThread_Patch.zip"
}

$checksumPath = Join-Path $distDir "SHA256SUMS.txt"
Set-Content -Path $checksumPath -Value $checksumContent -Encoding ASCII
Write-Host "[OK] SHA256 Checksums updated: $checksumPath" -ForegroundColor Green

# Finalize version.json with accurate patch size and installer download size
$downloadSize = if (Test-Path $setupExe) { (Get-Item $setupExe).Length } else { $null }
$finalManifest = [ordered]@{
    version      = $Version
    title        = $Title
    description  = $Notes
    releaseDate  = $releaseDate
    patchUrl     = $patchUrl
    patchSize    = $patchSize
    downloadUrl  = $downloadUrl
    downloadSize = $downloadSize
}
$jsonString = ConvertTo-Json $finalManifest -Depth 4
Set-Content -Path $versionFile -Value $jsonString -Encoding UTF8
Write-Host "[OK] Finalized version.json (v$Version, patchSize: $patchSize, downloadSize: $downloadSize)" -ForegroundColor Green

if ($BuildOnly) {
    Write-Host ""
    Write-Host "[OK] BuildOnly mode completed successfully!" -ForegroundColor Green
    Write-Host "Installer: $setupExe" -ForegroundColor Cyan
    Write-Host "Patch:     $patchZip" -ForegroundColor Cyan
    exit 0
}

# 4. GitHub Release Tag & Publication
$tagName = "v$Version"
Write-Host ""
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Publishing Release $tagName to GitHub                   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Check if GitHub CLI is installed
$hasGh = $false
try {
    $ghCmd = Get-Command gh -ErrorAction SilentlyContinue
    if ($ghCmd) { $hasGh = $true }
} catch {}

if ($hasGh) {
    Write-Host "GitHub CLI (gh) detected. Checking authentication..." -ForegroundColor Yellow
    $ghAuthCheck = (& gh auth status 2>&1)
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK] Authenticated to GitHub CLI!" -ForegroundColor Green

        $null = cmd.exe /c "gh release view $tagName 2>nul"
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Release $tagName already exists. Updating release assets..." -ForegroundColor Cyan
            & gh release upload $tagName "$setupExe" "$patchZip" "$checksumPath" "$versionFile" --clobber
            if ($LASTEXITCODE -eq 0) {
                Write-Host ""
                Write-Host "[OK] Release $tagName assets updated successfully on GitHub!" -ForegroundColor Green
                exit 0
            }
        } else {
            Write-Host "Creating GitHub Release $tagName..." -ForegroundColor Cyan
            & gh release create $tagName "$setupExe" "$patchZip" "$checksumPath" "$versionFile" --title "$Title" --notes "$Notes"
            if ($LASTEXITCODE -eq 0) {
                Write-Host ""
                Write-Host "[OK] Release $tagName published successfully on GitHub!" -ForegroundColor Green
                exit 0
            }
        }
    }
}

# Fallback: Print manual publication guide
Write-Host ""
Write-Host "GitHub CLI is either not installed or not authenticated." -ForegroundColor Yellow
Write-Host "To publish this release to GitHub manually, run:" -ForegroundColor White
Write-Host ""
Write-Host "1. Commit and push tag:" -ForegroundColor Cyan
Write-Host "   git add ." -ForegroundColor Gray
Write-Host "   git commit -m `"Release $tagName`"" -ForegroundColor Gray
Write-Host "   git tag -a $tagName -m `"$Title`"" -ForegroundColor Gray
Write-Host "   git push origin main --tags" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Create Release in GitHub Web UI:" -ForegroundColor Cyan
Write-Host "   URL: https://github.com/$GithubUser/SilkThread/releases/new" -ForegroundColor White
Write-Host "   - Select tag: $tagName" -ForegroundColor Gray
Write-Host "   - Title: $Title" -ForegroundColor Gray
Write-Host "   - Attach file: $setupExe" -ForegroundColor Gray
Write-Host "   - Attach file: $patchZip" -ForegroundColor Gray
Write-Host "   - Attach file: $checksumPath" -ForegroundColor Gray
Write-Host "   - Attach file: $versionFile" -ForegroundColor Gray
Write-Host ""
Write-Host "[OK] Publication pipeline completed." -ForegroundColor Green
