sap.ui.define([
    "sap/ui/core/mvc/Controller",
    "sap/ui/model/json/JSONModel",
    "sapuccino/service/CoffeeService"
], function (Controller, JSONModel, CoffeeService) {
    "use strict";

    return Controller.extend("sapuccino.controller.App", {
        onInit: function () {
            this._model = new JSONModel({
                machineStateText: "—",
                coffeesToday: 0,
                lastUsedText: "—",
                connectionText: "Conectando ao tempo real…"
            });
            this.getView().setModel(this._model);

            const that = this;

            // Estado inicial via REST (no serviço).
            CoffeeService.getStatus()
                .then(function (status) {
                    that._applyStatus(status);
                })
                .catch(function () {
                    // O tempo real cuida das atualizações; ignoramos a falha do carregamento inicial.
                });

            // Atualizações ao vivo (no serviço).
            CoffeeService.connectRealtime({
                onStatus: function (status) {
                    that._applyStatus(status);
                },
                onConnectionChange: function (text) {
                    that._model.setProperty("/connectionText", text);
                }
            });
        },

        // Aplica o snapshot de status ao modelo.
        _applyStatus: function (status) {
            if (!status) {
                return;
            }
            this._model.setProperty("/machineStateText", status.machineState === "online" ? "Online ✅" : "Offline ❌");
            this._model.setProperty("/coffeesToday", status.coffeesToday);
            this._model.setProperty("/lastUsedText", this._formatDate(status.lastUsedUtc));
        },

        _formatDate: function (isoUtc) {
            if (!isoUtc) {
                return "—";
            }
            const date = new Date(isoUtc);
            return isNaN(date.getTime()) ? "—" : date.toLocaleString("pt-BR");
        }
    });
});
