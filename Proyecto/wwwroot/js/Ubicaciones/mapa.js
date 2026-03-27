(function() {
    var mapFinal = null;
    var markerFinal = null;

    function initMap() {
        var $el = $('#mapa-final');
        if ($el.length === 0 || mapFinal !== null) return;

        var lat = parseFloat($el.attr('data-lat'));
        var lng = parseFloat($el.attr('data-lng'));

        mapFinal = L.map('mapa-final', {
            zoomControl: true,
            attributionControl: false
        }).setView([lat, lng], 16);

        RappiDozMap.addTileLayer(mapFinal);

        var iconRappi = L.icon({
            iconUrl: 'https://cdn-icons-png.flaticon.com/512/684/684908.png',
            iconSize: [46, 46],
            iconAnchor: [23, 46],
            popupAnchor: [0, -40]
        });

        markerFinal = L.marker([lat, lng], {
            draggable: true,
            icon: iconRappi
        }).addTo(mapFinal);

        markerFinal.on('dragend', function(e) {
            var pos = markerFinal.getLatLng();
            $('#lat-hidden').val(pos.lat.toFixed(10));
            $('#lng-hidden').val(pos.lng.toFixed(10));
        });

        setTimeout(function() {
            mapFinal.invalidateSize();
        }, 400);
    }

    if ($('#modalGeneral').hasClass('show')) {
        initMap();
    }
    $(document).on('shown.bs.modal', '#modalGeneral', function() {
        initMap();
    });

    $(document).off('click', '#btnConfirmarFinal').on('click', '#btnConfirmarFinal', function() {
        var nombre = $('#nombre-ubicacion').val();

        if (!nombre || nombre.trim() === "") {
            Swal.fire({
                icon: 'warning',
                title: 'Falta un detalle',
                text: 'Por favor, escribe un nombre para tu dirección.',
                confirmButtonColor: '#472825',
                background: _swalBg,
                color: _swalColor
            });
            return;
        }

        var btn = $(this);
        btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Guardando...');

        var data = {
            Latitud: $('#lat-hidden').val(),
            Longitud: $('#lng-hidden').val(),
            nombreUbicacion: nombre,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
        };

        $.post('/Ubicaciones/GuardarUbicacion', data)
            .done(function(res) {
                if (res.success) {
                    Swal.fire({
                        icon: 'success',
                        title: '¡Ubicación lista!',
                        text: res.message,
                        showConfirmButton: false,
                        timer: 1800,
                        background: _swalBg,
                        color: _swalColor
                    }).then(() => {
                        window.location.reload();
                    });
                } else {
                    Swal.fire('Error', res.message, 'error');
                    btn.prop('disabled', false).text('CONFIRMAR Y GUARDAR');
                }
            })
            .fail(function() {
                Swal.fire('Error', 'No se pudo conectar con el servidor. Verifica tu conexión.', 'error');
                btn.prop('disabled', false).text('CONFIRMAR Y GUARDAR');
            });
    });
})();
