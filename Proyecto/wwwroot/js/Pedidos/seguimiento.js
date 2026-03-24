document.addEventListener("DOMContentLoaded", function () {
    var mapEl = document.getElementById('map');
    if (!mapEl) return;

    const latO = parseFloat(mapEl.dataset.repartidorLat);
    const lngO = parseFloat(mapEl.dataset.repartidorLng);
    const latD = parseFloat(mapEl.dataset.usuarioLat);
    const lngD = parseFloat(mapEl.dataset.usuarioLng);

    const map = L.map('map', { zoomControl: false, attributionControl: false }).setView([latO, lngO], 15);
    RappiDozMap.addTileLayer(map);

    setTimeout(() => { try { map.invalidateSize(); } catch(e) { console.warn('invalidateSize failed', e); } }, 100);

    const iconM = L.divIcon({
        html: '<i class="fas fa-motorcycle moto-icon"></i>',
        className: 'no-bg', iconSize: [40, 40], iconAnchor: [20, 20]
    });

    const markerM = L.marker([latO, lngO], { icon: iconM }).addTo(map);
    L.marker([latD, lngD]).addTo(map);

    function iniciar(puntos) {
        const duracion = 10000;
        const inicio = performance.now();

        map.fitBounds([[latO, lngO], [latD, lngD]], { padding: [60, 60] });

        function frame(ahora) {
            const t = Math.min((ahora - inicio) / duracion, 1);
            const idx = Math.floor(t * (puntos.length - 1));

            const p = puntos[idx];
            if (p) {
                const curLat = p.lat !== undefined ? p.lat : p[0];
                const curLng = p.lng !== undefined ? p.lng : p[1];
                markerM.setLatLng([curLat, curLng]);
            }

            document.getElementById('bar').style.width = (t * 100) + "%";
            document.getElementById('timer').innerText = Math.ceil(10 - (t * 10)) + " seg";

            if (t < 1) {
                requestAnimationFrame(frame);
            } else {
                const tag = document.getElementById('status-tag');
                tag.innerText = "¡Llegó!";
                tag.classList.remove('text-camino');
                tag.classList.add('text-llegado');

                confetti({ particleCount: 150, spread: 70, origin: { y: 0.7 } });
            }
        }
        requestAnimationFrame(frame);
    }

    const control = L.Routing.control({
        waypoints: [L.latLng(latO, lngO), L.latLng(latD, lngD)],
        createMarker: () => null,
        lineOptions: { addWaypoints: false, styles: [{ color: '#472825', weight: 4, opacity: 0.1 }] },
        show: false
    }).addTo(map);

    let ok = false;
    control.on('routesfound', (e) => {
        console.log('routesfound event', e);
        if(!ok) { ok = true; iniciar(e.routes[0].coordinates); }
    });

    setTimeout(() => {
        if(!ok) {
            ok = true;
            let r = [];
            for(let i=0; i<=100; i++) r.push([latO+(latD-latO)*(i/100), lngO+(lngD-lngO)*(i/100)]);
            iniciar(r);
        }
    }, 1200);
});
