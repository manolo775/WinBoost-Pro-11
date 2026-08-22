param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [ValidateSet("Preview", "Stable")]
    [string]$Channel = "Preview",

    [string]$DownloadBaseUrl = "",

    [string]$ReleaseNotes = "",

    [switch]$PublishToR2,

    [string]$R2Bucket = "winboost-releases",

    [string]$WranglerVersion = "4.125.0"
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

$updateManifestPath =
    Join-Path $versionRoot "update-manifest.json"

$npxCommand = ""

$publishToR2Succeeded = $false

# ======================================
# WRANGLER HELPER
# ======================================

function Invoke-Wrangler
{
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $npxCommand `
        --yes `
        "wrangler@$WranglerVersion" `
        @Arguments

    if ($LASTEXITCODE -ne 0)
    {
        throw (
            "Wrangler command failed: wrangler " +
            ($Arguments -join " ")
        )
    }
}

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

$downloadBaseUri = $null

if (-not [string]::IsNullOrWhiteSpace($DownloadBaseUrl))
{
    try
    {
        $downloadBaseUri =
            New-Object System.Uri($DownloadBaseUrl)
    }
    catch
    {
        throw "DownloadBaseUrl is not a valid absolute URL."
    }

    if (-not $downloadBaseUri.IsAbsoluteUri)
    {
        throw "DownloadBaseUrl must be an absolute URL."
    }

    if ($downloadBaseUri.Scheme -ne "https")
    {
        throw "DownloadBaseUrl must use HTTPS."
    }
}

if ($PublishToR2)
{
    if ([string]::IsNullOrWhiteSpace($DownloadBaseUrl))
    {
        throw (
            "DownloadBaseUrl is required when " +
            "PublishToR2 is enabled."
        )
    }

    if ($downloadBaseUri.IsLoopback -or
        $downloadBaseUri.Host -eq "localhost")
    {
        throw (
            "PublishToR2 cannot use localhost " +
            "or a loopback DownloadBaseUrl."
        )
    }

    if ([string]::IsNullOrWhiteSpace($R2Bucket))
    {
        throw "R2Bucket is required."
    }

    $npxCommandInfo =
        Get-Command `
            "npx.cmd" `
            -ErrorAction SilentlyContinue

    if ($null -ne $npxCommandInfo)
    {
        $npxCommand =
            $npxCommandInfo.Path
    }
    else
    {
        $fallbackNpx =
            Join-Path `
                $env:ProgramFiles `
                "nodejs\npx.cmd"

        if (Test-Path $fallbackNpx)
        {
            $npxCommand =
                $fallbackNpx
        }
    }

    if ([string]::IsNullOrWhiteSpace($npxCommand) -or
        -not (Test-Path $npxCommand))
    {
        throw (
            "npx.cmd was not found. " +
            "Install Node.js LTS before publishing to R2."
        )
    }

    $expectedComSpec =
        Join-Path `
            $env:SystemRoot `
            "System32\cmd.exe"

    if (-not [string]::Equals(
            $env:ComSpec,
            $expectedComSpec,
            [System.StringComparison]::OrdinalIgnoreCase))
    {
        throw (
            "ComSpec is not configured correctly. " +
            "Expected '$expectedComSpec', " +
            "found '$env:ComSpec'."
        )
    }
}

Write-Host ""
Write-Host "========================================"
Write-Host " WinBoost Pro 11 Release Builder"
Write-Host "========================================"
Write-Host ""
Write-Host "Version : $Version"
Write-Host "Channel : $Channel"

if ($PublishToR2)
{
    Write-Host "R2      : Enabled"
    Write-Host "Bucket  : $R2Bucket"
}
else
{
    Write-Host "R2      : Disabled"
}

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
    Join-Path `
        $publishDirectory `
        "WinBoost.App.exe"

if (-not (Test-Path $appExecutable))
{
    throw "Published WinBoost.App.exe was not found."
}

$productVersion =
    (Get-Item $appExecutable).
        VersionInfo.
        ProductVersion

if ($productVersion -ne $Version)
{
    throw (
        "Published version mismatch. " +
        "Expected '$Version', " +
        "found '$productVersion'."
    )
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
    (Get-FileHash `
        $packagePath `
        -Algorithm SHA256).
        Hash.
        ToUpperInvariant()

# ======================================
# DOWNLOAD URL
# ======================================

$downloadUrl = ""

if (-not [string]::IsNullOrWhiteSpace(
        $DownloadBaseUrl))
{
    $downloadUrl =
        $DownloadBaseUrl.TrimEnd("/") +
        "/" +
        $packageName
}

# ======================================
# RELEASE INFORMATION
# ======================================

$releaseInfo =
    [ordered]@{
        Version = $Version
        Channel = $Channel
        PackageName = $packageName
        Sha256 = $sha256
        CreatedAtUtc =
            (Get-Date).
                ToUniversalTime().
                ToString("o")
    }

$releaseInfo |
    ConvertTo-Json |
    Set-Content `
        $releaseInfoPath `
        -Encoding UTF8

# ======================================
# UPDATE MANIFEST
# ======================================

$updateManifest =
    [ordered]@{
        Version = $Version
        Channel = $Channel
        DownloadUrl = $downloadUrl
        Sha256 = $sha256
        ReleaseNotes = $ReleaseNotes
    }

$updateManifest |
    ConvertTo-Json |
    Set-Content `
        $updateManifestPath `
        -Encoding UTF8

# ======================================
# CLOUDFLARE R2 PUBLISH
# ======================================

if ($PublishToR2)
{
    Write-Host ""
    Write-Host "========================================"
    Write-Host " Cloudflare R2 publication"
    Write-Host "========================================"
    Write-Host ""

    $verificationDirectory =
        Join-Path `
            ([System.IO.Path]::GetTempPath()) `
            (
                "WinBoost\ReleaseVerification\" +
                [Guid]::NewGuid().ToString("N")
            )

    New-Item `
        -ItemType Directory `
        -Path $verificationDirectory `
        -Force |
        Out-Null

    $remotePackageVerificationPath =
        Join-Path `
            $verificationDirectory `
            $packageName

    $previousManifestPath =
        Join-Path `
            $verificationDirectory `
            "previous-update-manifest.json"

    $remoteManifestVerificationPath =
        Join-Path `
            $verificationDirectory `
            "verified-update-manifest.json"

    $previousManifestExists = $false

    try
    {
        # ======================================
        # CHECK EXISTING REMOTE PACKAGE
        # ======================================

        Write-Host "Checking existing R2 package..."

        Remove-Item `
            $remotePackageVerificationPath `
            -Force `
            -ErrorAction SilentlyContinue

        & $npxCommand `
            --yes `
            "wrangler@$WranglerVersion" `
            r2 `
            object `
            get `
            "$R2Bucket/$packageName" `
            "--file=$remotePackageVerificationPath" `
            --remote `
            *> $null

        $remotePackageAlreadyExists =
            (
                $LASTEXITCODE -eq 0 -and
                (Test-Path $remotePackageVerificationPath)
            )

        if ($remotePackageAlreadyExists)
        {
            $existingRemoteSha =
                (Get-FileHash `
                    $remotePackageVerificationPath `
                    -Algorithm SHA256).
                    Hash.
                    ToUpperInvariant()

            if (-not [string]::Equals(
                    $sha256,
                    $existingRemoteSha,
                    [System.StringComparison]::OrdinalIgnoreCase))
            {
                throw (
                    "The R2 package '$packageName' already exists " +
                    "with a different SHA-256. " +
                    "Refusing to overwrite an immutable release package. " +
                    "Use a new WinBoost version."
                )
            }

            Write-Host (
                "Remote package already exists and " +
                "matches the local SHA-256."
            )

            Write-Host "Package upload skipped."
        }
        else
        {
            # ======================================
            # UPLOAD PACKAGE
            # ======================================

            Write-Host "Uploading package to R2..."

            Invoke-Wrangler `
                -Arguments @(
                    "r2",
                    "object",
                    "put",
                    "$R2Bucket/$packageName",
                    "--file=$packagePath",
                    "--content-type=application/zip",
                    "--cache-control=public,max-age=31536000,immutable",
                    "--remote"
                )

            # ======================================
            # DOWNLOAD PACKAGE FROM R2
            # ======================================

            Write-Host (
                "Downloading uploaded package " +
                "from R2 for verification..."
            )

            Remove-Item `
                $remotePackageVerificationPath `
                -Force `
                -ErrorAction SilentlyContinue

            Invoke-Wrangler `
                -Arguments @(
                    "r2",
                    "object",
                    "get",
                    "$R2Bucket/$packageName",
                    "--file=$remotePackageVerificationPath",
                    "--remote"
                )
        }

        # ======================================
        # VERIFY REMOTE PACKAGE SHA-256
        # ======================================

        if (-not (Test-Path $remotePackageVerificationPath))
        {
            throw (
                "The package could not be downloaded " +
                "from R2 for verification."
            )
        }

        $remotePackageSha =
            (Get-FileHash `
                $remotePackageVerificationPath `
                -Algorithm SHA256).
                Hash.
                ToUpperInvariant()

        if (-not [string]::Equals(
                $sha256,
                $remotePackageSha,
                [System.StringComparison]::OrdinalIgnoreCase))
        {
            throw (
                "R2 package SHA-256 verification failed. " +
                "Expected '$sha256', " +
                "received '$remotePackageSha'."
            )
        }

        Write-Host (
            "R2 package SHA-256 verified: " +
            $remotePackageSha
        )

        # ======================================
        # BACKUP CURRENT REMOTE MANIFEST
        # ======================================

        Write-Host (
            "Backing up current remote manifest " +
            "if one exists..."
        )

        Remove-Item `
            $previousManifestPath `
            -Force `
            -ErrorAction SilentlyContinue

        & $npxCommand `
            --yes `
            "wrangler@$WranglerVersion" `
            r2 `
            object `
            get `
            "$R2Bucket/update-manifest.json" `
            "--file=$previousManifestPath" `
            --remote `
            *> $null

        if ($LASTEXITCODE -eq 0 -and
            (Test-Path $previousManifestPath))
        {
            $previousManifestExists = $true

            Write-Host "Current manifest backup created."
        }
        else
        {
            $previousManifestExists = $false

            Remove-Item `
                $previousManifestPath `
                -Force `
                -ErrorAction SilentlyContinue

            Write-Host (
                "No existing remote manifest " +
                "was available for backup."
            )
        }

        # ======================================
        # PUBLISH MANIFEST
        # ======================================

        try
        {
            Write-Host "Publishing update manifest to R2..."

            Invoke-Wrangler `
                -Arguments @(
                    "r2",
                    "object",
                    "put",
                    "$R2Bucket/update-manifest.json",
                    "--file=$updateManifestPath",
                    "--content-type=application/json",
                    "--cache-control=no-store,no-cache,must-revalidate",
                    "--remote"
                )

            # ======================================
            # VERIFY REMOTE MANIFEST
            # ======================================

            Write-Host "Verifying remote update manifest..."

            Remove-Item `
                $remoteManifestVerificationPath `
                -Force `
                -ErrorAction SilentlyContinue

            Invoke-Wrangler `
                -Arguments @(
                    "r2",
                    "object",
                    "get",
                    "$R2Bucket/update-manifest.json",
                    "--file=$remoteManifestVerificationPath",
                    "--remote"
                )

            if (-not (Test-Path $remoteManifestVerificationPath))
            {
                throw (
                    "The published update manifest " +
                    "could not be downloaded for verification."
                )
            }

            $localManifestSha =
                (Get-FileHash `
                    $updateManifestPath `
                    -Algorithm SHA256).
                    Hash.
                    ToUpperInvariant()

            $remoteManifestSha =
                (Get-FileHash `
                    $remoteManifestVerificationPath `
                    -Algorithm SHA256).
                    Hash.
                    ToUpperInvariant()

            if (-not [string]::Equals(
                    $localManifestSha,
                    $remoteManifestSha,
                    [System.StringComparison]::OrdinalIgnoreCase))
            {
                throw (
                    "Remote update manifest verification failed. " +
                    "The uploaded manifest differs from the local manifest."
                )
            }

            $remoteManifest =
                Get-Content `
                    $remoteManifestVerificationPath `
                    -Raw |
                ConvertFrom-Json

            if ($remoteManifest.Version -ne $Version)
            {
                throw (
                    "Remote manifest version mismatch. " +
                    "Expected '$Version', " +
                    "found '$($remoteManifest.Version)'."
                )
            }

            if ($remoteManifest.Channel -ne $Channel)
            {
                throw (
                    "Remote manifest channel mismatch."
                )
            }

            if ($remoteManifest.DownloadUrl -ne $downloadUrl)
            {
                throw (
                    "Remote manifest DownloadUrl mismatch."
                )
            }

            if (-not [string]::Equals(
                    $remoteManifest.Sha256,
                    $sha256,
                    [System.StringComparison]::OrdinalIgnoreCase))
            {
                throw (
                    "Remote manifest SHA-256 value mismatch."
                )
            }

            # ======================================
            # VERIFY PUBLIC MANIFEST
            # ======================================

            $publicManifestUrl =
                $DownloadBaseUrl.TrimEnd("/") +
                "/update-manifest.json"

            $publicManifestVerified = $false

            for ($attempt = 1; $attempt -le 3; $attempt++)
            {
                try
                {
                    Write-Host (
                        "Verifying public manifest " +
                        "(attempt $attempt of 3)..."
                    )

                    $verificationToken =
                        [Guid]::NewGuid().
                            ToString("N")

                    $publicManifest =
                        Invoke-RestMethod `
                            -Uri (
                                $publicManifestUrl +
                                "?verify=" +
                                $verificationToken
                            ) `
                            -Method Get

                    if ($publicManifest.Version -ne $Version)
                    {
                        throw (
                            "Public manifest version mismatch."
                        )
                    }

                    if ($publicManifest.DownloadUrl -ne $downloadUrl)
                    {
                        throw (
                            "Public manifest DownloadUrl mismatch."
                        )
                    }

                    if (-not [string]::Equals(
                            $publicManifest.Sha256,
                            $sha256,
                            [System.StringComparison]::OrdinalIgnoreCase))
                    {
                        throw (
                            "Public manifest SHA-256 mismatch."
                        )
                    }

                    $publicManifestVerified = $true

                    break
                }
                catch
                {
                    if ($attempt -ge 3)
                    {
                        throw
                    }

                    Start-Sleep -Seconds 2
                }
            }

            if (-not $publicManifestVerified)
            {
                throw (
                    "Public update manifest verification failed."
                )
            }

            Write-Host "Remote manifest verified."
            Write-Host "Public manifest verified."

            $publishToR2Succeeded = $true
        }
        catch
        {
            $manifestPublishException =
                $_.Exception

            if ($previousManifestExists)
            {
                Write-Warning (
                    "Manifest publication failed. " +
                    "Restoring the previous manifest..."
                )

                try
                {
                    Invoke-Wrangler `
                        -Arguments @(
                            "r2",
                            "object",
                            "put",
                            "$R2Bucket/update-manifest.json",
                            "--file=$previousManifestPath",
                            "--content-type=application/json",
                            "--cache-control=no-store,no-cache,must-revalidate",
                            "--remote"
                        )

                    Write-Host (
                        "Previous remote manifest restored."
                    )
                }
                catch
                {
                    Write-Warning (
                        "The previous remote manifest " +
                        "could not be restored automatically."
                    )
                }
            }

            throw $manifestPublishException
        }
    }
    finally
    {
        Remove-Item `
            $verificationDirectory `
            -Recurse `
            -Force `
            -ErrorAction SilentlyContinue
    }
}

# ======================================
# RESULT
# ======================================

Write-Host ""
Write-Host "========================================"
Write-Host " Release completed successfully"
Write-Host "========================================"
Write-Host ""
Write-Host "Version  : $Version"
Write-Host "Channel  : $Channel"
Write-Host "Package  : $packagePath"
Write-Host "SHA-256  : $sha256"
Write-Host "Info     : $releaseInfoPath"
Write-Host "Manifest : $updateManifestPath"

if ($PublishToR2)
{
    if (-not $publishToR2Succeeded)
    {
        throw (
            "The local release was created, " +
            "but R2 publication was not completed."
        )
    }

    Write-Host ""
    Write-Host "Cloudflare R2 publication: SUCCESS"
    Write-Host "Bucket   : $R2Bucket"
    Write-Host "Package  : $packageName"
    Write-Host "Manifest : update-manifest.json"
}
elseif ([string]::IsNullOrWhiteSpace($downloadUrl))
{
    Write-Host ""
    Write-Host "NOTE: Download URL was not configured."
    Write-Host (
        "Provide -DownloadBaseUrl when " +
        "building a production release."
    )
}
else
{
    Write-Host ""
    Write-Host "NOTE: R2 publication was not requested."
    Write-Host (
        "Use -PublishToR2 when the release " +
        "is ready to be published."
    )
}

Write-Host ""