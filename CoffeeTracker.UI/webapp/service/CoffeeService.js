sap.ui.define([], function () {
    "use strict";

    // Centraliza toda a comunicação com o backend (REST + tempo real via SignalR).
    const API_BASE = "http://localhost:5036";

    // Executa um GET e devolve o JSON, lançando erro em respostas não-OK.
    function getJson(path) {
        return fetch(API_BASE + path).then(function (response) {
            if (!response.ok) {
                throw new Error("HTTP " + response.status);
            }
            return response.json();
        });
    }

    return {
        // Busca o status atual da máquina (estado, em uso, cafés de hoje, última utilização).
        getStatus: function () {
            return getJson("/api/coffee/status");
        },

        // Busca os KPIs do dia (total, comparativo vs ontem, média horária, hora de pico).
        getDashboard: function () {
            return getJson("/api/coffee/dashboard");
        },

        // Busca o relatório de consumo do período (série por dia e por hora) para os gráficos.
        getReports: function (days) {
            return getJson("/api/coffee/reports?days=" + (days || 7));
        },

        // Busca o relatório de disponibilidade do período (uptime, quedas, linha do tempo).
        getAvailability: function (days) {
            return getJson("/api/coffee/availability?days=" + (days || 7));
        },

        // Busca o histórico recente de cafés (apenas horários; a máquina não informa o tipo).
        getEvents: function () {
            return getJson("/api/coffee/events");
        },

        // Assina o hub SignalR e repassa o status ao vivo e as mudanças de conexão via callbacks.
        connectRealtime: function (handlers) {
            const onStatus = (handlers && handlers.onStatus) || function () {};
            const onConnectionChange = (handlers && handlers.onConnectionChange) || function () {};

            if (!window.signalR) {
                onConnectionChange("Tempo real indisponível (SignalR não carregou)");
                return null;
            }

            const connection = new window.signalR.HubConnectionBuilder()
                .withUrl(API_BASE + "/hubs/coffee")
                .withAutomaticReconnect()
                .build();

            connection.on("statusUpdated", function (status) {
                onStatus(status);
            });

            connection.onreconnecting(function () {
                onConnectionChange("Reconectando…");
            });
            connection.onreconnected(function () {
                onConnectionChange("🟢 Tempo real conectado");
            });
            connection.onclose(function () {
                onConnectionChange("🔴 Tempo real desconectado");
            });

            connection.start()
                .then(function () {
                    onConnectionChange("🟢 Tempo real conectado");
                })
                .catch(function () {
                    onConnectionChange("🔴 Falha ao conectar ao tempo real");
                });

            return connection;
        }
    };
});
