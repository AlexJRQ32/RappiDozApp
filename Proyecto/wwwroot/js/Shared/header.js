document.addEventListener('DOMContentLoaded', function() {
    const btnMenu = document.getElementById('btn-menu');
    const navbarMenu = document.getElementById('navbar-menu');
    const themeSwitch = document.getElementById('themeSwitch');
    const themeLabel = document.getElementById('themeLabel');

    function obtenerVarCss(nombre) {
        return getComputedStyle(document.documentElement).getPropertyValue(nombre).trim();
    }

    function sincronizarSwitch() {
        const temaActual = document.documentElement.getAttribute('data-theme') || 'light';
        const esOscuro = temaActual === 'dark';
        if (themeSwitch) {
            themeSwitch.checked = esOscuro;
        }
        if (themeLabel) {
            themeLabel.textContent = esOscuro ? 'Modo claro' : 'Modo oscuro';
        }
    }

    if (btnMenu && navbarMenu) {
        btnMenu.onclick = function() {
            navbarMenu.classList.toggle('active');
        };
    }

    if (themeSwitch) {
        sincronizarSwitch();
        themeSwitch.addEventListener('change', function () {
            const nuevoTema = this.checked ? 'dark' : 'light';
            document.documentElement.setAttribute('data-theme', nuevoTema);
            localStorage.setItem('rappidoz-theme', nuevoTema);
            sincronizarSwitch();
            document.dispatchEvent(new CustomEvent('rappidoz-theme-changed', { detail: nuevoTema }));
        });
    }

    window.obtenerColoresTema = function() {
        return {
            accent: obtenerVarCss('--swal-btn'),
            surface: obtenerVarCss('--surface-main'),
            text: obtenerVarCss('--text-main'),
            danger: obtenerVarCss('--danger-strong') || '#d33'
        };
    };
});

function confirmarSalida() {
    const colores = window.obtenerColoresTema ? window.obtenerColoresTema() : null;
    const logoutUrl = document.querySelector('.navbar').dataset.logoutUrl || '/Accesos/Salir';

    Swal.fire({
        title: '¿Cerrar sesión?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: colores?.accent || '#5a322d',
        cancelButtonColor: colores?.danger || '#d33',
        confirmButtonText: 'Sí, salir',
        background: colores?.surface || '#fffdf9',
        color: colores?.text || '#542f28',
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = logoutUrl;
        }
    });
}
