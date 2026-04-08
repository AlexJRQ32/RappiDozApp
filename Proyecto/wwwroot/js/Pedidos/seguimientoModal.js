(function () {
    "use strict";

    var _data       = null;
    var _map        = null;
    var _markerMoto = null;
    var _pollingId  = null;
    var _animDone   = false;
    var _routeLayer = null;

    function _swalV(n) { return getComputedStyle(document.documentElement).getPropertyValue(n).trim(); }

    fetch("/Pedidos/DatosSeguimiento")
        .then(function (r) { return r.json(); })
        .then(function (d) {
            if (d.activo) { _data = d; _showHeroBtn(d.estado); }
        })
        .catch(function () {});

    function _showHeroBtn(estado) {
        var wrap = document.getElementById("seguimiento-hero-wrap");
        if (wrap) wrap.style.display = "";
        _updateBadge(estado);
    }

    function _updateBadge(estado) {
        var b = document.getElementById("seguimiento-float-badge");
        if (!b) return;
        b.style.background = estado === "Pendiente" ? "#ff9800"
            : (estado === "Aceptado" || estado === "En preparacion") ? "#d4af37"
            : "#66bb6a";
    }

    window.abrirSeguimientoModal = function () {
        fetch("/Pedidos/DatosSeguimiento")
            .then(function (r) { return r.json(); })
            .then(function (d) {
                if (!d.activo) {
                    Swal.fire({ icon: "info", title: "Sin pedidos activos", text: "No tienes ningun envio pendiente.", confirmButtonColor: _swalV("--swal-btn"), background: _swalV("--modal-shell-bg"), color: _swalV("--modal-text") });
                    var wrap = document.getElementById("seguimiento-hero-wrap");
                    if (wrap) wrap.style.display = "none";
                    return;
                }
                _data = d;
                _openModal(d);
            })
            .catch(function () {});
    };

    function _openModal(d) {
        var modalEl = document.getElementById("modalGeneral");
        if (!modalEl) return;
        var dialog  = modalEl.querySelector(".modal-dialog");
        var content = modalEl.querySelector(".modal-content");
        var body    = document.getElementById("modalBodyGeneral");
        if (!body) return;

        if (dialog)  dialog.classList.add("modal-lg");
        if (content) content.setAttribute("style", "background:#1a1a1a!important;border:1px solid rgba(212,175,55,0.35)!important;border-radius:20px!important;overflow:hidden!important;");
        body.setAttribute("style", "padding:0!important;text-align:left!important;");
        body.innerHTML = _buildHTML(d);
        _populateStatus(d);

        var modal = new bootstrap.Modal(modalEl);
        modalEl.addEventListener("shown.bs.modal", function onShown() {
            modalEl.removeEventListener("shown.bs.modal", onShown);
            _initMap(d);
        });
        modalEl.addEventListener("hidden.bs.modal", function onHidden() {
            modalEl.removeEventListener("hidden.bs.modal", onHidden);
            _destroyMap();
            if (dialog)  dialog.classList.remove("modal-lg");
            if (content) content.removeAttribute("style");
            body.removeAttribute("style");
            body.innerHTML = "";
        });
        modal.show();
    }

    function _buildHTML(d) {
        var bc = d.estado === "Pendiente" ? "text-pendiente" : (d.estado === "Aceptado" || d.estado === "En preparacion") ? "text-preparando" : "text-camino";
        return "<div class='seg-modal-header'><div class='d-flex align-items-center gap-2'><i class='fa-solid fa-route' style='color:#d4af37;'></i><span class='seg-modal-title'>Seguimiento de Pedido</span></div><div class='d-flex align-items-center gap-2'><span id='modal-estado-badge' class='status-badge " + bc + "'>" + d.estado + "</span><button type='button' class='btn-close btn-close-white' data-bs-dismiss='modal'></button></div></div>"
            + "<div id='seguimiento-mapa-modal' class='seguimiento-mapa-modal'></div>"
            + "<div class='seguimiento-modal-info'><div id='seguimiento-modal-status-box'></div><div class='d-flex justify-content-between align-items-center mt-2'><div><small class='seguimiento-modal-meta'><i class='fas fa-hashtag'></i> Orden <span id='modal-orden-num'>#" + d.pedidoId + "</span></small><br><small class='seguimiento-modal-meta'><i class='fas fa-user'></i> <span id='modal-cliente-nombre'>" + d.nombreCliente + "</span></small></div><div id='modal-eta-box' class='modal-eta-box' style='display:none;'><small>ETA</small><span id='modal-timer'>--</span><div id='modal-bar' class='modal-progress-bar'></div></div></div></div>";
    }

    function _populateStatus(d) {
        var box = document.getElementById("seguimiento-modal-status-box");
        var etaBox = document.getElementById("modal-eta-box");
        if (!box) return;
        if (d.estado === "Pendiente") {
            box.innerHTML = "<div class='status-info-box' style='margin:0 0 10px'><i class='fa-solid fa-clock-rotate-left status-info-icon'></i><h4 class='status-info-title'>Esperando aprobacion</h4><p class='status-info-text'>El restaurante aun no ha aceptado tu solicitud.</p><div class='status-info-pulse'></div></div>";
            if (etaBox) etaBox.style.display = "none";
        } else if (d.estado === "Aceptado") {
            box.innerHTML = "<div class='status-info-box' style='margin:0 0 10px'><i class='fa-solid fa-circle-check status-info-icon status-info-icon-green'></i><h4 class='status-info-title'>Pedido aceptado</h4><p class='status-info-text'>El restaurante ha aceptado tu pedido.</p><div class='status-info-pulse'></div></div>";
            if (etaBox) etaBox.style.display = "none";
        } else if (d.estado === "En preparacion") {
            box.innerHTML = "<div class='status-info-box' style='margin:0 0 10px'><i class='fa-solid fa-fire-burner status-info-icon status-info-icon-amber'></i><h4 class='status-info-title'>Preparando tu pedido</h4><p class='status-info-text'>Ya casi esta listo!</p><div class='status-info-pulse'></div></div>";
            if (etaBox) etaBox.style.display = "none";
        } else {
            box.innerHTML = "";
            if (etaBox) etaBox.style.display = "";
        }
    }

    function _initMap(d) {
        _destroyMap();
        _animDone = false;
        var latO = parseFloat(d.latOrigen),  lngO = parseFloat(d.lngOrigen);
        var latD = parseFloat(d.latDestino), lngD = parseFloat(d.lngDestino);
        var routeColor = "#FFCC00";
        var isDark = document.documentElement.getAttribute("data-theme") === "dark";
        var pinColor = isDark ? "#FFCC00" : "#FF0000";

        _map = L.map("seguimiento-mapa-modal", { zoomControl: true, attributionControl: false }).setView([latO, lngO], 14);
        L.tileLayer("https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png").addTo(_map);

        var iconMoto = L.divIcon({ className: "custom-icon", html: "<i class='fas fa-motorcycle' style='color:" + routeColor + ";font-size:28px;'></i>", iconSize: [32,32], iconAnchor: [16,16] });
        var iconDest = L.divIcon({ className: "custom-icon", html: "<i class='fas fa-map-marker-alt' style='color:" + pinColor + ";font-size:28px;'></i>", iconSize: [32,32], iconAnchor: [16,32] });

        _markerMoto = L.marker([latO, lngO], { icon: iconMoto }).addTo(_map);
        L.marker([latD, lngD], { icon: iconDest }).addTo(_map);

        setTimeout(function () {
            if (!_map) return;
            _map.invalidateSize();
            _map.fitBounds([[latO,lngO],[latD,lngD]], { padding: [55,55] });
        }, 250);

        _fetchRouteOSRM(latO, lngO, latD, lngD, routeColor, d.estado);
        _startPolling(d.pedidoId);
    }

    function _fetchRouteOSRM(latO, lngO, latD, lngD, routeColor, estado) {
        var url = "https://router.project-osrm.org/route/v1/driving/"
            + lngO + "," + latO + ";"
            + lngD + "," + latD
            + "?overview=full&geometries=geojson";

        fetch(url)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (!_map) return;
                if (!data.routes || data.routes.length === 0) { _fallbackRoute(latO, lngO, latD, lngD, routeColor, estado); return; }
                var pts = data.routes[0].geometry.coordinates.map(function (c) { return { lat: c[1], lng: c[0] }; });
                var latlngs = pts.map(function (p) { return [p.lat, p.lng]; });
                var isPending = (estado === "Pendiente" || estado === "En preparacion");
                if (_routeLayer && _map) { _map.removeLayer(_routeLayer); }
                _routeLayer = L.polyline(latlngs, {
                    color: routeColor,
                    weight: isPending ? 3 : 5,
                    opacity: isPending ? 0.45 : 0.85,
                    dashArray: isPending ? "8,6" : null
                }).addTo(_map);
                _map.fitBounds(L.latLngBounds(latlngs), { padding: [55, 55] });
                if (!isPending) _runAnimation(pts);
            })
            .catch(function () { _fallbackRoute(latO, lngO, latD, lngD, routeColor, estado); });
    }

    function _fallbackRoute(latO, lngO, latD, lngD, routeColor, estado) {
        if (!_map) return;
        var isPending = (estado === "Pendiente" || estado === "En preparacion");
        var pts = [];
        for (var i = 0; i <= 100; i++) { var t = i/100; pts.push({ lat: latO+(latD-latO)*t, lng: lngO+(lngD-lngO)*t }); }
        if (_routeLayer && _map) { _map.removeLayer(_routeLayer); }
        _routeLayer = L.polyline([[latO,lngO],[latD,lngD]], {
            color: routeColor,
            weight: isPending ? 3 : 4,
            dashArray: "8,6",
            opacity: isPending ? 0.45 : 0.65
        }).addTo(_map);
        if (!isPending) _runAnimation(pts);
    }

    function _runAnimation(pts) {
        if (_animDone) return;
        var elTimer = document.getElementById("modal-timer");
        var elBar   = document.getElementById("modal-bar");
        var DURACION = Math.max(10000, pts.length * 55);
        var inicio = performance.now();
        var terminado = false;

        var uiInt = setInterval(function () {
            if (terminado) { clearInterval(uiInt); return; }
            var p = Math.min((performance.now() - inicio) / DURACION, 1);
            if (elBar) elBar.style.width = (p * 100) + "%";
            var s = Math.ceil((DURACION * (1 - p)) / 1000);
            if (elTimer) elTimer.textContent = s >= 60 ? Math.floor(s/60) + " min" : s + " seg";
        }, 300);

        function frame(ahora) {
            if (terminado || !_markerMoto) return;
            var prog = Math.min((ahora - inicio) / DURACION, 1);
            var idx  = Math.min(Math.floor(prog * pts.length), pts.length - 1);
            _markerMoto.setLatLng([pts[idx].lat, pts[idx].lng]);
            if (prog < 1) {
                requestAnimationFrame(frame);
            } else {
                terminado = true; _animDone = true;
                clearInterval(uiInt);
                if (elBar) elBar.style.width = "100%";
                if (elTimer) elTimer.textContent = "Llego!";
                _completarEntrega();
            }
        }
        requestAnimationFrame(frame);
    }

    function _completarEntrega() {
        var token = document.querySelector("input[name='__RequestVerificationToken']");
        var tv = token ? token.value : "";
        fetch("/Pedidos/CompletarSeguimiento", { method: "POST", headers: { "Content-Type": "application/x-www-form-urlencoded" }, body: "id=" + _data.pedidoId + "&__RequestVerificationToken=" + encodeURIComponent(tv) })
            .then(function () { var w = document.getElementById("seguimiento-hero-wrap"); if (w) w.style.display = "none"; })
            .catch(function () {});
    }

    function _startPolling(pedidoId) {
        if (_pollingId) clearInterval(_pollingId);
        _pollingId = setInterval(function () {
            fetch("/Pedidos/ObtenerEstado/" + pedidoId)
                .then(function (r) { return r.json(); })
                .then(function (d) {
                    if (!_data || d.estado === _data.estado) return;
                    _data.estado = d.estado;
                    _populateStatus(_data);
                    _updateBadge(d.estado);
                    var badge = document.getElementById("modal-estado-badge");
                    if (badge) badge.textContent = d.estado;
                    if (d.estado === "Entregado" || d.estado === "Cancelado") {
                        clearInterval(_pollingId);
                        var w = document.getElementById("seguimiento-hero-wrap");
                        if (w) w.style.display = "none";
                    }
                    if (d.estado !== "Pendiente" && d.estado !== "En preparacion" && _map && !_animDone) {
                        _fetchRouteOSRM(parseFloat(_data.latOrigen), parseFloat(_data.lngOrigen), parseFloat(_data.latDestino), parseFloat(_data.lngDestino), "#FFCC00", d.estado);
                    }
                })
                .catch(function () {});
        }, 8000);
    }

    function _destroyMap() {
        if (_pollingId) { clearInterval(_pollingId); _pollingId = null; }
        if (_map) { _map.remove(); _map = null; _markerMoto = null; _routeLayer = null; }
        _animDone = false;
    }
})();