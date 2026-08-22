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
    Join-Path `
        $repositoryRoot `
        "src\WinBoost-Pro-11"

$appProject =
    Join-Path `
        $solutionRoot `
        "WinBoost.App\WinBoost.App.csproj"

$releaseRoot =
    Join-Path `
        $repositoryRoot `
        "releases"

$versionRoot =
    Join-Path `
        $releaseRoot `
        $Version

$publishDirectory =
    Join-Path `
        $versionRoot `
        "publish"

$packageName =
    "WinBoost-$Version.zip"

$packagePath =
    Join-Path `
        $versionRoot `
        $packageName

$releaseInfoPath =
    Join-Path `
        $versionRoot `
        "release-info.json"

$updateManifestPath =
    Join-Path `
        $versionRoot `
        "update-manifest.json"

$npxCommand = ""

$publishToR2Succeeded = $false


# ======================================
# UTF-8 WITHOUT BOM
# ======================================

function Write-JsonUtf8NoBom
{
    param(
        [Parameter(Mandatory = $true)]
        $Value,

        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $json =
        $Value |
        ConvertTo-Json -Depth 10

    $utf8WithoutBom =
        New-Object `
            System.Text.UTF8Encoding($false)

    [System.IO.File]::WriteAllText(
        $Path,
        $json + [Environment]::NewLine,
        $utf8WithoutBom)
}


# ======================================
# DETERMINISTIC ZIP
# ======================================

function New-DeterministicZip
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceDirectory,

        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    Add-Type `
        -AssemblyName System.IO.Compression

    Add-Type `
        -AssemblyName System.IO.Compression.FileSystem

    if (Test-Path $DestinationPath)
    {
        Remove-Item `
            $DestinationPath `
            -Force
    }

    $sourceRoot =
        [System.IO.Path]::GetFullPath(
            $SourceDirectory)

    $sourceRoot =
        $sourceRoot.TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar
        ) +
        [System.IO.Path]::DirectorySeparatorChar

    $files =
        Get-ChildItem `
            -Path $SourceDirectory `
            -File `
            -Recurse |
        Sort-Object FullName

    $fileStream =
        [System.IO.File]::Open(
            $DestinationPath,
            [System.IO.FileMode]::CreateNew,
            [System.IO.FileAccess]::ReadWrite,
            [System.IO.FileShare]::None)

    try
    {
        $archive =
            New-Object `
                System.IO.Compression.ZipArchive(
                    $fileStream,
                    [System.IO.Compression.ZipArchiveMode]::Create,
                    $false)

        try
        {
            foreach ($file in $files)
            {
                $relativePath =
                    $file.FullName.Substring(
                        $sourceRoot.Length)

                $relativePath =
                    $relativePath.Replace(
                        "\",
                        "/")

                $entry =
                    $archive.CreateEntry(
                        $relativePath,
                        [System.IO.Compression.CompressionLevel]::Optimal)

                # Fixăm timestamp-ul din ZIP.
                # Astfel, aceeași versiune construită din
                # aceleași fișiere produce același pachet.
                $entry.LastWriteTime =
                    [System.DateTimeOffset]::Parse(
                        "2000-01-01T00:00:00Z")

                $inputStream =
                    [System.IO.File]::OpenRead(
                        $file.FullName)

                $outputStream =
                    $entry.Open()

                try
                {
                    $inputStream.CopyTo(
                        $outputStream)
                }
                finally
                {
                    $outputStream.Dispose()
                    $inputStream.Dispose()
                }
            }
        }
        finally
        {
            $archive.Dispose()
        }
    }
    finally
    {
        $fileStream.Dispose()
    }
}


# ======================================
# WRANGLER
# ======================================

function Invoke-Wrangler
{
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $script:npxCommand `
        --yes `
        "wrangler@$script:WranglerVersion" `
        @Arguments

    $exitCode =
        $LASTEXITCODE

    if ($exitCode -ne 0)
    {
        throw (
            "Wrangler command failed with exit code " +
            "$exitCode`: wrangler " +
            ($Arguments -join " ")
        )
    }
}


# ======================================
# PUBLIC DOWNLOAD HELPERS
# ======================================

function Add-VerificationQuery
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    $verificationToken =
        [Guid]::NewGuid().
            ToString("N")

    if ($Url.Contains("?"))
    {
        return (
            $Url +
            "&winboost_verify=" +
            $verificationToken
        )
    }

    return (
        $Url +
        "?winboost_verify=" +
        $verificationToken
    )
}


function Invoke-PublicDownload
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url,

        [Parameter(Mandatory = $true)]
        [string]$DestinationPath,

        [int]$Attempts = 1,

        [int]$DelaySeconds = 2,

        [switch]$AllowNotFound
    )

    $lastStatusCode = ""
    $lastExitCode = 0

    for (
        $attempt = 1;
        $attempt -le $Attempts;
        $attempt++
    )
    {
        Remove-Item `
            $DestinationPath `
            -Force `
            -ErrorAction SilentlyContinue

        $requestUrl =
            Add-VerificationQuery `
                -Url $Url

        Write-Host (
            "Public verification attempt " +
            "$attempt of $Attempts..."
        )

        $statusOutput =
            & curl.exe `
                -sS `
                -L `
                --connect-timeout 20 `
                --max-time 180 `
                -H "Cache-Control: no-cache" `
                -H "Pragma: no-cache" `
                -o $DestinationPath `
                -w "%{http_code}" `
                $requestUrl

        $lastExitCode =
            $LASTEXITCODE

        $lastStatusCode =
            (
                $statusOutput |
                Out-String
            ).Trim()

        if (
            $lastExitCode -eq 0 -and
            $lastStatusCode -match "^2\d\d$" -and
            (Test-Path $DestinationPath)
        )
        {
            return $true
        }

        if (
            $lastStatusCode -eq "404"
        )
        {
            Remove-Item `
                $DestinationPath `
                -Force `
                -ErrorAction SilentlyContinue

            if (
                $AllowNotFound -and
                $attempt -ge $Attempts
            )
            {
                return $false
            }
        }

        if ($attempt -lt $Attempts)
        {
            Start-Sleep `
                -Seconds $DelaySeconds
        }
    }

    if (
        $AllowNotFound -and
        $lastStatusCode -eq "404"
    )
    {
        return $false
    }

    throw (
        "Public download verification failed. " +
        "URL='$Url', " +
        "HTTP='$lastStatusCode', " +
        "curl exit code='$lastExitCode'."
    )
}


# ======================================
# VALIDATION
# ======================================

if ([string]::IsNullOrWhiteSpace(
        $Version))
{
    throw "Version is required."
}

if (-not
    (Test-Path $appProject))
{
    throw (
        "WinBoost.App.csproj was not found: " +
        $appProject
    )
}

$downloadBaseUri = $null

if (-not
    [string]::IsNullOrWhiteSpace(
        $DownloadBaseUrl))
{
    try
    {
        $downloadBaseUri =
            New-Object `
                System.Uri(
                    $DownloadBaseUrl)
    }
    catch
    {
        throw (
            "DownloadBaseUrl is not a valid " +
            "absolute URL."
        )
    }

    if (-not
        $downloadBaseUri.IsAbsoluteUri)
    {
        throw (
            "DownloadBaseUrl must be an " +
            "absolute URL."
        )
    }

    if ($downloadBaseUri.Scheme -ne "https")
    {
        throw (
            "DownloadBaseUrl must use HTTPS."
        )
    }
}

if ($PublishToR2)
{
    if ([string]::IsNullOrWhiteSpace(
            $DownloadBaseUrl))
    {
        throw (
            "DownloadBaseUrl is required " +
            "when PublishToR2 is enabled."
        )
    }

    if (
        $downloadBaseUri.IsLoopback -or
        $downloadBaseUri.Host -eq "localhost"
    )
    {
        throw (
            "PublishToR2 cannot use localhost " +
            "or a loopback DownloadBaseUrl."
        )
    }

    if ([string]::IsNullOrWhiteSpace(
            $R2Bucket))
    {
        throw "R2Bucket is required."
    }

    $curlCommand =
        Get-Command `
            "curl.exe" `
            -ErrorAction SilentlyContinue

    if ($null -eq $curlCommand)
    {
        throw (
            "curl.exe was not found."
        )
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

    if (
        [string]::IsNullOrWhiteSpace(
            $npxCommand) -or
        -not
            (Test-Path $npxCommand)
    )
    {
        throw (
            "npx.cmd was not found. " +
            "Install Node.js LTS before " +
            "publishing to R2."
        )
    }

    $expectedComSpec =
        Join-Path `
            $env:SystemRoot `
            "System32\cmd.exe"

    if (-not
        [string]::Equals(
            $env:ComSpec,
            $expectedComSpec,
            [System.StringComparison]::
                OrdinalIgnoreCase))
    {
        throw (
            "ComSpec is not configured correctly. " +
            "Expected '$expectedComSpec', " +
            "found '$env:ComSpec'."
        )
    }
}


# ======================================
# HEADER
# ======================================

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
    Write-Host (
        "Removing previous local release directory..."
    )

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

if (-not
    (Test-Path $appExecutable))
{
    throw (
        "Published WinBoost.App.exe " +
        "was not found."
    )
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

Write-Host (
    "Published version verified: " +
    $productVersion
)


# ======================================
# CREATE DETERMINISTIC PACKAGE
# ======================================

Write-Host (
    "Creating deterministic update package..."
)

New-DeterministicZip `
    -SourceDirectory $publishDirectory `
    -DestinationPath $packagePath

if (-not
    (Test-Path $packagePath))
{
    throw (
        "Update package was not created."
    )
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

if (-not
    [string]::IsNullOrWhiteSpace(
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

Write-JsonUtf8NoBom `
    -Value $releaseInfo `
    -Path $releaseInfoPath


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

Write-JsonUtf8NoBom `
    -Value $updateManifest `
    -Path $updateManifestPath


# ======================================
# CLOUDFLARE R2 PUBLICATION
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
                [Guid]::NewGuid().
                    ToString("N")
            )

    New-Item `
        -ItemType Directory `
        -Path $verificationDirectory `
        -Force |
        Out-Null

    $publicPackagePath =
        Join-Path `
            $verificationDirectory `
            "public-package.zip"

    $previousManifestPath =
        Join-Path `
            $verificationDirectory `
            "previous-update-manifest.json"

    $publicManifestPath =
        Join-Path `
            $verificationDirectory `
            "public-update-manifest.json"

    $previousManifestExists = $false

    $publicManifestUrl =
        $DownloadBaseUrl.TrimEnd("/") +
        "/update-manifest.json"

    try
    {
        # ======================================
        # CHECK EXISTING PACKAGE
        # ======================================

        Write-Host (
            "Checking whether the package " +
            "already exists publicly..."
        )

        $packageAlreadyExists =
            Invoke-PublicDownload `
                -Url $downloadUrl `
                -DestinationPath $publicPackagePath `
                -Attempts 3 `
                -DelaySeconds 2 `
                -AllowNotFound

        if ($packageAlreadyExists)
        {
            Write-Host (
                "Remote package already exists."
            )

            $existingRemoteSha =
                (Get-FileHash `
                    $publicPackagePath `
                    -Algorithm SHA256).
                    Hash.
                    ToUpperInvariant()

            Write-Host (
                "Existing remote SHA-256: " +
                $existingRemoteSha
            )

            if (-not
                [string]::Equals(
                    $sha256,
                    $existingRemoteSha,
                    [System.StringComparison]::
                        OrdinalIgnoreCase))
            {
                throw (
                    "The public package '$packageName' " +
                    "already exists with a different " +
                    "SHA-256. Refusing to overwrite " +
                    "an immutable WinBoost release. " +
                    "Use a new version number."
                )
            }

            Write-Host (
                "Existing package SHA-256 matches."
            )

            Write-Host (
                "Package upload skipped."
            )
        }
        else
        {
            # ======================================
            # UPLOAD PACKAGE
            # ======================================

            Write-Host (
                "Remote package does not exist yet."
            )

            Write-Host (
                "Uploading package to R2..."
            )

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

            Write-Host (
                "Package upload completed."
            )
        }


        # ======================================
        # VERIFY PUBLIC PACKAGE
        # ======================================

        Write-Host (
            "Downloading package through " +
            "downloads.winboostapp.com..."
        )

        Invoke-PublicDownload `
            -Url $downloadUrl `
            -DestinationPath $publicPackagePath `
            -Attempts 8 `
            -DelaySeconds 2 |
            Out-Null

        $publicPackageSha =
            (Get-FileHash `
                $publicPackagePath `
                -Algorithm SHA256).
                Hash.
                ToUpperInvariant()

        Write-Host (
            "Public package SHA-256: " +
            $publicPackageSha
        )

        if (-not
            [string]::Equals(
                $sha256,
                $publicPackageSha,
                [System.StringComparison]::
                    OrdinalIgnoreCase))
        {
            throw (
                "Public package SHA-256 " +
                "verification failed. Expected '" +
                $sha256 +
                "', received '" +
                $publicPackageSha +
                "'."
            )
        }

        Write-Host (
            "Public package SHA-256 verified."
        )


        # ======================================
        # BACKUP CURRENT PUBLIC MANIFEST
        # ======================================

        Write-Host (
            "Backing up current public manifest..."
        )

        $previousManifestExists =
            Invoke-PublicDownload `
                -Url $publicManifestUrl `
                -DestinationPath $previousManifestPath `
                -Attempts 3 `
                -DelaySeconds 2 `
                -AllowNotFound

        if ($previousManifestExists)
        {
            Write-Host (
                "Current manifest backup created."
            )
        }
        else
        {
            Write-Host (
                "No current public manifest exists."
            )
        }


        # ======================================
        # PUBLISH NEW MANIFEST
        # ======================================

        try
        {
            Write-Host (
                "Publishing update manifest to R2..."
            )

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

            Write-Host (
                "Manifest upload completed."
            )


            # ======================================
            # VERIFY PUBLIC MANIFEST
            # ======================================

            Write-Host (
                "Verifying public update manifest..."
            )

            Invoke-PublicDownload `
                -Url $publicManifestUrl `
                -DestinationPath $publicManifestPath `
                -Attempts 8 `
                -DelaySeconds 2 |
                Out-Null

            $publicManifestJson =
                [System.IO.File]::ReadAllText(
                    $publicManifestPath,
                    [System.Text.Encoding]::UTF8)

            $publicManifest =
                $publicManifestJson |
                ConvertFrom-Json

            if ($publicManifest.Version -ne
                $Version)
            {
                throw (
                    "Public manifest version mismatch. " +
                    "Expected '$Version', found " +
                    "'$($publicManifest.Version)'."
                )
            }

            if ($publicManifest.Channel -ne
                $Channel)
            {
                throw (
                    "Public manifest channel mismatch. " +
                    "Expected '$Channel', found " +
                    "'$($publicManifest.Channel)'."
                )
            }

            if ($publicManifest.DownloadUrl -ne
                $downloadUrl)
            {
                throw (
                    "Public manifest DownloadUrl " +
                    "mismatch."
                )
            }

            if (-not
                [string]::Equals(
                    $publicManifest.Sha256,
                    $sha256,
                    [System.StringComparison]::
                        OrdinalIgnoreCase))
            {
                throw (
                    "Public manifest SHA-256 " +
                    "value mismatch."
                )
            }

            if (
                [string]$publicManifest.ReleaseNotes -ne
                [string]$ReleaseNotes
            )
            {
                throw (
                    "Public manifest ReleaseNotes " +
                    "mismatch."
                )
            }

            Write-Host (
                "Public manifest verified."
            )

            $publishToR2Succeeded = $true
        }
        catch
        {
            $manifestException =
                $_.Exception

            Write-Warning (
                "Manifest publication or " +
                "verification failed."
            )

            if ($previousManifestExists)
            {
                Write-Warning (
                    "Restoring previous manifest..."
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
                        "Previous manifest restored."
                    )
                }
                catch
                {
                    Write-Warning (
                        "Previous manifest could not " +
                        "be restored automatically."
                    )
                }
            }
            else
            {
                Write-Warning (
                    "No previous manifest exists. " +
                    "Removing failed new manifest..."
                )

                try
                {
                    Invoke-Wrangler `
                        -Arguments @(
                            "r2",
                            "object",
                            "delete",
                            "$R2Bucket/update-manifest.json",
                            "--remote"
                        )

                    Write-Host (
                        "Failed manifest removed."
                    )
                }
                catch
                {
                    Write-Warning (
                        "Failed manifest could not " +
                        "be removed automatically."
                    )
                }
            }

            throw $manifestException
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
    Write-Host (
        "Cloudflare R2 publication: SUCCESS"
    )

    Write-Host "Bucket   : $R2Bucket"
    Write-Host "Package  : $packageName"
    Write-Host (
        "Manifest : update-manifest.json"
    )
}
elseif (
    [string]::IsNullOrWhiteSpace(
        $downloadUrl))
{
    Write-Host ""
    Write-Host (
        "NOTE: Download URL was not configured."
    )

    Write-Host (
        "Provide -DownloadBaseUrl when " +
        "building a production release."
    )
}
else
{
    Write-Host ""
    Write-Host (
        "NOTE: R2 publication was not requested."
    )

    Write-Host (
        "Use -PublishToR2 when the release " +
        "is ready to be published."
    )
}

Write-Host ""