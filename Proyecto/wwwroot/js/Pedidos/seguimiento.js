(function () {
    var mapFinal = null;
    var markerMoto = null;
    var routingControl = null;

    function initTracking() {
        var $el = $('#mapa-final');
        if ($el.length === 0 || mapFinal !== null) return;

        // Leer coordenadas
        var latO = parseFloat($el.attr('data-lat'));
        var lngO = parseFloat($el.attr('data-lng'));
        var latD = parseFloat($el.attr('data-dest-lat'));
        var lngD = parseFloat($el.attr('data-dest-lng'));

        // Inicializar Leaflet
        mapFinal = L.map('mapa-final', { zoomControl: false, attributionControl: false }).setView([latO, lngO], 15);
        L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png').addTo(mapFinal);

        // Iconos
        var iconMoto = L.divIcon({
            className: 'custom-icon',
            html: '<i class="fas fa-motorcycle" style="color: #FFCC00; font-size: 32px;"></i>',
            iconSize: [32, 32], iconAnchor: [16, 16]
        });
        var iconDest = L.divIcon({
            className: 'custom-icon',
            html: '<i class="fas fa-map-marker-alt" style="color: #FF0000; font-size: 32px;"></i>',
            iconSize: [32, 32], iconAnchor: [16, 32]
        });

        markerMoto = L.marker([latO, lngO], { icon: iconMoto }).addTo(mapFinal);
        L.marker([latD, lngD], { icon: iconDest }).addTo(mapFinal);

        // Routing
        routingControl = L.Routing.control({
            waypoints: [L.latLng(latO, lngO), L.latLng(latD, lngD)],
            createMarker: function () { return null; },
            lineOptions: { addWaypoints: false, styles: [{ color: '#FFCC00', weight: 6, opacity: 0.8 }] },
            show: false
        }).addTo(mapFinal);

        routingControl.on('routesfound', function (e) {
            var points = e.routes[0].coordinates;
            setTimeout(function () {
                mapFinal.invalidateSize();
                mapFinal.fitBounds([[latO, lngO], [latD, lngD]], { padding: [100, 100] });
            }, 300);
            startAnimation(points);
        });
    }

    function startAnimation(puntos) {
        var duracion = 10000;
        var inicio = performance.now();

        function frame(ahora) {
            var progreso = Math.min((ahora - inicio) / duracion, 1);
            var idx = Math.floor(progreso * (puntos.length - 1));

            if (puntos[idx]) {
                markerMoto.setLatLng([puntos[idx].lat, puntos[idx].lng]);
            }

            $('#bar').css('width', (progreso * 100) + '%');
            $('#timer').text(Math.ceil(10 - (progreso * 10)) + " seg");

            if (progreso < 1) {
                requestAnimationFrame(frame);
            } else {
                $('#status-tag').text('¡LLEGÓ!').removeClass('text-camino').addClass('text-llegado');
                if (typeof confetti === 'function') confetti({ particleCount: 150, spread: 70, origin: { y: 0.7 } });
            }
        }
        requestAnimationFrame(frame);
    }

    $(document).ready(initTracking);
})();