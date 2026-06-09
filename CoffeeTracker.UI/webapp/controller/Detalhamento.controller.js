sap.ui.define([
    "sap/ui/core/mvc/Controller",
    "sap/ui/model/json/JSONModel",
    "sapuccino/service/CoffeeService"
], function (Controller, JSONModel, CoffeeService) {
    "use strict";

    return Controller.extend("sapuccino.controller.Detalhamento", {
        onInit: function () {
            this._model = new JSONModel({
                periodDays: "30",
                totalCoffees: 0,
                averagePerDay: 0,
                busiestDayText: "—",
                busiestDayCaption: "",
                peakHourText: "—",
                daily: [],
                monthly: [],
                monthlyYears: [],
                selectedYear: "",
                peakHours: [],
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

            // O gráfico "Cafés por mês" usa um período longo (independente do seletor 7/30/90).
            CoffeeService.getReports(180)
                .then(this._applyMonthly.bind(this))
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
            // Série por dia como colunas de largura fixa (últimos 15 dias), com rótulo dd/MM.
            const dailyAll = report.daily || [];
            const dailyRecent = dailyAll.slice(-15);
            const maxDaily = Math.max(1, ...dailyRecent.map(function (d) { return d.count; }));
            this._model.setProperty("/daily", dailyRecent.map(function (d) {
                return {
                    label: this._formatDayLabel(d.date),
                    value: String(d.count),
                    height: ((d.count / maxDaily) * 100).toFixed(1) + "%"
                };
            }.bind(this)));

            // Distribuição por hora: pontos (x = hora, y = cafés) para o LineMicroChart.
            this._model.setProperty("/hourlyPoints", (report.hourly || []).map(function (h) {
                return { x: h.hour, y: h.count };
            }));

            // Horários de pico: as 3 horas com mais cafés no período.
            const topHours = (report.hourly || [])
                .filter(function (h) { return h.count > 0; })
                .sort(function (a, b) { return b.count - a.count; })
                .slice(0, 3);
            const medals = ["🥇", "🥈", "🥉"];
            this._model.setProperty("/peakHours", topHours.map(function (h, index) {
                return {
                    rank: medals[index] || ("#" + (index + 1)),
                    hour: this._pad(h.hour) + "h",
                    countText: h.count + " cafés"
                };
            }.bind(this)));
        },

        // Agrega "Cafés por mês" de um período longo (180 dias), agrupado por ano,
        // e prepara as abas de ano (2025 / 2026).
        _applyMonthly: function (report) {
            if (!report) {
                return;
            }
            const monthNames = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez"];
            const byMonth = {};
            (report.daily || []).forEach(function (d) {
                const ym = d.date.slice(0, 7);
                byMonth[ym] = (byMonth[ym] || 0) + d.count;
            });

            // Para cada ano com dados, monta os meses contíguos (do 1º ao último com dados).
            const byYear = {};
            Object.keys(byMonth).forEach(function (ym) {
                const year = ym.slice(0, 4);
                byYear[year] = byYear[year] || {};
                byYear[year][parseInt(ym.slice(5, 7), 10)] = byMonth[ym];
            });
            const monthsByYear = {};
            Object.keys(byYear).forEach(function (year) {
                const monthsObj = byYear[year];
                const present = Object.keys(monthsObj).map(Number).sort(function (a, b) { return a - b; });
                const months = [];
                for (let m = present[0]; m <= present[present.length - 1]; m++) {
                    months.push({ label: monthNames[m - 1], count: monthsObj[m] || 0 });
                }
                monthsByYear[year] = months;
            });
            this._monthsByYear = monthsByYear;

            const years = Object.keys(this._monthsByYear).sort();
            this._model.setProperty("/monthlyYears", years.map(function (y) {
                return { key: y, text: y };
            }));

            // Ano selecionado: mantém o atual se ainda existir, senão o mais recente.
            let selected = this._model.getProperty("/selectedYear");
            if (years.indexOf(selected) < 0) {
                selected = years.length ? years[years.length - 1] : "";
                this._model.setProperty("/selectedYear", selected);
            }
            this._renderMonthly(selected);
        },

        // Renderiza as colunas do ano selecionado.
        _renderMonthly: function (year) {
            const months = (this._monthsByYear && this._monthsByYear[year]) || [];
            const maxMonth = Math.max(1, ...months.map(function (o) { return o.count; }));
            this._model.setProperty("/monthly", months.map(function (o) {
                return {
                    label: o.label,
                    value: String(o.count),
                    height: ((o.count / maxMonth) * 100).toFixed(1) + "%"
                };
            }));
        },

        onYearChange: function (event) {
            const year = event.getParameter("item").getKey();
            this._model.setProperty("/selectedYear", year);
            this._renderMonthly(year);
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
