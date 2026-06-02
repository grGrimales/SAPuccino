<#
.SYNOPSIS
    Sobe o backend (.NET) e o frontend (SAPUI5) do SAPuccino em janelas separadas.
.EXAMPLE
    .\start.ps1
    Backend em http://localhost:5036 e frontend em http://localhost:8080.
.EXAMPLE
    .\start.ps1 -UiPort 8030
    Usa a porta 8030 para o frontend (útil se a 8080 estiver ocupada).
#>
[CmdletBinding()]
param(
    [int]$UiPort = 8080
)

$root = $PSScriptRoot
$apiDir = Join-Path $root "CoffeeTracker.Api"
$uiDir = Join-Path $root "CoffeeTracker.UI"

# Garante o dotnet 10 no PATH das janelas filhas.
$pathRefresh = '$env:Path = [Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [Environment]::GetEnvironmentVariable("Path","User");'

Write-Host "Subindo backend  -> http://localhost:5036" -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command",
    "$pathRefresh Set-Location '$apiDir'; dotnet run --launch-profile http"

Write-Host "Subindo frontend -> http://localhost:$UiPort" -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command",
    "Set-Location '$uiDir'; npx ui5 serve --port $UiPort"

Write-Host "`nDuas janelas foram abertas. Acesse: " -NoNewline
Write-Host "http://localhost:$UiPort/index.html" -ForegroundColor Green
Write-Host "Para parar, feche as janelas ou use Ctrl+C em cada uma."
