<#
.SYNOPSIS
Stage a local libpathime CMake install into this repo's package layouts.

.DESCRIPTION
Copies the native libraries and the pathime-data dictionary tree from a
`cmake --install` prefix into (any of) three git-ignored destinations:

  nuget  -> artifacts/nuget/<rid>/{native,data/pathime-data}   (nuspec source root)
  unity  -> unity/com.ben.pathime/Plugins/<platform>/x86_64/    (+ pathime-data~)
  tests  -> artifacts/native/<rid>/                             (test fallback probe)

Binaries and data are never committed; run this after building libpathime
(see README.md — note that a bare build tree stages no pathime-data, only
`cmake --install` does).

.EXAMPLE
scripts\stage-native.ps1 -Prefix C:\dev\pathime-dist
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Prefix,

    [ValidateSet("win-x64")]
    [string]$Rid = "win-x64",

    [ValidateSet("nuget", "unity", "tests")]
    [string[]]$Targets = @("nuget", "unity", "tests")
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

$bin = Join-Path $Prefix "bin"
$libraryPath = Join-Path $bin "pathime.dll"
$dataPath = Join-Path $bin "pathime-data"

if (-not (Test-Path $libraryPath)) {
    throw "No pathime.dll under '$bin'. Point -Prefix at a CMake install prefix (cmake --install ... --prefix <Prefix>)."
}
if (-not (Test-Path $dataPath)) {
    throw "No pathime-data under '$bin'. A bare build tree stages no dictionaries - run 'cmake --install'."
}

$dlls = Get-ChildItem $bin -Filter *.dll

function Stage-Flat([string]$destination, [string]$dataDirName) {
    if (Test-Path $destination) {
        Remove-Item -Recurse -Force $destination
    }
    New-Item -ItemType Directory -Force $destination | Out-Null
    Copy-Item $dlls.FullName $destination
    Copy-Item -Recurse $dataPath (Join-Path $destination $dataDirName)
    $bytes = (Get-ChildItem -Recurse -File $destination | Measure-Object Length -Sum).Sum
    "Staged from $Prefix at $(Get-Date -Format o): $($dlls.Count) DLLs + pathime-data, $([math]::Round($bytes / 1MB, 1)) MB" |
        Out-File -Encoding utf8 (Join-Path $destination "STAGED.txt")
    Write-Host ("  {0}  ({1:N1} MB)" -f $destination, ($bytes / 1MB))
}

foreach ($target in $Targets) {
    switch ($target) {
        "nuget" {
            $root = Join-Path $repoRoot "artifacts\nuget\$Rid"
            if (Test-Path $root) { Remove-Item -Recurse -Force $root }
            $native = Join-Path $root "native"
            New-Item -ItemType Directory -Force $native | Out-Null
            Copy-Item $dlls.FullName $native
            $data = Join-Path $root "data"
            New-Item -ItemType Directory -Force $data | Out-Null
            Copy-Item -Recurse $dataPath (Join-Path $data "pathime-data")
            # The package ships the licence text of everything it contains,
            # as the install prefix does (share/doc/pathime). Installs older
            # than libpathime v0.1.0 have no licences directory; refuse them
            # rather than stage a package payload with no notices.
            $docDir = Join-Path $Prefix "share\doc\pathime"
            if (-not (Test-Path (Join-Path $docDir "licenses"))) {
                throw "No licences under '$docDir' - the nuget layout needs a libpathime >= v0.1.0 install."
            }
            $lic = Join-Path $root "licenses"
            New-Item -ItemType Directory -Force $lic | Out-Null
            Copy-Item (Join-Path $docDir "licenses\*") $lic
            Copy-Item (Join-Path $docDir "LICENSE") (Join-Path $lic "libpathime.txt")
            Write-Host "  $root"
        }
        "unity" {
            # Trailing ~ hides the data folder from Unity's asset importer.
            Stage-Flat (Join-Path $repoRoot "unity\com.ben.pathime\Plugins\Windows\x86_64") "pathime-data~"
        }
        "tests" {
            Stage-Flat (Join-Path $repoRoot "artifacts\native\$Rid") "pathime-data"
        }
    }
}

Write-Host ""
Write-Host "To use this build directly:"
Write-Host "  `$env:PATHIME_LIBRARY = `"$libraryPath`""
