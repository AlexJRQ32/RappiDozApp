$(document).ready(function () {
    $('#menuToggle, #overlay').click(function () {
        $('#sidebar').toggleClass('active');
        $('#overlay').toggleClass('show');
    });
});

// VARIABLES DE COLOR GLOBALES (Para que funcionen en todas las funciones de abajo)
const getBg = () => getComputedStyle(document.documentElement).getPropertyValue('--modal-shell-bg').trim();
const getColor = () => getComputedStyle(document.documentElement).getPropertyValue('--modal-text').trim();

function cargarSeccion(tipo) {
    $('#sidebar').removeClass('active');
    $('#overlay').removeClass('show');

    const contenedor = $("#contenedor-dinamico");
    contenedor.html('<div class="text-center p-5"><div class="spinner-border text-warning"></div></div>');

    contenedor.load('/Dashboard/Get' + tipo, function (res, status, xhr) {
        if (status === "error") {
            Swal.fire({
                title: 'Error',
                text: 'No se pudo cargar la sección ' + tipo,
                icon: 'error',
                background: getBg(), // Agregado
                color: getColor()    // Agregado
            });
        }
    });
}

function abrirForm(entidad, accion, id = 0) {
    const url = `/Dashboard/GetForm?entidad=${entidad}&accion=${accion}&id=${id}`;
    $("#modalContainerBody").html('<div class="text-center p-5"><div class="spinner-border text-warning"></div></div>');

    const modalEl = document.getElementById('rdModal');
    const instancia = bootstrap.Modal.getOrCreateInstance(modalEl);
    instancia.show();

    $("#modalContainerBody").load(url, function (res, status, xhr) {
        if (status === "error") {
            $("#modalContainerBody").html('<div class="alert alert-danger m-3">Error al cargar formulario.</div>');
        }
    });
}

function obtenerRutaEntidad(entidad) {
    switch (entidad) {
        case 'Producto': return 'Productos';
        case 'Restaurante': return 'Restaurantes';
        case 'Usuario': return 'Usuarios';
        case 'Cupon': return 'Cupones';
        default: return entidad;
    }
}

function guardarDatos(entidad) {
    const form = document.getElementById('form' + entidad);
    if (!form) return;

    const formData = new FormData(form);
    const btn = event?.target;
    if (btn) btn.disabled = true;
    const rutaEntidad = obtenerRutaEntidad(entidad);

    $.ajax({
        url: `/${rutaEntidad}/Guardar`,
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (res) {
            if (btn) btn.disabled = false;
            if (res.success) {
                bootstrap.Modal.getOrCreateInstance(document.getElementById('rdModal')).hide();
                Swal.fire({
                    title: '¡Éxito!',
                    text: res.message,
                    icon: 'success',
                    confirmButtonColor: '#472825',
                    background: getBg(), // Agregado
                    color: getColor()    // Agregado
                });

                let sec = (entidad === 'Producto') ? 'Menus' : (entidad === 'Restaurante') ? 'Restaurantes' : (entidad === 'Usuario') ? 'Usuarios' : entidad + 'es';
                cargarSeccion(sec);
            } else {
                Swal.fire({
                    title: 'Error',
                    text: res.message,
                    icon: 'error',
                    background: getBg(), // Agregado
                    color: getColor()    // Agregado
                });
            }
        }
    });
}

function eliminarRegistro(entidad, id) {
    const rutaEntidad = obtenerRutaEntidad(entidad);

    Swal.fire({
        title: '¿Confirmar eliminación?',
        text: "No podrás revertir esto",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#472825',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        background: getBg(), // Agregado
        color: getColor()    // Agregado
    }).then((result) => {
        if (result.isConfirmed) {
            $.post(`/${rutaEntidad}/Eliminar`, { id: id }, function (res) {
                if (res.success) {
                    Swal.fire({
                        title: 'Eliminado',
                        text: res.message,
                        icon: 'success',
                        background: getBg(), // Agregado
                        color: getColor()    // Agregado
                    });

                    let sec;
                    if (entidad === 'Producto') sec = 'Menus';
                    else if (entidad === 'Restaurante') sec = 'Restaurantes';
                    else if (entidad === 'Usuario') sec = 'Usuarios';
                    else sec = entidad + 'es';

                    cargarSeccion(sec);
                } else {
                    Swal.fire({
                        title: 'Error',
                        text: res.message,
                        icon: 'error',
                        background: getBg(), // Agregado
                        color: getColor()    // Agregado
                    });
                }
            });
        }
    });
}