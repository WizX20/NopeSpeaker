#Requires -Version 7.4
<#
.SYNOPSIS
    Publishes the app for one platform and zips it into dist/NopeSpeaker-<version>-win-<arch>.zip.
.OUTPUTS
    An object with the zip's path and SHA256.
#>
[CmdletBinding()]
param(
    [ValidateSet('x64', 'ARM64')]
    [string] $Platform = 'x64'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$rid = "win-$($Platform.ToLowerInvariant())"
$version = ([xml](Get-Content (Join-Path $root 'Directory.Build.props'))).Project.PropertyGroup.Version
$dist = Join-Path $root 'dist'
$out = Join-Path $dist "NopeSpeaker-$rid"

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
dotnet publish (Join-Path $root 'src/NopeSpeaker') -c Release -p:Platform=$Platform -r $rid --self-contained -o $out
if ($LASTEXITCODE) { throw "dotnet publish failed ($LASTEXITCODE)" }

$zip = Join-Path $dist "NopeSpeaker-$version-$rid.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $out '*') -DestinationPath $zip
[pscustomobject]@{ Zip = $zip; Sha256 = (Get-FileHash $zip -Algorithm SHA256).Hash }
