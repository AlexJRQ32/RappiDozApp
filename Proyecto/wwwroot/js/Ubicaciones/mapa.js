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
        var nombre = $('#nombre-ubicacion').val();
        var lat = $('#lat-hidden').val();
        var lng = $('#lng-hidden').val();

        if (!nombre || nombre.trim() === "") {
            alert("Por favor, escribe un nombre para la ubicación.");
            return;
        }

        console.log("Datos listos para guardar:", {
            nombre: nombre,
            latitud: lat,
            longitud: lng
        });
    });
    // #endregion

})();