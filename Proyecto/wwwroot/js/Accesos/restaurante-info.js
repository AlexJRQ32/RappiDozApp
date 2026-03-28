// Obtenemos estilos de la raíz para SweetAlert
const _cs = getComputedStyle(document.documentElement);
const _swalBg = _cs.getPropertyValue('--section-bg-primary').trim() || '#1a1a1a';
const _swalColor = _cs.getPropertyValue('--text-main').trim() || '#ffffff';
const _swalBtn = _cs.getPropertyValue('--accent-main').trim() || '#472825';

// ... (Tus variables de estilo _swalBg, etc.)

document.getElementById('restaurantForm').addEventListener('submit', function (e) {
    e.preventDefault();

    const form = this;
    const btn = document.getElementById('button');
    const formData = new FormData(form);

    // CAPTURA MANUAL PARA EVITAR ERRORES DE PUNTO/COMA
    formData.append('LatitudStr', document.getElementById('Latitud').value);
    formData.append('LongitudStr', document.getElementById('Longitud').value);

    if (!formData.get('LatitudStr') || formData.get('LatitudStr') == "0") {
        Swal.fire({
            icon: 'warning',
            title: 'Ubicación vacía',
            text: 'Debes marcar el local en el mapa.',
            confirmButtonColor: _swalBtn,
            background: _swalBg,
            color: _swalColor
        });
        return;
    }

    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Guardando...';

    fetch(form.action, {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                Swal.fire({
                    icon: 'success',
                    title: '¡Negocio Registrado!',
                    timer: 2000,
                    showConfirmButton: false,
                    background: _swalBg,
                    color: _swalColor
                }).then(() => {
                    window.location.href = data.redirectUrl;
                });
            } else {
                throw new Error(data.message);
            }
        })
        .catch(error => {
            btn.disabled = false;
            btn.textContent = 'Registrar Negocio';

            Swal.fire({
                icon: 'error',
                title: 'No se pudo registrar',
                text: error.message || 'Verifica los campos e intenta de nuevo.',
                confirmButtonColor: _swalBtn,
                background: _swalBg,
                color: _swalColor
            });
        });
});