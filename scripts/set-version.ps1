#Requires -Version 7.4
<#
.SYNOPSIS
    Stamps a version into Directory.Build.props (<Version>), which the app, the zips and the
    release all read. Used by the Release workflow.
.PARAMETER Version
    Semantic version without a leading "v", e.g. 1.2.0.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version
)

$ErrorActionPreference = 'Stop'
$props = Join-Path (Split-Path $PSScriptRoot -Parent) 'Directory.Build.props'
$text = [IO.File]::ReadAllText($props)
if ($text -notmatch '<Version>[^<]*</Version>') { throw "no <Version> element in $props" }
$new = [regex]::Replace($text, '<Version>[^<]*</Version>', "<Version>$Version</Version>")
# Same version is fine: the first release ships the version the file already carries.
if ($new -ne $text) { [IO.File]::WriteAllText($props, $new, [Text.UTF8Encoding]::new($false)) }
$check = ([xml](Get-Content -LiteralPath $props -Raw)).Project.PropertyGroup.Version
if ($check -ne $Version) { throw "Directory.Build.props reads back as $check, expected $Version" }
Write-Host "Directory.Build.props Version = $Version" -ForegroundColor Green
