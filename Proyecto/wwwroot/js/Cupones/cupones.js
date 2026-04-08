function ejecutarArranque(contenedor) {
    if (contenedor.dataset.requiresLogin === 'true') {
        const _v = (n) => getComputedStyle(document.documentElement).getPropertyValue(n).trim();
        Swal.fire({
            title: '¡Sesión Requerida!',
            text: 'Inicia sesión para arrancar cupones.',
            icon: 'info',
            confirmButtonColor: _v('--swal-btn'),
            background: _v('--modal-shell-bg'),
            color: _v('--modal-text')
        });
        return;
    }

    const parteBlanca = contenedor.querySelector('.voucher-bottom');
    parteBlanca.classList.add('arrancar-anim');

    setTimeout(() => {
        contenedor.querySelector('form').submit();
    }, 700);
}
