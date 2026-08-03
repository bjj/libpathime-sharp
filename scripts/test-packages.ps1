<#
.SYNOPSIS
Pack the real packages, restore them into a throwaway consumer, and prove
the result runs — as a build, as a RID publish, and as a RID-less publish.

.DESCRIPTION
CI's dotnet test runs against the staged native tree; users get .nupkg
files. This script closes that gap and is what CI and the release workflow
both run (the release, before the NuGet push gate):

  1. dotnet pack PathimeSharp and nuget pack the RID's NativeAssets nuspec
     into a local feed (the staged tree under artifacts/nuget/<rid> must
     exist — run the shared native action or stage-native first).
  2. Create a console app that references ONLY the NativeAssets package:
     PathimeSharp must arrive through its dependency.
  3. dotnet run, dotnet publish -r <rid>, and RID-less dotnet publish; run
     all three outputs and require all five engines plus a typed
     commit — and require pathime-data/ to have survived the RID publish's
     native-asset flattening (the buildTransitive targets' job).

.EXAMPLE
scripts\test-packages.ps1 -Rid win-x64
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("win-x64", "linux-x64")]
    [string]$Rid,

    [string]$Version = "0.0.1-local"
)

$ErrorActionPreference = "Stop"
function Invoke-Checked([scriptblock]$Step) {
    & $Step
    if ($LASTEXITCODE -ne 0) { throw "failed (exit $LASTEXITCODE): $Step" }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$work = Join-Path $repoRoot "artifacts/consumer"
$feed = Join-Path $work "feed"
$app = Join-Path $work "app"
if (Test-Path $work) { Remove-Item -Recurse -Force $work }
New-Item -ItemType Directory -Force $feed, $app | Out-Null

if (-not (Test-Path (Join-Path $repoRoot "artifacts/nuget/$Rid/native"))) {
    throw "No staged tree under artifacts/nuget/$Rid - stage natives first."
}

Write-Host "== Pack into the local feed ($Version)"
Invoke-Checked { dotnet pack (Join-Path $repoRoot "src/PathimeSharp/PathimeSharp.csproj") `
    -c Release -p:Version=$Version -o $feed -v q }
Invoke-Checked { nuget pack (Join-Path $repoRoot "packaging/PathimeSharp.NativeAssets.$Rid/PathimeSharp.NativeAssets.$Rid.nuspec") `
    -Version $Version -Properties version=$Version -OutputDirectory $feed -NoDefaultExcludes }

Write-Host "== Create the consumer"
Push-Location $app
try {
    Invoke-Checked { dotnet new console --framework net8.0 --name Consumer -v q }
    Set-Location Consumer
    # The local feed first; nuget.org stays available for the console
    # template's implicit bits.
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Out-File -Encoding utf8 nuget.config

    @'
using System;
using System.Linq;
using PathimeSharp;

Pathime.Init(dataDir: Environment.GetEnvironmentVariable("CONSUMER_DATA_DIR"));
try
{
    var missing = Enum.GetValues<EngineId>().Where(id => !Pathime.HasEngine(id)).ToList();
    if (missing.Count > 0)
        throw new Exception("engines missing: " + string.Join(", ", missing));

    using var engine = new Engine(EngineId.Pinyin);
    using var context = new Context(engine);
    context.Type("nihao");
    context.SelectCandidate(0);
    string committed = context.TakeCommitted();
    if (string.IsNullOrEmpty(committed))
        throw new Exception("nothing committed");
    Console.WriteLine($"OK {Pathime.Version}: 5/5 engines, committed '{committed}'");
}
finally
{
    Pathime.Shutdown();
}
'@ | Out-File -Encoding utf8 Program.cs

    # Only the native package; PathimeSharp must arrive transitively.
    Invoke-Checked { dotnet add package "PathimeSharp.NativeAssets.$Rid" --version $Version }

    $dataDir = Join-Path $work "userdata"
    $env:CONSUMER_DATA_DIR = $dataDir

    Write-Host "== dotnet run"
    Invoke-Checked { dotnet run -c Release -v q }

    Write-Host "== dotnet publish -r $Rid"
    Invoke-Checked { dotnet publish -c Release -r $Rid --self-contained false -o publish-rid -v q }
    if (-not (Test-Path "publish-rid/pathime-data")) {
        throw "pathime-data/ did not survive the RID publish - buildTransitive targets broke"
    }
    Invoke-Checked { dotnet publish-rid/Consumer.dll }

    Write-Host "== RID-less dotnet publish"
    Invoke-Checked { dotnet publish -c Release -o publish-portable -v q }
    Invoke-Checked { dotnet publish-portable/Consumer.dll }

    Write-Host "== Consumer test passed"
}
finally {
    Pop-Location
    Remove-Item Env:\CONSUMER_DATA_DIR -ErrorAction SilentlyContinue
}
