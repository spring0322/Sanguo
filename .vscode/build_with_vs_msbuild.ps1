param(
    [Parameter(Mandatory=$true)][string]$SolutionPath,
    [Parameter(Mandatory=$true)][string]$Configuration,
    [Parameter(Mandatory=$false)][string]$Platform = 'x64'
)

$pf86 = [System.Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
$vswhere = Join-Path $pf86 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    Write-Error "vswhere.exe not found at $vswhere"
    exit 1
}

$msbuild = & "$vswhere" -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) {
    Write-Error 'MSBuild.exe not found via vswhere'
    exit 1
}

& "$msbuild" "$SolutionPath" /m /restore /t:Build "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/v:m"
exit $LASTEXITCODE


