param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'artifacts\releases')
)
$ErrorActionPreference = 'Stop'
$output = [IO.Path]::GetFullPath($OutputDirectory)
$assemblyInfo = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'PwM\Properties\AssemblyInfo.cs') -Raw
$versionMatch = [regex]::Match($assemblyInfo, '(?m)^\[assembly: AssemblyVersion\("(?<version>\d+\.\d+\.\d+(?:\.\d+)?)"\)\]')
if (-not $versionMatch.Success) { throw 'Cannot read PwM AssemblyVersion.' }
$version = [version]$versionMatch.Groups['version'].Value
$packageVersion = if ($version.Revision -gt 0) { $version.ToString(4) } else { $version.ToString(3) }
New-Item -ItemType Directory -Path $output -Force | Out-Null
# A fresh directory prevents stale output or personal files entering the application ZIP.
$publish = Join-Path $output ('publish-x64-' + [guid]::NewGuid().ToString('N'))
dotnet publish (Join-Path $PSScriptRoot 'PwM\PwM.csproj') -c Release -r win-x64 --self-contained true -o $publish -m:1 -nr:false -p:UseSharedCompilation=false -p:PublishSingleFile=false -p:PublishReadyToRun=true "-p:OutputPath=$output\build\" -v minimal
if ($LASTEXITCODE -ne 0) { throw 'PwM x64 publish failed.' }
foreach ($file in @('PwM.exe', 'PwM.dll', 'PwM.runtimeconfig.json', 'PwM.Updater.dll',
    'PwM.UpdateCleanup.exe', 'PwM.UpdateCleanup.dll', 'PwM.UpdateCleanup.runtimeconfig.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $publish $file))) { throw "A required application component is missing: $file" }
}
$asset = Join-Path $output "PwM-v${packageVersion}_x64_R2R.zip"
Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $asset -Force
Get-FileHash -LiteralPath $asset -Algorithm SHA256 | Select-Object Path, Hash
Write-Output "Upload $asset to the PwM GitHub release."
