<#
.SYNOPSIS
Runs one DewPad scenario inside the real game on a throwaway single player farm. Results and pictures land in testprofile\results.

.DESCRIPTION
The live Mods folder and your saves are never touched. The game gets its own small Mods folder under testprofile. The throwaway DPTest farm
is removed afterwards.

.PARAMETER Scenario
The scenario name the harness knows, such as sheet.

.PARAMETER WithDewPad
Also installs the mod itself, with the settings given in -Config.

.PARAMETER ConfigJson
Settings written to the mod config.json, as JSON text such as {"PromptStyle":"PlayStation"}.
#>
param(
    [Parameter(Mandatory)][string] $Scenario,
    [switch] $WithDewPad,
    [string] $ConfigJson = "",
    [int] $TimeoutSeconds = 420,
    [string] $GamePath = 'D:\SteamLibrary\steamapps\common\Stardew Valley'
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$work = Join-Path $repo 'testprofile'
$results = Join-Path $work 'results'
$mods = Join-Path $work 'Mods'
$logDir = Join-Path $env:APPDATA 'StardewValley\ErrorLogs'

if ($WithDewPad) {
    dotnet build (Join-Path $repo 'DewPad\DewPad.csproj') -c Release --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Mod build failed.' }
}
dotnet build (Join-Path $repo 'DewPad.DevHarness\DewPad.DevHarness.csproj') -c Debug --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Harness build failed.' }

New-Item -ItemType Directory -Force $results | Out-Null
Remove-Item (Join-Path $results "$Scenario-*") -ErrorAction SilentlyContinue
if (Test-Path $mods) { Remove-Item $mods -Recurse -Force }
New-Item -ItemType Directory $mods | Out-Null
Copy-Item (Join-Path $GamePath 'Mods\ConsoleCommands') (Join-Path $mods 'ConsoleCommands') -Recurse
$harness = Join-Path $mods 'DewPad.DevHarness'
New-Item -ItemType Directory $harness | Out-Null
Copy-Item (Join-Path $repo 'DewPad.DevHarness\bin\Debug\net6.0\DewPad.DevHarness.dll') $harness
Copy-Item (Join-Path $repo 'DewPad.DevHarness\manifest.json') $harness
if ($WithDewPad) {
    $own = Join-Path $mods 'DewPad'
    New-Item -ItemType Directory $own | Out-Null
    Copy-Item (Join-Path $repo 'DewPad\bin\Release\net6.0\DewPad.dll') $own
    Copy-Item (Join-Path $repo 'DewPad\manifest.json') $own
    if (Test-Path (Join-Path $repo 'DewPad\i18n')) { Copy-Item (Join-Path $repo 'DewPad\i18n') (Join-Path $own 'i18n') -Recurse }
    if ($ConfigJson) { Set-Content (Join-Path $own "config.json") $ConfigJson }
}

$prefs = Join-Path $env:APPDATA 'StardewValley\startup_preferences'
$prefsBackup = $null
if (Test-Path $prefs) { $prefsBackup = Get-Content $prefs -Raw }

$env:DPH_ONLY = $Scenario
$env:DPH_OUT = Join-Path $results "$Scenario-result.json"
$proc = Start-Process -FilePath (Join-Path $GamePath 'StardewModdingAPI.exe') -ArgumentList '--no-terminal', '--mods-path', "`"$mods`"" -WorkingDirectory $GamePath -PassThru
Remove-Item Env:DPH_ONLY, Env:DPH_OUT -ErrorAction SilentlyContinue

if (-not $proc.WaitForExit($TimeoutSeconds * 1000)) { Write-Warning "Timed out after $TimeoutSeconds seconds."; Stop-Process -Id $proc.Id -Force }

Get-ChildItem (Join-Path $env:APPDATA 'StardewValley\Saves') -Directory -Filter 'DPTest_*' -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force
if ($null -ne $prefsBackup -and (Get-Content $prefs -Raw) -ne $prefsBackup) { Set-Content $prefs $prefsBackup -NoNewline }
Copy-Item (Join-Path $logDir 'SMAPI-latest.txt') (Join-Path $results "$Scenario-SMAPI.txt") -Force
$f = Join-Path $results "$Scenario-result.json"
if (Test-Path $f) { Write-Host "Results: $f" } else { Write-Warning 'No results file.' }
