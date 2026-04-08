(function () {
    var mapFinal = null;
    var markerFinal = null;

    // #region Inicialización
    function initMap() {
        var $el = $('#mapa-final');
        if ($el.length === 0 || mapFinal !== null) return;

        var lat = parseFloat($el.attr('data-lat')) || 9.9333;
        var lng = parseFloat($el.attr('data-lng')) || -84.0833;

        var isDark = $('html').attr('data-theme') === 'dark';
        var pinColor = isDark ? '#FFCC00' : '#FF0000';

        mapFinal = L.map('mapa-final', {
            zoomControl: true,
            attributionControl: false
        }).setView([lat, lng], 16);

        L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png').addTo(mapFinal);

        var iconRappi = L.divIcon({
            className: 'custom-div-icon',
            html: '<i class="fas fa-map-marker-alt" style="color: ' + pinColor + ' !important;"></i>',
            iconSize: [30, 30],
            iconAnchor: [15, 30]
        });

        markerFinal = L.marker([lat, lng], {
            draggable: true,
            icon: iconRappi
        }).addTo(mapFinal);

        markerFinal.on('dragend', function (e) {
            var pos = markerFinal.getLatLng();
            $('#lat-hidden').val(pos.lat.toFixed(10));
            $('#lng-hidden').val(pos.lng.toFixed(10));
        });

        setTimeout(function () {
            mapFinal.invalidateSize();
        }, 400);
    }
    // #endregion

    // #region Triggers
    if ($('#modalGeneral').hasClass('show')) {
        initMap();
    }

    $(document).on('shown.bs.modal', '#modalGeneral', function () {
        initMap();
    });
    // #endregion

    // #region Confirmación
    $(document).off('click', '#btnConfirmarFinal').on('click', '#btnConfirmarFinal', function () {
        var lat = $('#lat-hidden').val();
        var lng = $('#lng-hidden').val();
        var nombreInput = document.getElementById('nombre-ubicacion');

        if (nombreInput) {
            var nombre = nombreInput.value.trim();
            if (!nombre) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Campo requerido',
                    text: 'Por favor, escribe un nombre para la ubicación.',
                    confirmButtonColor: getComputedStyle(document.documentElement).getPropertyValue('--swal-btn').trim() || '#d97b4a',
                    background: getComputedStyle(document.documentElement).getPropertyValue('--section-bg-primary').trim() || '#1a1a1a',
                    color: getComputedStyle(document.documentElement).getPropertyValue('--text-main').trim() || '#ffffff'
                });
                return;
            }
            var btn = document.getElementById('btnConfirmarFinal');
            if (btn) btn.disabled = true;
            fetch('/Ubicaciones/GuardarUbicacion', {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'Latitud=' + encodeURIComponent(lat) + '&Longitud=' + encodeURIComponent(lng) + '&nombreUbicacion=' + encodeURIComponent(nombre)
            })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.success) {
                    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalGeneral')).hide();
                    Swal.fire({
                        icon: 'success',
                        title: '¡Guardado!',
                        text: data.message,
                        timer: 1800,
                        showConfirmButton: false,
                        background: getComputedStyle(document.documentElement).getPropertyValue('--section-bg-primary').trim() || '#1a1a1a',
                        color: getComputedStyle(document.documentElement).getPropertyValue('--text-main').trim() || '#ffffff'
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error',
                        text: data.message,
                        background: getComputedStyle(document.documentElement).getPropertyValue('--section-bg-primary').trim() || '#1a1a1a',
                        color: getComputedStyle(document.documentElement).getPropertyValue('--text-main').trim() || '#ffffff'
                    });
                }
            })
            .catch(function () {
                Swal.fire({
                    icon: 'error',
                    title: 'Error de conexión',
                    text: 'No se pudo guardar la ubicación.',
                    background: getComputedStyle(document.documentElement).getPropertyValue('--section-bg-primary').trim() || '#1a1a1a',
                    color: getComputedStyle(document.documentElement).getPropertyValue('--text-main').trim() || '#ffffff'
                });
            })
            .finally(function () {
                if (btn) btn.disabled = false;
            });
            return;
        }

        var latField = document.getElementById('Latitud');
        var lngField = document.getElementById('Longitud');
        var textoSpan = document.getElementById('texto-ubicacion');

        if (latField) latField.value = lat;
        if (lngField) lngField.value = lng;
        if (textoSpan) textoSpan.innerHTML = '<i class="fas fa-check-circle"></i> Ubicación seleccionada';

        var modalEl = document.getElementById('modalGeneral');
        if (modalEl && typeof bootstrap !== 'undefined') {
            bootstrap.Modal.getOrCreateInstance(modalEl).hide();
        }
    });
    // #endregion

})();