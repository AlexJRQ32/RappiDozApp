const _cs = getComputedStyle(document.documentElement);
const _swalBg = _cs.getPropertyValue('--section-bg-primary').trim() || '#fffdf9';
const _swalColor = _cs.getPropertyValue('--text-main').trim() || '#542f28';
const _swalBtn = _cs.getPropertyValue('--accent-main').trim() || '#d3ab80';

document.getElementById('registerForm').addEventListener('submit', function (e) {
    e.preventDefault();

    const form = this;
    const pass = document.getElementById('PasswordHash').value;
    const confirmPass = document.getElementById('confirmarPassword').value;
    const errorDiv = document.getElementById('error-message');
    const formData = new FormData(form);

    errorDiv.classList.add('hidden-feedback');

    if (pass !== confirmPass) {
        Swal.fire({
            icon: 'error',
            title: 'Contraseñas diferentes',
            text: 'Asegúrate de que ambas contraseñas sean idénticas.',
            confirmButtonColor: _swalBtn,
            background: _swalBg,
            color: _swalColor
        });
        return;
    }

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
                title: '¡Cuenta creada!',
                text: 'Bienvenido a la familia Rappi\'Doz',
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
            errorDiv.textContent = "El correo ya está registrado o los datos son inválidos.";
            errorDiv.classList.remove('hidden-feedback');

            Swal.fire({
                icon: 'warning',
                title: 'No se pudo registrar',
                text: 'Parece que este correo ya tiene una cuenta activa.',
                confirmButtonColor: _swalBtn,
                background: _swalBg,
                color: _swalColor
            });
        }
    })
    .catch(error => {
        Swal.fire({
            icon: 'error',
            title: 'Error de conexión',
            text: 'Inténtalo de nuevo en unos minutos.',
            confirmButtonColor: _swalBtn,
            background: _swalBg,
            color: _swalColor
        });
        console.error('Error:', error);
    });
});
