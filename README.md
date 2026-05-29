# SAPuccino — Hack the Open: Desafio Café Tracker

Monitora em tempo real uma máquina de café via **MQTT** (indicador `H1`) e mostra o
**status da máquina**, os **cafés do dia** e a **última utilização**.

- **Backend** — .NET 10 (arquitetura DDD em camadas), assina o broker MQTT e publica
  atualizações ao vivo via **SignalR**.
- **Frontend** — SAPUI5, mostra os dados e atualiza em tempo real.

## Estrutura do repositório

```
CoffeeTracker.Api    -> Backend .NET 10 (Domain / Application / Infrastructure / Endpoints)
CoffeeTracker.UI     -> Frontend SAPUI5
CoffeeTracker.Tests  -> Testes (estrutura; desenvolvidos pela equipe de QA)
docker-compose.yml   -> PostgreSQL (opcional, para histórico)
```

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org) e npm
- (Opcional) [Docker](https://www.docker.com/) para subir o PostgreSQL

## Clonar o projeto

```bash
git clone https://github.com/grGrimales/SAPuccino.git
cd SAPuccino
```

## Início rápido (rodar localmente)

Use **dois terminais**, a partir da raiz do projeto:

**Terminal 1 — Backend** (http://localhost:5036):

```bash
cd CoffeeTracker.Api
dotnet run --launch-profile http
```

**Terminal 2 — Frontend** (http://localhost:8080):

```bash
cd CoffeeTracker.UI
npm install
npx ui5 serve --port 8080
```

Depois abra **http://localhost:8080/index.html** no navegador.

> Detalhes (credenciais MQTT, endpoints, PostgreSQL) nas seções abaixo.

## Backend — CoffeeTracker.Api

Roda em **http://localhost:5036**.

```bash
cd CoffeeTracker.Api
dotnet run --launch-profile http
```

### Credenciais do broker MQTT (não ficam no repositório)

As credenciais ficam em **.NET User Secrets** (fora do Git). Peça os valores ao time e configure:

```bash
cd CoffeeTracker.Api
dotnet user-secrets set "Mqtt:Broker"   "<host-do-broker>"
dotnet user-secrets set "Mqtt:Port"     "1883"
dotnet user-secrets set "Mqtt:Username" "<usuario>"
dotnet user-secrets set "Mqtt:Password" "<senha>"
```

O topic da máquina já está em `appsettings.json` (`/IoT/SAACE/DADOSAPONTAMENTO`).

### Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| GET  | `/api/coffee/status`   | Estado (online/offline), cafés de hoje e última utilização |
| GET  | `/api/coffee/events`   | Histórico de cafés |
| POST | `/api/coffee/simulate` | Simula um café (útil para a demo sem a máquina) |
| —    | `/hubs/coffee`         | Hub SignalR (evento `statusUpdated`) |

### (Opcional) PostgreSQL para histórico

Sem banco o app funciona em memória (o tempo real continua). Para persistir o histórico:

```bash
docker-compose up -d
```

## Frontend — CoffeeTracker.UI

Roda em **http://localhost:8080** (rode o backend antes; o FE consome `http://localhost:5036`).

```bash
cd CoffeeTracker.UI
npm install
npx ui5 serve --port 8080
```

Abra **http://localhost:8080/index.html** no navegador.

## Testar o tempo real

Com o frontend aberto, simule um café e veja os dados atualizarem sem recarregar:

```bash
curl -X POST http://localhost:5036/api/coffee/simulate
```
