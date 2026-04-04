document.addEventListener("DOMContentLoaded", function () {
    var mapEl = document.getElementById('map');
    if (!mapEl) return;

    const _v = (n) => getComputedStyle(document.documentElement).getPropertyValue(n).trim();

    const latIni = parseFloat(mapEl.dataset.lat) || 9.9333;
    const lngIni = parseFloat(mapEl.dataset.lng) || -84.0833;

    const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
    const pinColor = isDark ? '#FFCC00' : '#FF0000';

    const map = L.map('map', {
        zoomControl: true,
        attributionControl: false
    }).setView([latIni, lngIni], 15);

    L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png').addTo(map);

    const iconRappi = L.divIcon({
        className: 'custom-div-icon',
        html: `<i class="fas fa-map-marker-alt" style="color: ${pinColor} !important;"></i>`,
        iconSize: [30, 30],
        iconAnchor: [15, 30]
    });

    const marker = L.marker([latIni, lngIni], {
        draggable: true,
        icon: iconRappi
    }).addTo(map);

    function actualizarInputs(lat, lng) {
        const inputLat = document.getElementById("Latitud");
        const inputLng = document.getElementById("Longitud");
        if (inputLat) inputLat.value = lat.toFixed(10);
        if (inputLng) inputLng.value = lng.toFixed(10);
    }

    marker.on('dragend', function (e) {
        const pos = e.target.getLatLng();
        actualizarInputs(pos.lat, pos.lng);
    });

    map.on('click', function (e) {
        marker.setLatLng(e.latlng);
        actualizarInputs(e.latlng.lat, e.latlng.lng);
    });

    const formUbi = document.getElementById('formUbicacion');
    if (formUbi) {
        formUbi.addEventListener('submit', function (e) {
            e.preventDefault();

            const btn = document.getElementById('btn-confirmar-mapa');
            const originalHTML = btn.innerHTML;

            btn.disabled = true;
            btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Guardando...';

            const formData = new FormData(this);
            const token = document.querySelector('input[name="__RequestVerificationToken"]');

            fetch(this.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': token ? token.value : ''
                }
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        Swal.fire({
                            icon: 'success',
                            title: '¡Dirección Guardada!',
                            text: data.message,
                            confirmButtonColor: _v('--rd-cafe'),
                            background: _v('--modal-shell-bg'),
                            color: _v('--modal-text')
                        }).then(() => {
                            window.location.reload();
                        });
                    } else {
                        Swal.fire({ icon: 'error', title: 'Error', text: data.message, background: _v('--modal-shell-bg'), color: _v('--modal-text') });
                        btn.disabled = false;
                        btn.innerHTML = originalHTML;
                    }
                })
                .catch(err => {
                    console.error("Error:", err);
                    btn.disabled = false;
                    btn.innerHTML = originalHTML;
                });
        });
    }

    setTimeout(() => { map.invalidateSize(); }, 400);
});