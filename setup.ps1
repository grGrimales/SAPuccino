<#
.SYNOPSIS
    Setup automatizado do ambiente SAPuccino (Café Tracker).
.DESCRIPTION
    Verifica pré-requisitos, sobe o PostgreSQL (Docker ou nativo), configura
    as credenciais MQTT e do banco via User Secrets e instala as dependências
    do backend (.NET) e do frontend (SAPUI5).
.EXAMPLE
    .\setup.ps1
    Setup interativo (pergunta a senha do MQTT e, se nativo, do Postgres).
.EXAMPLE
    .\setup.ps1 -DbMode docker -SkipMqtt
    Usa Docker para o banco e pula a configuração do MQTT (máquina ficará offline).
#>
[CmdletBinding()]
param(
    [ValidateSet("auto", "docker", "native", "none")]
    [string]$DbMode = "auto",

    [string]$PgSuperUser = "postgres",
    [string]$PgSuperPassword,
    [int]$PgPort = 5432,

    [string]$MqttBroker,
    [int]$MqttPort = 1883,
    [string]$MqttUser,
    [string]$MqttPassword,
    [switch]$SkipMqtt
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$apiDir = Join-Path $root "CoffeeTracker.Api"
$uiDir = Join-Path $root "CoffeeTracker.UI"

function Write-Step { param($m) Write-Host "`n=== $m ===" -ForegroundColor Cyan }
function Write-Ok   { param($m) Write-Host "  [OK] $m" -ForegroundColor Green }
function Write-Warn { param($m) Write-Host "  [!]  $m" -ForegroundColor Yellow }
function Write-Err  { param($m) Write-Host "  [X]  $m" -ForegroundColor Red }

# Garante que ferramentas recém-instaladas (ex.: dotnet) entrem no PATH desta sessão.
$env:Path = [Environment]::GetEnvironmentVariable("Path", "Machine") + ";" +
            [Environment]::GetEnvironmentVariable("Path", "User")

# ---------------------------------------------------------------------------
Write-Step "1/5 Verificando pré-requisitos"

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Err ".NET SDK não encontrado. Instale o SDK 10: https://dotnet.microsoft.com/download"
    exit 1
}
$has10 = (& dotnet --list-sdks) | Where-Object { $_ -match "^10\." }
if ($has10) { Write-Ok ".NET SDK 10 encontrado" }
else {
    Write-Err "O projeto exige .NET 10. SDKs instalados:`n$(& dotnet --list-sdks)"
    Write-Warn "Instale com: winget install Microsoft.DotNet.SDK.10"
    exit 1
}

$node = Get-Command node -ErrorAction SilentlyContinue
if ($node) { Write-Ok "Node.js encontrado ($(& node --version))" }
else {
    Write-Err "Node.js não encontrado (necessário para o frontend SAPUI5)."
    Write-Warn "Instale com: winget install OpenJS.NodeJS.LTS"
    exit 1
}

# ---------------------------------------------------------------------------
Write-Step "2/5 Configurando o banco de dados (PostgreSQL)"

# Resolve o modo de banco quando 'auto': prefere Docker, depois Postgres nativo.
$dockerCmd = Get-Command docker -ErrorAction SilentlyContinue
$psql = Get-ChildItem "C:\Program Files\PostgreSQL\*\bin\psql.exe" -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName

if ($DbMode -eq "auto") {
    if ($dockerCmd)  { $DbMode = "docker" }
    elseif ($psql)   { $DbMode = "native" }
    else             { $DbMode = "none" }
    Write-Ok "Modo de banco detectado: $DbMode"
}

switch ($DbMode) {
    "docker" {
        if (-not $dockerCmd) { Write-Err "Docker não encontrado, mas -DbMode docker foi pedido."; exit 1 }
        Write-Host "  Subindo container do Postgres via docker-compose..."
        Push-Location $root
        try { & docker compose up -d } catch { & docker-compose up -d }
        Pop-Location
        # appsettings.json já aponta para a porta 55432 do compose; remove override que possa conflitar.
        Push-Location $apiDir
        try { & dotnet user-secrets remove "ConnectionStrings:Postgres" 2>$null } catch {}
        Pop-Location
        Write-Ok "Postgres (Docker) no ar na porta 55432"
    }
    "native" {
        if (-not $psql) { Write-Err "psql não encontrado. Instale com: winget install PostgreSQL.PostgreSQL.16"; exit 1 }
        if (-not $PgSuperPassword) {
            $sec = Read-Host "  Senha do superusuário '$PgSuperUser' do Postgres" -AsSecureString
            $PgSuperPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
                [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec))
        }
        $env:PGPASSWORD = $PgSuperPassword
        Write-Host "  Criando role 'coffee_user' e database 'coffee_tracker' (se não existirem)..."
        $createRole = "DO `$`$ BEGIN IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname='coffee_user') THEN CREATE ROLE coffee_user LOGIN PASSWORD 'coffee_pass'; END IF; END `$`$;"
        & $psql -U $PgSuperUser -h 127.0.0.1 -p $PgPort -v ON_ERROR_STOP=1 -c $createRole | Out-Null
        $dbExists = (& $psql -U $PgSuperUser -h 127.0.0.1 -p $PgPort -t -c "SELECT 1 FROM pg_database WHERE datname='coffee_tracker'") -match "1"
        if (-not $dbExists) {
            & $psql -U $PgSuperUser -h 127.0.0.1 -p $PgPort -c "CREATE DATABASE coffee_tracker OWNER coffee_user" | Out-Null
        }
        # Aponta a API para a porta nativa via User Secrets (sem alterar o appsettings.json).
        Push-Location $apiDir
        & dotnet user-secrets set "ConnectionStrings:Postgres" "Host=127.0.0.1;Port=$PgPort;Database=coffee_tracker;Username=coffee_user;Password=coffee_pass" | Out-Null
        Pop-Location
        Write-Ok "Postgres nativo configurado (porta $PgPort, db coffee_tracker)"
    }
    "none" {
        Write-Warn "Nenhum banco configurado. A app roda em memória (sem histórico persistente)."
        Write-Warn "Para histórico: instale Docker ou PostgreSQL e rode novamente."
    }
}

# ---------------------------------------------------------------------------
Write-Step "3/5 Configurando credenciais do MQTT"

if ($SkipMqtt) {
    Write-Warn "MQTT pulado (-SkipMqtt). A máquina aparecerá 'offline'; use /api/coffee/simulate para testar."
}
else {
    # Nada de credenciais no repositório: broker, usuário e senha são informados aqui
    # (ou via parâmetros) e guardados apenas em User Secrets, fora do Git.
    if (-not $MqttBroker) { $MqttBroker = Read-Host "  Broker MQTT (host; Enter em branco para pular)" }
    if (-not $MqttUser)   { $MqttUser = Read-Host "  Usuário MQTT" }
    if (-not $MqttPassword) {
        $sec = Read-Host "  Senha do broker MQTT" -AsSecureString
        $MqttPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec))
    }
    if ([string]::IsNullOrWhiteSpace($MqttBroker) -or [string]::IsNullOrWhiteSpace($MqttPassword)) {
        Write-Warn "MQTT incompleto. A máquina ficará 'offline' (a simulação continua funcionando)."
    }
    else {
        Push-Location $apiDir
        & dotnet user-secrets set "Mqtt:Broker"   $MqttBroker   | Out-Null
        & dotnet user-secrets set "Mqtt:Port"     "$MqttPort"   | Out-Null
        & dotnet user-secrets set "Mqtt:Username" $MqttUser     | Out-Null
        & dotnet user-secrets set "Mqtt:Password" $MqttPassword | Out-Null
        Pop-Location
        Write-Ok "MQTT configurado (broker $MqttBroker`:$MqttPort)"
    }
}

# ---------------------------------------------------------------------------
Write-Step "4/5 Restaurando dependências do backend (.NET)"
Push-Location $apiDir
& dotnet restore | Out-Null
Pop-Location
Write-Ok "Backend restaurado"

# ---------------------------------------------------------------------------
Write-Step "5/5 Instalando dependências do frontend (SAPUI5)"
Push-Location $uiDir
& npm install --no-fund --no-audit
Pop-Location
Write-Ok "Frontend instalado"

# ---------------------------------------------------------------------------
Write-Host "`n=== Setup concluído! ===" -ForegroundColor Green
Write-Host "Para subir tudo:    .\start.ps1"
Write-Host "Só backend:         cd CoffeeTracker.Api ; dotnet run --launch-profile http   (http://localhost:5036)"
Write-Host "Só frontend:        cd CoffeeTracker.UI  ; npx ui5 serve --port 8080          (http://localhost:8080)"
