const _cs = getComputedStyle(document.documentElement);
const _swalBg = _cs.getPropertyValue('--section-bg-primary').trim();
const _swalColor = _cs.getPropertyValue('--text-main').trim();
const _swalBtn = _cs.getPropertyValue('--accent-main').trim();

document.getElementById('restaurantForm').addEventListener('submit', function (e) {
    e.preventDefault();

    const form = this;
    const errorDiv = document.getElementById('error-message');
    const formData = new FormData(form);

    errorDiv.classList.add('hidden-feedback');

    fetch(form.action, {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
    .then(response => {
        if (response.redirected) {
            Swal.fire({
                icon: 'success',
                title: '¡Negocio Registrado!',
                text: 'Tu restaurante se ha creado correctamente.',
                showConfirmButton: false,
                timer: 2000,
                background: _swalBg,
                color: _swalColor
            }).then(() => {
                window.location.href = response.url;
            });
            return;
        }
        return response.text();
    })
    .then(data => {
        if (data) {
            errorDiv.textContent = "Hubo un error al guardar los datos del restaurante.";
            errorDiv.classList.remove('hidden-feedback');

            Swal.fire({
                icon: 'error',
                title: 'No se pudo registrar',
                text: 'Verifica que todos los campos sean válidos e inténtalo de nuevo.',
                confirmButtonColor: _swalBtn,
                background: _swalBg,
                color: _swalColor
            });
        }
    })
    .catch(error => {
        Swal.fire({
            icon: 'warning',
            title: 'Error de servidor',
            text: 'No pudimos conectar con el servidor. Inténtalo más tarde.',
            confirmButtonColor: _swalBtn,
            background: _swalBg,
            color: _swalColor
        });
        console.error('Error:', error);
    });
});
