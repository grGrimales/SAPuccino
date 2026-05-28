sap.ui.define([
    "sap/ui/core/mvc/Controller",
    "sap/ui/model/json/JSONModel"
], function (Controller, JSONModel) {
    "use strict";

    var BACKEND_URL = "http://localhost:5036/";

    return Controller.extend("sapuccino.controller.App", {
        onInit: function () {
            this.getView().setModel(new JSONModel({
                backendMessage: "BE sin probar todavía…"
            }));
        },

        onPingBackend: function () {
            var oModel = this.getView().getModel();
            oModel.setProperty("/backendMessage", "Llamando al BE…");

            fetch(BACKEND_URL)
                .then(function (res) {
                    if (!res.ok) {
                        throw new Error("HTTP " + res.status);
                    }
                    return res.json();
                })
                .then(function (data) {
                    oModel.setProperty(
                        "/backendMessage",
                        "✅ BE responde: " + data.message + " (status: " + data.status + ")"
                    );
                })
                .catch(function (err) {
                    oModel.setProperty(
                        "/backendMessage",
                        "❌ Error llamando al BE: " + err.message
                    );
                });
        }
    });
});
