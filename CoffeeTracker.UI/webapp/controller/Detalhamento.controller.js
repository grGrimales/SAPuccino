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
                monthly: [],
                hourlyPoints: [],
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

            // Série por dia: últimos 15 dias (label + count) para o ColumnMicroChart.
            const dailyAll = report.daily || [];
            this._model.setProperty("/daily", dailyAll.slice(-15).map(function (d) {
                return { label: this._formatDayLabel(d.date), count: d.count };
            }.bind(this)));

            // Série por mês: últimos 12 meses (preenche os meses com dados, resto 0).
            const byMonth = {};
            dailyAll.forEach(function (d) {
                const ym = d.date.slice(0, 7);
                byMonth[ym] = (byMonth[ym] || 0) + d.count;
            });
            const monthNames = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];
            const ref = (report.toDate || "").split("-");
            const refYear = parseInt(ref[0], 10) || 2026;
            const refMonth = parseInt(ref[1], 10) || 1;
            const monthly = [];
            for (let i = 11; i >= 0; i--) {
                let mm = refMonth - i;
                let yy = refYear;
                while (mm <= 0) { mm += 12; yy -= 1; }
                const ym = yy + "-" + String(mm).padStart(2, "0");
                monthly.push({ label: monthNames[mm - 1], count: byMonth[ym] || 0 });
            }
            this._model.setProperty("/monthly", monthly);

            // Distribuição por hora: pontos (x = hora, y = cafés) para o LineMicroChart.
            this._model.setProperty("/hourlyPoints", (report.hourly || []).map(function (h) {
                return { x: h.hour, y: h.count };
            }));
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
