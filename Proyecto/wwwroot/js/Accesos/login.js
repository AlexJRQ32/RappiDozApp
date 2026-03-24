const _cs = getComputedStyle(document.documentElement);
const _swalBg = _cs.getPropertyValue('--section-bg-primary').trim();
const _swalColor = _cs.getPropertyValue('--text-main').trim();
const _swalBtn = _cs.getPropertyValue('--accent-main').trim();

document.getElementById('loginForm').addEventListener('submit', function (e) {
    e.preventDefault();

    const form = this;
    const errorDiv = document.getElementById('error-message');
    const formData = new FormData(form);

    errorDiv.style.display = "none";

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
                title: '¡Acceso Correcto!',
                text: 'Bienvenido de nuevo a Rappi\'Doz',
                showConfirmButton: false,
                timer: 1500,
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
            errorDiv.textContent = "Correo o contraseña incorrectos.";
            errorDiv.style.display = "block";

            Swal.fire({
                icon: 'error',
                title: 'Error de acceso',
                text: 'Las credenciales no coinciden. Inténtalo de nuevo.',
                confirmButtonColor: _swalBtn,
                background: _swalBg,
                color: _swalColor
            });
        }
    })
    .catch(error => {
        Swal.fire({
            icon: 'warning',
            title: 'Servidor no disponible',
            text: 'Hubo un problema de conexión. Inténtalo más tarde.',
            confirmButtonColor: _swalBtn,
            background: _swalBg,
            color: _swalColor
        });
        console.error('Error:', error);
    });
});
