sap.ui.define([], function () {
    "use strict";

    // Centraliza toda a comunicação com o backend (REST + tempo real via SignalR).
    const API_BASE = "http://localhost:5036";

    return {
        // Busca o status atual da máquina (estado, cafés de hoje, última utilização).
        getStatus: function () {
            return fetch(API_BASE + "/api/coffee/status").then(function (response) {
                if (!response.ok) {
                    throw new Error("HTTP " + response.status);
                }
                return response.json();
            });
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
