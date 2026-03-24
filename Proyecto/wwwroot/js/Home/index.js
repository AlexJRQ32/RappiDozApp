document.addEventListener("DOMContentLoaded", function () {
    var mapEl = document.getElementById('map');
    if (!mapEl) return;

    const latIni = parseFloat(mapEl.dataset.lat);
    const lngIni = parseFloat(mapEl.dataset.lng);

    const map = L.map('map').setView([latIni, lngIni], 15);
    RappiDozMap.addTileLayer(map);

    const iconRappi = L.icon({
        iconUrl: 'https://cdn-icons-png.flaticon.com/512/684/684908.png',
        iconSize: [46, 46],
        iconAnchor: [23, 46],
        popupAnchor: [0, -40]
    });

    const marker = L.marker([latIni, lngIni], {
        draggable: true,
        icon: iconRappi
    }).addTo(map);

    function actualizarInputs(lat, lng) {
        document.getElementById("Latitud").value = lat;
        document.getElementById("Longitud").value = lng;
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
            btn.disabled = true;
            btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Guardando...';

            const formData = new FormData(this);

            fetch(this.action, {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    Swal.fire({
                        icon: 'success',
                        title: '¡Dirección Guardada!',
                        text: data.message,
                        confirmButtonColor: '#472825'
                    }).then(() => {
                        window.location.reload();
                    });
                } else {
                    Swal.fire({ icon: 'error', title: 'Error', text: data.message });
                    btn.disabled = false;
                    btn.innerHTML = '<i class="fas fa-map-pin me-2"></i> GUARDAR ESTA DIRECCIÓN';
                }
            })
            .catch(err => {
                console.error("Error:", err);
                btn.disabled = false;
            });
        });
    }
});
