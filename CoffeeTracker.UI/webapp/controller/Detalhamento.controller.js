sap.ui.define([
    "sap/ui/core/mvc/Controller",
    "sap/ui/model/json/JSONModel",
    "sapuccino/service/CoffeeService"
], function (Controller, JSONModel, CoffeeService) {
    "use strict";

    return Controller.extend("sapuccino.controller.Detalhamento", {
        onInit: function () {
            this._model = new JSONModel({
                periodDays: "7",
                totalCoffees: 0,
                averagePerDay: 0,
                busiestDayText: "—",
                busiestDayCaption: "",
                peakHourText: "—",
                daily: [],
                hourly: [],
                uptimePercent: 0,
                uptimeWidth: "0%",
                currentStateText: "—",
                outages: 0,
                longestOutageText: "—"
            });
            this.getView().setModel(this._model);

            // Recarrega os relatórios sempre que esta rota é exibida.
            this.getOwnerComponent().getRouter()
                .getRoute("detalhamento")
                .attachPatternMatched(this._onRouteMatched, this);
        },

        _onRouteMatched: function () {
            this._loadReports();
        },

        onBack: function () {
            this.getOwnerComponent().getRouter().navTo("monitor");
        },

        onPeriodChange: function (event) {
            this._model.setProperty("/periodDays", event.getParameter("item").getKey());
            this._loadReports();
        },

        _loadReports: function () {
            const days = parseInt(this._model.getProperty("/periodDays"), 10) || 7;

            CoffeeService.getReports(days)
                .then(this._applyReport.bind(this))
                .catch(function () { /* depende do banco */ });

            CoffeeService.getAvailability(days)
                .then(this._applyAvailability.bind(this))
                .catch(function () { /* depende do banco */ });
        },

        _applyReport: function (report) {
            if (!report) {
                return;
            }

            this._model.setProperty("/totalCoffees", report.totalCoffees);
            this._model.setProperty("/averagePerDay", this._round(report.averagePerDay));
            this._model.setProperty("/peakHourText", this._formatHour(report.peakHour));

            if (report.busiestDay) {
                this._model.setProperty("/busiestDayText", this._formatDayLabel(report.busiestDay.date));
                this._model.setProperty("/busiestDayCaption", report.busiestDay.count + " cafés");
            } else {
                this._model.setProperty("/busiestDayText", "—");
                this._model.setProperty("/busiestDayCaption", "");
            }

            // Série por dia (barras proporcionais ao maior valor).
            const daily = report.daily || [];
            const maxDaily = Math.max(1, ...daily.map(function (d) { return d.count; }));
            this._model.setProperty("/daily", daily.map(function (d) {
                return {
                    label: this._formatDayLabel(d.date),
                    value: String(d.count),
                    width: ((d.count / maxDaily) * 100).toFixed(1) + "%"
                };
            }.bind(this)));

            // Distribuição por hora (apenas horas com algum café, para não poluir).
            const hourly = (report.hourly || []).filter(function (h) { return h.count > 0; });
            const maxHourly = Math.max(1, ...hourly.map(function (h) { return h.count; }));
            this._model.setProperty("/hourly", hourly.map(function (h) {
                return {
                    label: this._pad(h.hour) + "h",
                    value: String(h.count),
                    width: ((h.count / maxHourly) * 100).toFixed(1) + "%"
                };
            }.bind(this)));
        },

        _applyAvailability: function (availability) {
            if (!availability) {
                return;
            }
            this._model.setProperty("/uptimePercent", this._round(availability.uptimePercent));
            this._model.setProperty("/uptimeWidth", this._round(availability.uptimePercent) + "%");
            this._model.setProperty("/currentStateText", availability.currentState === "online" ? "Online" : "Offline");
            this._model.setProperty("/outages", availability.outages);
            this._model.setProperty("/longestOutageText", this._formatDuration(availability.longestOutageSeconds));
        },

        // ----- Helpers -----
        _formatDayLabel: function (isoDate) {
            // "yyyy-MM-dd" -> "dd/MM".
            if (!isoDate) {
                return "—";
            }
            const parts = isoDate.split("-");
            return parts.length === 3 ? parts[2] + "/" + parts[1] : isoDate;
        },

        _formatHour: function (peak) {
            if (!peak) {
                return "—";
            }
            return peak.hour + "h (" + peak.count + ")";
        },

        // Segundos -> "Xh Ym" ou "Ym" ou "—".
        _formatDuration: function (seconds) {
            if (!seconds) {
                return "—";
            }
            const hours = Math.floor(seconds / 3600);
            const minutes = Math.floor((seconds % 3600) / 60);
            if (hours > 0) {
                return hours + "h " + minutes + "m";
            }
            return minutes + "m";
        },

        _pad: function (value) {
            return String(value).padStart(2, "0");
        },

        _round: function (value) {
            return Math.round((value || 0) * 10) / 10;
        }
    });
});
