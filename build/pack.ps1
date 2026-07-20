# SPDX-License-Identifier: GPL-3.0-or-later
<#
.SYNOPSIS
  Packs the CollectivePlatform libraries into the offline NuGet feed (feed/).
.DESCRIPTION
  The product repos consume these packages from a committed local folder feed, so the shared
  library works fully offline (no NuGet server) — consistent with the stack's no-server stance.
  After bumping the version in Directory.Build.props, run this and re-vendor into consumers.
.EXAMPLE
  ./build/pack.ps1
#>
param([string]$Configuration = "Release")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$feed = Join-Path $root "feed"
New-Item -ItemType Directory -Force $feed | Out-Null

dotnet pack (Join-Path $root "src/Collective.Platform.Abstractions/Collective.Platform.Abstractions.csproj") -c $Configuration -o $feed
dotnet pack (Join-Path $root "src/Collective.Platform/Collective.Platform.csproj") -c $Configuration -o $feed
dotnet pack (Join-Path $root "src/Collective.Platform.Controls/Collective.Platform.Controls.csproj") -c $Configuration -o $feed
dotnet pack (Join-Path $root "src/Collective.Platform.Secrets/Collective.Platform.Secrets.csproj") -c $Configuration -o $feed
dotnet pack (Join-Path $root "src/Collective.Platform.Testing/Collective.Platform.Testing.csproj") -c $Configuration -o $feed
dotnet pack (Join-Path $root "src/Collective.Update/Collective.Update.csproj") -c $Configuration -o $feed

Write-Host "Packed to $feed"
Get-ChildItem $feed -Filter *.nupkg | Format-Table Name, @{ Name = 'KB'; Expression = { [math]::Round($_.Length / 1KB, 1) } } -AutoSize
