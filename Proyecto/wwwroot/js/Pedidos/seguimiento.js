(function () {
    var mapFinal = null;
    var markerMoto = null;

    function initTracking() {
        var $el = $('#mapa-final');
        if ($el.length === 0 || mapFinal !== null) return;

        var latO = parseFloat($el.attr('data-lat'));
        var lngO = parseFloat($el.attr('data-lng'));
        var latD = parseFloat($el.attr('data-dest-lat'));
        var lngD = parseFloat($el.attr('data-dest-lng'));

        var isDark = $('html').attr('data-theme') === 'dark';
        var pinColor = isDark ? '#FFCC00' : '#FF0000';
        var routeColor = '#FFCC00';

        mapFinal = L.map('mapa-final', { zoomControl: false, attributionControl: false }).setView([latO, lngO], 15);
        L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png').addTo(mapFinal);

        var iconMoto = L.divIcon({
            className: 'custom-icon',
            html: '<i class="fas fa-motorcycle" style="color: ' + routeColor + '; font-size: 32px;"></i>',
            iconSize: [32, 32], iconAnchor: [16, 16]
        });
        var iconDest = L.divIcon({
            className: 'custom-icon',
            html: '<i class="fas fa-map-marker-alt" style="color: ' + pinColor + '; font-size: 32px;"></i>',
            iconSize: [32, 32], iconAnchor: [16, 32]
        });

        markerMoto = L.marker([latO, lngO], { icon: iconMoto }).addTo(mapFinal);
        L.marker([latD, lngD], { icon: iconDest }).addTo(mapFinal);

        setTimeout(function () {
            mapFinal.invalidateSize();
            mapFinal.fitBounds([[latO, lngO], [latD, lngD]], { padding: [100, 100] });
        }, 300);

        fetchRoute(latO, lngO, latD, lngD, routeColor);
    }

    function fetchRoute(latO, lngO, latD, lngD, routeColor) {
        var url = '/Pedidos/GetRuta?latO=' + latO + '&lngO=' + lngO + '&latD=' + latD + '&lngD=' + lngD;

        fetch(url)
            .then(function (r) {
                if (!r.ok) throw new Error('ruta no disponible');
                return r.json();
            })
            .then(function (points) {
                L.polyline(points.map(function (p) { return [p.lat, p.lng]; }), {
                    color: routeColor, weight: 6, opacity: 0.8
                }).addTo(mapFinal);
                startAnimation(points);
            })
            .catch(function () {
                useFallback(latO, lngO, latD, lngD, routeColor);
            });
    }

    function useFallback(latO, lngO, latD, lngD, routeColor) {
        var steps = 200;
        var points = [];
        for (var i = 0; i <= steps; i++) {
            var t = i / steps;
            points.push({ lat: latO + (latD - latO) * t, lng: lngO + (lngD - lngO) * t });
        }
        L.polyline([[latO, lngO], [latD, lngD]], {
            color: routeColor, weight: 4, dashArray: '10,8', opacity: 0.6
        }).addTo(mapFinal);
        startAnimation(points);
    }

    var VELOCIDAD_MS = 100;

    function lerp(a, b, t) { return a + (b - a) * t; }

    function haversine(lat1, lng1, lat2, lng2) {
        var R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
                Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
                Math.sin(dLng / 2) * Math.sin(dLng / 2);
        return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }

    function formatTiempo(seg) {
        if (seg <= 0) return '0 seg';
        if (seg < 60) return seg + ' seg';
        var m = Math.floor(seg / 60);
        var s = seg % 60;
        return s > 0 ? m + ' min ' + s + ' seg' : m + ' min';
    }

    function startAnimation(puntos) {
        var cumDist = new Array(puntos.length);
        cumDist[0] = 0;
        for (var i = 0; i < puntos.length - 1; i++) {
            cumDist[i + 1] = cumDist[i] + haversine(puntos[i].lat, puntos[i].lng, puntos[i + 1].lat, puntos[i + 1].lng);
        }
        var distanciaTotal = cumDist[puntos.length - 1];
        var segundosTotal  = Math.max(8, distanciaTotal / VELOCIDAD_MS);
        var duracion       = segundosTotal * 1000;
        var inicio         = performance.now();
        var terminado      = false;
        var lastIdx        = 0;

        var elMoto  = markerMoto.getElement();
        if (elMoto) elMoto.style.willChange = 'transform';

        var elBar   = document.getElementById('bar');
        var elTimer = document.getElementById('timer');

        var uiInterval = setInterval(function () {
            if (terminado) { clearInterval(uiInterval); return; }
            var p = Math.min((performance.now() - inicio) / duracion, 1);
            elBar.style.width   = (p * 100) + '%';
            elTimer.textContent = formatTiempo(Math.ceil(segundosTotal * (1 - p)));
        }, 250);

        function frame(ahora) {
            if (terminado) return;
            var progreso        = Math.min((ahora - inicio) / duracion, 1);
            var metrosRecorridos = progreso * distanciaTotal;

            // Avanzar el índice solo hacia adelante (O(1) amortizado)
            while (lastIdx < puntos.length - 2 && cumDist[lastIdx + 1] <= metrosRecorridos) {
                lastIdx++;
            }

            var lat, lng;
            var segDist = cumDist[lastIdx + 1] - cumDist[lastIdx];
            if (segDist > 0) {
                var frac = (metrosRecorridos - cumDist[lastIdx]) / segDist;
                lat = lerp(puntos[lastIdx].lat, puntos[lastIdx + 1].lat, frac);
                lng = lerp(puntos[lastIdx].lng, puntos[lastIdx + 1].lng, frac);
            } else {
                lat = puntos[lastIdx].lat;
                lng = puntos[lastIdx].lng;
            }

            markerMoto.setLatLng([lat, lng]);

            if (progreso < 1) {
                requestAnimationFrame(frame);
            } else {
                terminado = true;
                clearInterval(uiInterval);
                elBar.style.width   = '100%';
                elTimer.textContent = '0 seg';
                if (elMoto) elMoto.style.willChange = '';
                $('#status-tag').text('¡LLEGÓ!').removeClass('text-camino').addClass('text-llegado');
                if (typeof confetti === 'function') confetti({ particleCount: 150, spread: 70, origin: { y: 0.7 } });
            }
        }
        requestAnimationFrame(frame);
    }

    $(document).ready(initTracking);
})();