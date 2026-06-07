sap.ui.define([
    "sap/ui/core/mvc/Controller",
    "sap/ui/model/json/JSONModel",
    "sapuccino/service/CoffeeService"
], function (Controller, JSONModel, CoffeeService) {
    "use strict";

    return Controller.extend("sapuccino.controller.Monitor", {
        onInit: function () {
            this._model = new JSONModel({
                connectionText: "Conectando ao tempo real…",
                heroIcon: "☕",
                heroState: "—",
                heroSubtitle: "Carregando estado da máquina…",
                coffeesToday: 0,
                changeText: "",
                changeClass: "kpiCaption",
                lastUsedDate: "—",
                lastUsedTime: "",
                clockTime: "--:--:--",
                clockDate: "",
                systemActiveText: "",
                averagePerHour: 0,
                averageIntervalText: "—",
                peakHourText: "—",
                recentEvents: []
            });
            this.getView().setModel(this._model);

            // Relógio ao vivo (hora do navegador) atualizado a cada segundo.
            this._tickClock();
            this._clockTimer = setInterval(this._tickClock.bind(this), 1000);

            // Carga inicial via REST (estado + KPIs + histórico).
            this._refreshStatus();
            this._refreshDashboard();
            this._refreshEvents();

            // Atualizações ao vivo via SignalR (no serviço).
            this._connection = CoffeeService.connectRealtime({
                onStatus: this._applyStatus.bind(this),
                onConnectionChange: function (text) {
                    this._model.setProperty("/connectionText", text);
                }.bind(this)
            });
        },

        onExit: function () {
            if (this._clockTimer) {
                clearInterval(this._clockTimer);
            }
            if (this._connection && this._connection.stop) {
                this._connection.stop();
            }
        },

        onOpenDetalhamento: function () {
            this.getOwnerComponent().getRouter().navTo("detalhamento");
        },

        // ----- Cargas REST -----
        _refreshStatus: function () {
            CoffeeService.getStatus()
                .then(this._applyStatus.bind(this))
                .catch(function () { /* o tempo real assume as atualizações */ });
        },

        _refreshDashboard: function () {
            CoffeeService.getDashboard()
                .then(this._applyDashboard.bind(this))
                .catch(function () { /* sem banco os KPIs ficam zerados */ });
        },

        _refreshEvents: function () {
            CoffeeService.getEvents()
                .then(function (events) {
                    const items = (events || []).map(function (event) {
                        return { time: this._formatTime(event.occurredAtUtc) };
                    }.bind(this));
                    this._model.setProperty("/recentEvents", items);
                }.bind(this))
                .catch(function () { /* histórico depende do banco */ });
        },

        // ----- Aplicação dos dados ao modelo -----
        _applyStatus: function (status) {
            if (!status) {
                return;
            }

            const isOnline = status.machineState === "online";
            const isInUse = status.inUse === true;

            // Estado e cor da faixa: offline (cinza), em uso (vermelho), disponível (verde).
            let heroClass = "heroOffline";
            if (!isOnline) {
                this._model.setProperty("/heroIcon", "🔌");
                this._model.setProperty("/heroState", "OFFLINE");
                this._model.setProperty("/heroSubtitle", "Máquina desconectada");
                this._model.setProperty("/systemActiveText", "🔴 Sistema inativo");
            } else if (isInUse) {
                heroClass = "heroInUse";
                this._model.setProperty("/heroIcon", "☕");
                this._model.setProperty("/heroState", "EM USO");
                this._model.setProperty("/heroSubtitle", "Preparando café…");
                this._model.setProperty("/systemActiveText", "🟢 Sistema ativo");
            } else {
                heroClass = "heroAvailable";
                this._model.setProperty("/heroIcon", "✔️");
                this._model.setProperty("/heroState", "DISPONÍVEL");
                this._model.setProperty("/heroSubtitle", "Máquina pronta para uso");
                this._model.setProperty("/systemActiveText", "🟢 Sistema ativo");
            }
            this._applyHeroClass(heroClass);

            this._model.setProperty("/coffeesToday", status.coffeesToday);
            this._model.setProperty("/lastUsedDate", this._formatDate(status.lastUsedUtc));
            this._model.setProperty("/lastUsedTime", this._formatTime(status.lastUsedUtc));

            // Um café novo (ou mudança de estado) também atualiza os KPIs e o histórico.
            this._refreshDashboard();
            this._refreshEvents();
        },

        _applyDashboard: function (dashboard) {
            if (!dashboard) {
                return;
            }

            this._model.setProperty("/coffeesToday", dashboard.coffeesToday);
            this._model.setProperty("/averagePerHour", this._round(dashboard.averagePerHour));
            this._model.setProperty("/averageIntervalText", this._formatInterval(dashboard.averageIntervalSeconds));
            this._model.setProperty("/peakHourText", this._formatPeakHour(dashboard.peakHourToday));

            // Variação vs ontem (seta para cima/baixo e cor).
            const change = dashboard.changeVsYesterdayPercent;
            if (change === null || change === undefined) {
                this._model.setProperty("/changeText", "sem comparativo de ontem");
                this._model.setProperty("/changeClass", "kpiCaption");
            } else if (change >= 0) {
                this._model.setProperty("/changeText", "↑ +" + this._round(change) + "% em relação a ontem");
                this._model.setProperty("/changeClass", "kpiUp");
            } else {
                this._model.setProperty("/changeText", "↓ " + this._round(change) + "% em relação a ontem");
                this._model.setProperty("/changeClass", "kpiDown");
            }
        },

        // ----- Helpers de apresentação -----
        _applyHeroClass: function (heroClass) {
            const hero = this.byId("heroBanner");
            if (!hero) {
                return;
            }
            hero.removeStyleClass("heroInUse").removeStyleClass("heroAvailable").removeStyleClass("heroOffline");
            hero.addStyleClass(heroClass);
        },

        _tickClock: function () {
            const now = new Date();
            this._model.setProperty("/clockTime", now.toLocaleTimeString("pt-BR"));
            this._model.setProperty("/clockDate", now.toLocaleDateString("pt-BR", {
                weekday: "long", day: "2-digit", month: "long", year: "numeric"
            }));
        },

        _formatDate: function (isoUtc) {
            if (!isoUtc) {
                return "—";
            }
            const date = new Date(isoUtc);
            return isNaN(date.getTime()) ? "—" : date.toLocaleDateString("pt-BR");
        },

        _formatTime: function (isoUtc) {
            if (!isoUtc) {
                return "";
            }
            const date = new Date(isoUtc);
            return isNaN(date.getTime()) ? "" : date.toLocaleTimeString("pt-BR");
        },

        // Segundos -> "mm:ss" (intervalo médio entre cafés).
        _formatInterval: function (seconds) {
            if (seconds === null || seconds === undefined) {
                return "—";
            }
            const minutes = Math.floor(seconds / 60);
            const rest = seconds % 60;
            return String(minutes).padStart(2, "0") + ":" + String(rest).padStart(2, "0");
        },

        // { hour, count } -> "13h (5)".
        _formatPeakHour: function (peak) {
            if (!peak) {
                return "—";
            }
            return peak.hour + "h (" + peak.count + ")";
        },

        _round: function (value) {
            return Math.round((value || 0) * 10) / 10;
        }
    });
});
