document.addEventListener("DOMContentLoaded", function () {
    var mapEl = document.getElementById('map');
    if (!mapEl) return;

    // 1. OBTENER COORDENADAS INICIALES
    const latIni = parseFloat(mapEl.dataset.lat) || 9.9333;
    const lngIni = parseFloat(mapEl.dataset.lng) || -84.0833;

    // 2. DETECCIÓN DE MODO Y COLOR DEL PIN
    const isDark = document.documentElement.getAttribute('data-theme') === 'dark';
    const pinColor = isDark ? '#FFCC00' : '#FF0000';

    // 3. INICIALIZACIÓN DEL MAPA
    const map = L.map('map', {
        zoomControl: true,
        attributionControl: false
    }).setView([latIni, lngIni], 15);

    // Usamos el TileLayer estándar que reacciona a tu filtro CSS
    L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png').addTo(map);

    // 4. ICONO PERSONALIZADO (FontAwesome)
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

    // 5. FUNCIONES DE ACTUALIZACIÓN
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

    // 6. GESTIÓN DEL FORMULARIO
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
                            confirmButtonColor: '#472825'
                        }).then(() => {
                            window.location.reload();
                        });
                    } else {
                        Swal.fire({ icon: 'error', title: 'Error', text: data.message });
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

    // Ajuste de tamaño por si está dentro de un modal o contenedor dinámico
    setTimeout(() => { map.invalidateSize(); }, 400);
});