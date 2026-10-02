Set-StrictMode -Version Latest

function Assert-File {
    param([Parameter(Mandatory)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file not found: $Path"
    }
}

function Assert-SameHash {
    param(
        [Parameter(Mandatory)][string]$Source,
        [Parameter(Mandatory)][string]$Destination
    )
    $SourceHash = (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash
    $DestinationHash = (Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash
    if ($SourceHash -ne $DestinationHash) {
        throw "DLL verification failed: $Destination"
    }
}

function Get-ProjectMetadata {
    param(
        [Parameter(Mandatory)][string]$ProjectFile,
        [Parameter(Mandatory)][string]$Configuration
    )
    $Output = & dotnet msbuild $ProjectFile -nologo "-p:Configuration=$Configuration" '-getProperty:GameDir,Version,AssemblyName,TargetPath'
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not resolve project properties through MSBuild.'
    }
    return (($Output -join [Environment]::NewLine) | ConvertFrom-Json).Properties
}

function Invoke-FormatAndBuild {
    param(
        [Parameter(Mandatory)][string]$ProjectFile,
        [Parameter(Mandatory)][string]$Configuration
    )
    Write-Host 'Formatting...'
    & dotnet tool run csharpier format .
    if ($LASTEXITCODE -ne 0) {
        throw 'CSharpier failed. Run dotnet tool restore if the formatter is missing.'
    }

    Write-Host 'Building...'
    & dotnet build $ProjectFile -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw 'Build failed.'
    }
}

function Assert-VersionConsistency {
    param(
        [Parameter(Mandatory)][string]$Dll,
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][string]$PluginSourcePath
    )
    Assert-File $Dll
    $ExpectedAssemblyVersion = "$Version.0"
    $AssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($Dll).Version.ToString()
    $FileVersion = (Get-Item -LiteralPath $Dll).VersionInfo.FileVersion
    if ($AssemblyVersion -ne $ExpectedAssemblyVersion -or $FileVersion -ne $ExpectedAssemblyVersion) {
        throw "DLL version mismatch. Project=$Version, Assembly=$AssemblyVersion, File=$FileVersion"
    }

    $PluginSource = Get-Content -LiteralPath $PluginSourcePath -Raw
    if ($PluginSource -notmatch 'const\s+string\s+PluginVersion\s*=\s*"([^"]+)"' -or $Matches[1] -ne $Version) {
        throw 'Plugin.PluginVersion must match the project version.'
    }
    Write-Host "Verified DLL version: $AssemblyVersion"
}

function Install-LocalPlugin {
    param(
        [Parameter(Mandatory)][string]$GameDir,
        [Parameter(Mandatory)][string]$AssemblyName,
        [Parameter(Mandatory)][string]$Dll,
        [Parameter(Mandatory)][string]$DllName,
        [Parameter(Mandatory)][string]$ArtifactsRoot,
        [Parameter(Mandatory)][string]$FontBundle,
        [Parameter(Mandatory)][string]$FontLicense
    )
    Assert-File (Join-Path $GameDir 'BepInEx\core\BepInEx.dll')
    $PluginRoot = Join-Path $GameDir 'BepInEx\plugins'
    $LocalPluginDir = Join-Path $PluginRoot $AssemblyName
    $LocalDll = Join-Path $LocalPluginDir $DllName
    $LegacyLocalDll = Join-Path $PluginRoot $DllName
    $BackupDir = Join-Path $ArtifactsRoot ('install-backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N'))

    foreach ($ExistingDll in @($LocalDll, $LegacyLocalDll)) {
        if (Test-Path -LiteralPath $ExistingDll -PathType Leaf) {
            New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
            $BackupName = if ($ExistingDll -eq $LegacyLocalDll) { "legacy-$DllName" } else { $DllName }
            Copy-Item -LiteralPath $ExistingDll -Destination (Join-Path $BackupDir $BackupName)
        }
    }

    New-Item -ItemType Directory -Path $LocalPluginDir -Force | Out-Null
    $LocalAssetsDir = Join-Path $LocalPluginDir 'assets'
    $LocalLicensesDir = Join-Path $LocalPluginDir 'LICENSES'
    New-Item -ItemType Directory -Path $LocalLicensesDir -Force | Out-Null
    Copy-Item -LiteralPath $FontLicense -Destination (Join-Path $LocalLicensesDir 'AlegreyaSans-OFL.txt')
    if (Test-Path -LiteralPath $FontBundle -PathType Leaf) {
        New-Item -ItemType Directory -Path $LocalAssetsDir -Force | Out-Null
        Copy-Item -LiteralPath $FontBundle -Destination (Join-Path $LocalAssetsDir 'knownformulahelper_fonts.bundle')
    }

    $SwapId = [guid]::NewGuid().ToString('N')
    $StagedDll = "$LocalDll.installing-$SwapId"
    $PreviousDll = "$LocalDll.previous-$SwapId"
    Copy-Item -LiteralPath $Dll -Destination $StagedDll
    Assert-SameHash $Dll $StagedDll
    $PreviousMoved = $false
    $NewMoved = $false
    try {
        if (Test-Path -LiteralPath $LocalDll -PathType Leaf) {
            Move-Item -LiteralPath $LocalDll -Destination $PreviousDll
            $PreviousMoved = $true
        }
        Move-Item -LiteralPath $StagedDll -Destination $LocalDll
        $NewMoved = $true
        Assert-SameHash $Dll $LocalDll
    }
    catch {
        if ($NewMoved) { Remove-Item -LiteralPath $LocalDll -Force }
        if ($PreviousMoved) { Move-Item -LiteralPath $PreviousDll -Destination $LocalDll }
        throw
    }
    finally {
        if (Test-Path -LiteralPath $StagedDll -PathType Leaf) {
            Remove-Item -LiteralPath $StagedDll -Force
        }
    }

    if ($PreviousMoved) {
        try { Remove-Item -LiteralPath $PreviousDll -Force }
        catch { Write-Warning "The running game still holds the old DLL. After closing it, remove: $PreviousDll" }
    }
    if (Test-Path -LiteralPath $LegacyLocalDll -PathType Leaf) {
        Remove-Item -LiteralPath $LegacyLocalDll -Force
    }

    Write-Host "Installed and SHA256 verified: $LocalDll"
    if (Test-Path -LiteralPath $BackupDir) {
        Write-Host "Previous DLL backup: $BackupDir"
    }
}
