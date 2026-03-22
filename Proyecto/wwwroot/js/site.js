// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function openSharedModal(url) {
    const modalBody = document.getElementById('modalBodyGeneral');
    const modalEl = document.getElementById('modalGeneral');
    if (!modalBody || !modalEl || typeof bootstrap === 'undefined') return;

    modalBody.innerHTML = '<div class="text-center p-5"><div class="spinner-border text-warning"></div></div>';
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    modal.show();

    if (typeof $ === 'function') {
        $('#modalBodyGeneral').load(url, function (response, status) {
            if (status === 'error') {
                modalBody.innerHTML = '<div class="alert alert-danger m-3">No se pudo cargar el contenido.</div>';
            }
        });
        return;
    }

    fetch(url, {
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(async response => {
            const html = await response.text();
            if (!response.ok) {
                throw new Error(html || 'No se pudo cargar el contenido.');
            }
            modalBody.innerHTML = html;
        })
        .catch(error => {
            modalBody.innerHTML = '<div class="alert alert-danger m-3">' + error.message + '</div>';
        });
}

window.abrirMapa = function () {
    openSharedModal('/Ubicaciones/Mapa');
};

window.abrirPerfil = function () {
    openSharedModal('/Usuarios/Perfil');
};

window.abrirCalificar = function () {
    openSharedModal('/Valoraciones/Crear');
};

document.addEventListener('change', function (e) {
    if (e.target && e.target.id === 'fotoInput' && e.target.files && e.target.files[0]) {
        const reader = new FileReader();
        reader.onload = function (ev) {
            const preview = document.getElementById('previewPerfil');
            if (preview) preview.src = ev.target.result;
        };
        reader.readAsDataURL(e.target.files[0]);
    }
});

document.addEventListener('click', function (e) {
    const star = e.target.closest('#contenedorEstrellas i');
    if (!star) return;

    const value = parseInt(star.getAttribute('data-value') || '0', 10);
    const input = document.getElementById('inputEstrellas');
    const stars = document.querySelectorAll('#contenedorEstrellas i');
    if (input) input.value = value;

    stars.forEach(s => {
        const starValue = parseInt(s.getAttribute('data-value') || '0', 10);
        s.classList.toggle('active', starValue <= value);
    });
});

document.addEventListener('submit', function (e) {
    const perfilForm = e.target.closest('#formPerfilUsuario');
    if (perfilForm) {
        e.preventDefault();
        const data = new FormData(perfilForm);

        fetch('/Usuarios/GuardarPerfil', {
            method: 'POST',
            body: data,
            headers: {
                'RequestVerificationToken': perfilForm.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        })
            .then(r => r.json())
            .then(res => {
                if (res.success) {
                    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalGeneral')).hide();
                    Swal.fire({ icon: 'success', title: 'Éxito', text: res.message, confirmButtonColor: '#472825' })
                        .then(() => window.location.reload());
                } else {
                    Swal.fire('Error', res.message, 'error');
                }
            })
            .catch(() => Swal.fire('Error', 'No se pudo guardar tu perfil.', 'error'));
        return;
    }

    const calificarForm = e.target.closest('#formCalificar');
    if (calificarForm) {
        e.preventDefault();
        const data = new FormData(calificarForm);

        fetch(calificarForm.action, {
            method: 'POST',
            body: data,
            headers: {
                'RequestVerificationToken': calificarForm.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        })
            .then(r => r.json())
            .then(res => {
                if (res.success) {
                    bootstrap.Modal.getOrCreateInstance(document.getElementById('modalGeneral')).hide();
                    Swal.fire({ icon: 'success', title: 'Gracias', text: res.message, confirmButtonColor: '#472825' });
                } else {
                    Swal.fire('Error', res.message || 'No se pudo guardar la valoración.', 'error');
                }
            })
            .catch(() => Swal.fire('Error', 'No se pudo guardar la valoración.', 'error'));
    }
});
