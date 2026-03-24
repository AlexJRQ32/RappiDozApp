function ejecutarArranque(contenedor) {
    if (contenedor.dataset.requiresLogin === 'true') {
        Swal.fire({ title: '¡Sesión Requerida!', text: 'Inicia sesión para arrancar cupones.', icon: 'info' });
        return;
    }

    const parteBlanca = contenedor.querySelector('.voucher-bottom');
    parteBlanca.classList.add('arrancar-anim');

    setTimeout(() => {
        contenedor.querySelector('form').submit();
    }, 700);
}
