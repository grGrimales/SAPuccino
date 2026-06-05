# SAPuccino — Contrato da API (Backend → Frontend)

Documento de referência para o desenvolvimento do frontend (SAPUI5).
Backend: **.NET 10** · arquitetura DDD · tempo real via **SignalR**.

## Base

- **Base URL (dev):** `http://localhost:5036`
- **CORS liberado para:** `http://localhost:8080` e `http://localhost:8030`
- **Swagger (dev):** `http://localhost:5036/swagger`
- **Datas/horas:** todos os campos `...Utc` vêm em **ISO 8601 UTC** (ex.: `2026-06-05T18:09:05Z`). Converta para o fuso local no frontend.
- **Agregações** (dia, hora, pico) já são calculadas no backend no fuso **America/Sao_Paulo**.

---

## Tempo real (SignalR)

- **Hub:** `http://localhost:5036/hubs/coffee`
- **Evento escutado:** `statusUpdated`
- **Payload:** o mesmo objeto de `GET /api/coffee/status` (ver abaixo).
- Disparado quando: chega um café novo, a máquina entra/sai de "em uso", ou a conexão com o broker muda.

Exemplo (cliente JS):
```js
const conn = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5036/hubs/coffee")
  .withAutomaticReconnect()
  .build();
conn.on("statusUpdated", (status) => { /* status = CoffeeStatus */ });
conn.start();
```

---

## Endpoints REST

### 1. `GET /api/coffee/status`
Estado vivo da máquina (use junto com o SignalR).

| Campo | Tipo | Descrição |
|------|------|-----------|
| `machineState` | string | `"online"` ou `"offline"` (conexão com o broker MQTT) |
| `inUse` | boolean | `true` enquanto a máquina está preparando um café (H1 = 1) |
| `coffeesToday` | int | Cafés detectados hoje (fuso local) |
| `lastUsedUtc` | string \| null | Momento do último café (UTC), ou `null` |

```json
{ "machineState": "online", "inUse": false, "coffeesToday": 10, "lastUsedUtc": "2026-06-05T18:09:05Z" }
```

> Sugestão de uso no Monitor: `offline` → cinza; `inUse` → vermelho ("Em uso"); senão → verde ("Disponível").

---

### 2. `GET /api/coffee/dashboard`
KPIs do dia (derivados do histórico). Ideal para os cartões do Monitor.

| Campo | Tipo | Descrição |
|------|------|-----------|
| `date` | string `yyyy-MM-dd` | Data local de referência |
| `coffeesToday` | int | Cafés de hoje |
| `coffeesYesterday` | int | Cafés de ontem (mesmo intervalo) |
| `changeVsYesterdayPercent` | number \| null | Variação % vs ontem; `null` se ontem = 0 |
| `averagePerHour` | number | Média de cafés/hora desde o 1º café do dia |
| `averageIntervalSeconds` | int \| null | Intervalo médio entre cafés de hoje (s); `null` se < 2 cafés |
| `lastUsedUtc` | string \| null | Último café (UTC) |
| `secondsSinceLastCoffee` | int \| null | Segundos desde o último café |
| `peakHourToday` | objeto \| null | `{ "hour": 0–23, "count": int }` — horário de pico de hoje |

```json
{
  "date": "2026-06-05",
  "coffeesToday": 10,
  "coffeesYesterday": 0,
  "changeVsYesterdayPercent": null,
  "averagePerHour": 2.9,
  "averageIntervalSeconds": 1340,
  "lastUsedUtc": "2026-06-05T18:09:05Z",
  "secondsSinceLastCoffee": 274,
  "peakHourToday": { "hour": 13, "count": 5 }
}
```

---

### 3. `GET /api/coffee/reports?days=N`
Relatório de consumo para gráficos. `days` opcional (padrão **7**, limitado a **90**).

| Campo | Tipo | Descrição |
|------|------|-----------|
| `fromDate` / `toDate` | string `yyyy-MM-dd` | Início/fim do período |
| `days` | int | Dias considerados |
| `totalCoffees` | int | Total no período |
| `averagePerDay` | number | Média por dia |
| `busiestDay` | objeto \| null | `{ "date": "yyyy-MM-dd", "count": int }` |
| `peakHour` | objeto \| null | `{ "hour": 0–23, "count": int }` |
| `daily` | array | Série por dia: `[{ "date": "yyyy-MM-dd", "count": int }]` (inclui dias com 0) |
| `hourly` | array | Distribuição por hora: `[{ "hour": 0–23, "count": int }]` (24 posições) |
| `lastUsedUtc` | string \| null | Último café no período |

```json
{
  "fromDate": "2026-05-30", "toDate": "2026-06-05", "days": 7,
  "totalCoffees": 42, "averagePerDay": 6,
  "busiestDay": { "date": "2026-06-01", "count": 29 },
  "peakHour": { "hour": 12, "count": 11 },
  "daily": [ { "date": "2026-05-30", "count": 0 }, { "date": "2026-06-05", "count": 10 } ],
  "hourly": [ { "hour": 0, "count": 0 }, { "hour": 13, "count": 5 } ],
  "lastUsedUtc": "2026-06-05T18:09:05Z"
}
```

---

### 4. `GET /api/coffee/availability?days=N`
Disponibilidade/uptime. `days` opcional (padrão **7**, limitado a **90**).

| Campo | Tipo | Descrição |
|------|------|-----------|
| `fromUtc` / `toUtc` | string (UTC) | Janela do relatório |
| `days` | int | Dias considerados |
| `currentState` | string | `"online"` ou `"offline"` |
| `currentSinceUtc` | string \| null | Desde quando o estado atual vigora |
| `uptimePercent` | number | % de tempo online no período |
| `totalOnlineSeconds` | int | Tempo total online (s) |
| `totalOfflineSeconds` | int | Tempo total offline (s) |
| `outages` | int | Número de quedas |
| `longestOutageSeconds` | int | Maior queda (s) |
| `longestOutageStartUtc` | string \| null | Início da maior queda |
| `transitions` | array | `[{ "occurredAtUtc": "UTC", "isOnline": bool }]` (linha do tempo) |

```json
{
  "fromUtc": "2026-05-29T03:00:00Z", "toUtc": "2026-06-05T18:13:00Z", "days": 7,
  "currentState": "online", "currentSinceUtc": "2026-06-05T11:00:00Z",
  "uptimePercent": 98.5, "totalOnlineSeconds": 600000, "totalOfflineSeconds": 9000,
  "outages": 2, "longestOutageSeconds": 6000, "longestOutageStartUtc": "2026-06-02T10:00:00Z",
  "transitions": [ { "occurredAtUtc": "2026-06-05T11:00:00Z", "isOnline": true } ]
}
```

---

### 5. `GET /api/coffee/events`
Histórico recente de cafés (até 50 por padrão).

```json
[ { "id": "719bcdd3-...", "occurredAtUtc": "2026-06-05T18:09:05Z" } ]
```

---

### 6. `POST /api/coffee/simulate`
Simula um café (útil para demo sem a máquina). Conta um café e dispara o `statusUpdated`.
Resposta: `202 Accepted`. *(Não altera o estado `inUse`, que depende do sinal real do H1.)*

---

## Observações para o frontend

- **Monitor:** combine `machineState` + `inUse` para a cor da faixa; use `/dashboard` para os cartões; atualize ao vivo pelo `statusUpdated`.
- **Detalhamento:** use `/reports` (gráficos por dia/hora) e `/availability` (uptime + linha do tempo), com seletor de período (`days=7|30|90`).
- **O que o backend NÃO fornece** (não há sensor): volume de água, agenda de limpeza, duração de preparo e status de "sucesso/falha" do café.
