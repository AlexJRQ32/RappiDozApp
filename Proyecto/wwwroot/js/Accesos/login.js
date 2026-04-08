const _cs = getComputedStyle(document.documentElement);
const _swalBg = _cs.getPropertyValue('--section-bg-primary').trim() || '#fffdf9';
const _swalColor = _cs.getPropertyValue('--text-main').trim() || '#542f28';
const _swalBtn = _cs.getPropertyValue('--swal-btn').trim() || '#5a322d';

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

(function () {
    var toggleBtn = document.getElementById('toggle-password');
    var passwordInput = document.getElementById('password');
    if (!toggleBtn || !passwordInput) return;

    toggleBtn.addEventListener('click', function () {
        var isHidden = passwordInput.type === 'password';
        passwordInput.type = isHidden ? 'text' : 'password';
        toggleBtn.className = isHidden ? 'fa-solid fa-lock-open' : 'fa-solid fa-lock';
        passwordInput.focus();
    });
})();

(function () {
    var correoInput  = document.getElementById('correo');
    var passwordInput = document.getElementById('password');

    function enableAutocomplete() {
        if (correoInput)  correoInput.removeAttribute('readonly');
        if (passwordInput) passwordInput.removeAttribute('readonly');
    }

    if (correoInput)  correoInput.addEventListener('focus',  enableAutocomplete, { once: true });
    if (passwordInput) passwordInput.addEventListener('focus', enableAutocomplete, { once: true });
})();
