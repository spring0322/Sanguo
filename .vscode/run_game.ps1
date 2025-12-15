param(
    [Parameter(Mandatory = $true)][string]$ExePath
)

if (-not (Test-Path -LiteralPath $ExePath)) {
    Write-Error "Exe not found: $ExePath"
    exit 1
}

$workDir = Split-Path -Parent $ExePath
Start-Process -FilePath $ExePath -WorkingDirectory $workDir
exit $LASTEXITCODE


