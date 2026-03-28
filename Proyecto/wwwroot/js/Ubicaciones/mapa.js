(function () {
    var mapFinal = null;
    var markerFinal = null;

    function initMap() {
        var $el = $('#mapa-final');
        // Evitar inicializar si el elemento no existe o el mapa ya está creado
        if ($el.length === 0 || mapFinal !== null) return;

        var lat = parseFloat($el.attr('data-lat')) || 9.9333;
        var lng = parseFloat($el.attr('data-lng')) || -84.0833;

        // 1. DETECCIÓN DE MODO (Tema)
        // Leemos el atributo data-theme del HTML para decidir colores
        var isDark = $('html').attr('data-theme') === 'dark';
        var pinColor = isDark ? '#FFCC00' : '#FF0000'; // Amarillo en oscuro, Rojo en claro

        // 2. CONFIGURACIÓN DEL MAPA
        mapFinal = L.map('mapa-final', {
            zoomControl: true,
            attributionControl: false
        }).setView([lat, lng], 16);

        // Usamos la capa Voyager que es limpia y reacciona bien a los filtros CSS
        L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png').addTo(mapFinal);

        // 3. CREACIÓN DEL ICONO PERSONALIZADO (30px)
        var iconRappi = L.divIcon({
            className: 'custom-div-icon',
            // Inyectamos el color dinámico directamente en el style
            html: '<i class="fas fa-map-marker-alt" style="color: ' + pinColor + ' !important;"></i>',
            iconSize: [30, 30],
            iconAnchor: [15, 30] // Punto exacto: mitad del ancho y total del alto
        });

        // 4. CREACIÓN DEL MARCADOR
        markerFinal = L.marker([lat, lng], {
            draggable: true,
            icon: iconRappi
        }).addTo(mapFinal);

        // Evento al terminar de arrastrar el pin
        markerFinal.on('dragend', function (e) {
            var pos = markerFinal.getLatLng();
            $('#lat-hidden').val(pos.lat.toFixed(10));
            $('#lng-hidden').val(pos.lng.toFixed(10));
        });

        // Corrección de tamaño para asegurar que cargue bien en modales
        setTimeout(function () {
            mapFinal.invalidateSize();
        }, 400);
    }

    // DISPARADORES (Triggers)
    // Si el modal ya está abierto al cargar la página
    if ($('#modalGeneral').hasClass('show')) {
        initMap();
    }

    // Cuando el modal de Bootstrap se termina de mostrar
    $(document).on('shown.bs.modal', '#modalGeneral', function () {
        initMap();
    });

    // BOTÓN DE CONFIRMACIÓN
    $(document).off('click', '#btnConfirmarFinal').on('click', '#btnConfirmarFinal', function () {
        var nombre = $('#nombre-ubicacion').val();
        var lat = $('#lat-hidden').val();
        var lng = $('#lng-hidden').val();

        if (!nombre || nombre.trim() === "") {
            alert("Por favor, escribe un nombre para la ubicación.");
            return;
        }

        // Aquí iría tu lógica de guardado AJAX
        console.log("Datos listos para guardar:", {
            nombre: nombre,
            latitud: lat,
            longitud: lng
        });
    });

})();