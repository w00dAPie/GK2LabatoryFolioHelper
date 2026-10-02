Set-StrictMode -Version Latest

function New-ReleasePackages {
    param(
        [Parameter(Mandatory)][string]$ProjectRoot,
        [Parameter(Mandatory)][string]$ArtifactsRoot,
        [Parameter(Mandatory)][string]$AssemblyName,
        [Parameter(Mandatory)][string]$ProjectName,
        [Parameter(Mandatory)][string]$PackageName,
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][string]$Dll,
        [Parameter(Mandatory)][string]$DllName,
        [Parameter(Mandatory)][string]$Manifest,
        [Parameter(Mandatory)][string]$Readme,
        [Parameter(Mandatory)][string]$Changelog,
        [Parameter(Mandatory)][string]$Icon,
        [Parameter(Mandatory)][string]$FontBundle,
        [Parameter(Mandatory)][string]$FontLicense,
        [Parameter(Mandatory)][string]$ThunderstoreReleaseRoot,
        [Parameter(Mandatory)][string]$NexusReleaseRoot
    )
    $StagingRoot = Join-Path $ArtifactsRoot ('release-' + [guid]::NewGuid().ToString('N'))
    $ThunderstorePackage = Join-Path $StagingRoot 'thunderstore'
    $NexusPackage = Join-Path $StagingRoot 'nexus'
    $NexusPluginFolder = Join-Path $NexusPackage "BepInEx\plugins\$AssemblyName"
    $ThunderstorePluginFolder = Join-Path $ThunderstorePackage "plugins\$AssemblyName"

    foreach ($Directory in @($ThunderstorePluginFolder, $NexusPluginFolder, $ThunderstoreReleaseRoot, $NexusReleaseRoot)) {
        New-Item -ItemType Directory -Path $Directory -Force | Out-Null
    }
    foreach ($PluginFolder in @($ThunderstorePluginFolder, $NexusPluginFolder)) {
        New-Item -ItemType Directory -Path (Join-Path $PluginFolder 'assets') -Force | Out-Null
        New-Item -ItemType Directory -Path (Join-Path $PluginFolder 'LICENSES') -Force | Out-Null
    }

    Copy-Item -LiteralPath $Dll -Destination (Join-Path $ThunderstorePluginFolder $DllName)
    Copy-Item -LiteralPath $Dll -Destination (Join-Path $NexusPluginFolder $DllName)
    Copy-Item -LiteralPath $FontBundle -Destination (Join-Path $ThunderstorePluginFolder 'assets\knownformulahelper_fonts.bundle')
    Copy-Item -LiteralPath $FontBundle -Destination (Join-Path $NexusPluginFolder 'assets\knownformulahelper_fonts.bundle')
    Copy-Item -LiteralPath $FontLicense -Destination (Join-Path $ThunderstorePluginFolder 'LICENSES\AlegreyaSans-OFL.txt')
    Copy-Item -LiteralPath $FontLicense -Destination (Join-Path $NexusPluginFolder 'LICENSES\AlegreyaSans-OFL.txt')
    Copy-Item -LiteralPath $Manifest -Destination (Join-Path $ThunderstorePackage 'manifest.json')
    Copy-Item -LiteralPath $Icon -Destination (Join-Path $ThunderstorePackage 'icon.png')

    foreach ($Package in @($ThunderstorePackage, $NexusPackage)) {
        Copy-Item -LiteralPath $Readme -Destination (Join-Path $Package 'README.md')
        Copy-Item -LiteralPath (Join-Path $ProjectRoot 'LICENSE') -Destination (Join-Path $Package 'LICENSE')
        Copy-Item -LiteralPath (Join-Path $ProjectRoot 'docs') -Destination (Join-Path $Package 'docs') -Recurse
        if (Test-Path -LiteralPath $Changelog -PathType Leaf) {
            Copy-Item -LiteralPath $Changelog -Destination (Join-Path $Package 'CHANGELOG.md')
        }
    }

    Assert-SameHash $Dll (Join-Path $ThunderstorePluginFolder $DllName)
    Assert-SameHash $Dll (Join-Path $NexusPluginFolder $DllName)

    $ThunderstoreZip = Join-Path $ThunderstoreReleaseRoot "$PackageName-$Version.zip"
    $NexusZip = Join-Path $NexusReleaseRoot "$ProjectName-$Version.zip"
    Compress-Archive -Path (Join-Path $ThunderstorePackage '*') -DestinationPath $ThunderstoreZip -CompressionLevel Optimal -Force
    Compress-Archive -Path (Join-Path $NexusPackage '*') -DestinationPath $NexusZip -CompressionLevel Optimal -Force

    Write-Host "Thunderstore package: $ThunderstoreZip"
    Write-Host "Nexus package: $NexusZip"
}

function Publish-Thunderstore {
    param(
        [Parameter(Mandatory)][string]$ThunderstoreToml,
        [Parameter(Mandatory)][string]$TokenFile,
        [Parameter(Mandatory)][string]$Version
    )
    $TomlContent = Get-Content -LiteralPath $ThunderstoreToml -Raw
    $VersionPattern = '(?m)^(\s*versionNumber\s*=\s*)"[^"]+"([ \t]*\r?)$'
    if ([regex]::Matches($TomlContent, $VersionPattern).Count -ne 1) {
        throw 'Expected exactly one versionNumber in thunderstore.toml.'
    }
    $UpdatedToml = [regex]::Replace($TomlContent, $VersionPattern, ('${1}"' + $Version + '"${2}'))
    [System.IO.File]::WriteAllText($ThunderstoreToml, $UpdatedToml, [System.Text.UTF8Encoding]::new($false))

    $ThunderstoreToken = (Get-Content -LiteralPath $TokenFile -Raw).Trim()
    if ([string]::IsNullOrWhiteSpace($ThunderstoreToken)) {
        throw '.thunderstore-token is empty.'
    }
    try {
        & tcli publish --token $ThunderstoreToken
        if ($LASTEXITCODE -ne 0) {
            throw 'Thunderstore publish failed.'
        }
    }
    finally {
        $ThunderstoreToken = $null
    }
    Write-Host 'Thunderstore upload completed. Nexus requires manual upload.'
}
