param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [ValidateSet("Preview", "Stable")]
    [string]$Channel = "Preview"
)

$ErrorActionPreference = "Stop"

# ======================================
# PATHS
# ======================================

$repositoryRoot =
    Split-Path -Parent $PSScriptRoot

$solutionRoot =
    Join-Path $repositoryRoot "src\WinBoost-Pro-11"

$appProject =
    Join-Path $solutionRoot "WinBoost.App\WinBoost.App.csproj"

$releaseRoot =
    Join-Path $repositoryRoot "releases"

$versionRoot =
    Join-Path $releaseRoot $Version

$publishDirectory =
    Join-Path $versionRoot "publish"

$packageName =
    "WinBoost-$Version.zip"

$packagePath =
    Join-Path $versionRoot $packageName

$releaseInfoPath =
    Join-Path $versionRoot "release-info.json"

# ======================================
# VALIDATION
# ======================================

if ([string]::IsNullOrWhiteSpace($Version))
{
    throw "Version is required."
}

if (-not (Test-Path $appProject))
{
    throw "WinBoost.App.csproj was not found: $appProject"
}

Write-Host ""
Write-Host "========================================"
Write-Host " WinBoost Pro 11 Release Builder"
Write-Host "========================================"
Write-Host ""
Write-Host "Version : $Version"
Write-Host "Channel : $Channel"
Write-Host ""

# ======================================
# CLEAN RELEASE DIRECTORY
# ======================================

if (Test-Path $versionRoot)
{
    Write-Host "Removing previous release directory..."

    Remove-Item `
        $versionRoot `
        -Recurse `
        -Force
}

New-Item `
    -ItemType Directory `
    -Path $publishDirectory `
    -Force |
    Out-Null

# ======================================
# PUBLISH WINBOOST
# ======================================

Write-Host "Publishing WinBoost..."

dotnet publish `
    $appProject `
    -c Release `
    -p:Version=$Version `
    -p:InformationalVersion=$Version `
    -p:IncludeSourceRevisionInInformationalVersion=false `
    -o $publishDirectory

if ($LASTEXITCODE -ne 0)
{
    throw "WinBoost publish failed."
}

# ======================================
# VERIFY PUBLISHED VERSION
# ======================================

$appExecutable =
    Join-Path $publishDirectory "WinBoost.App.exe"

if (-not (Test-Path $appExecutable))
{
    throw "Published WinBoost.App.exe was not found."
}

$productVersion =
    (Get-Item $appExecutable).VersionInfo.ProductVersion

if ($productVersion -ne $Version)
{
    throw "Published version mismatch. Expected '$Version', found '$productVersion'."
}

Write-Host "Published version verified: $productVersion"

# ======================================
# CREATE UPDATE PACKAGE
# ======================================

Write-Host "Creating update package..."

Compress-Archive `
    -Path "$publishDirectory\*" `
    -DestinationPath $packagePath `
    -Force

if (-not (Test-Path $packagePath))
{
    throw "Update package was not created."
}

# ======================================
# SHA-256
# ======================================

Write-Host "Calculating SHA-256..."

$sha256 =
    (Get-FileHash $packagePath -Algorithm SHA256).Hash.ToUpperInvariant()

# ======================================
# RELEASE INFORMATION
# ======================================

$releaseInfo =
    [ordered]@{
        Version = $Version
        Channel = $Channel
        PackageName = $packageName
        Sha256 = $sha256
        CreatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    }

$releaseInfo |
    ConvertTo-Json |
    Set-Content `
        $releaseInfoPath `
        -Encoding UTF8

# ======================================
# RESULT
# ======================================

Write-Host ""
Write-Host "========================================"
Write-Host " Release completed successfully"
Write-Host "========================================"
Write-Host ""
Write-Host "Version : $Version"
Write-Host "Channel : $Channel"
Write-Host "Package : $packagePath"
Write-Host "SHA-256 : $sha256"
Write-Host "Info    : $releaseInfoPath"
Write-Host ""