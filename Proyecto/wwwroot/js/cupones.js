// #region Cupones
function copiarCodigo(texto) {
    navigator.clipboard.writeText(texto).then(() => {
        const _v = (n) => getComputedStyle(document.documentElement).getPropertyValue(n).trim();
        const Toast = Swal.mixin({
            toast: true,
            position: 'top-end',
            showConfirmButton: false,
            timer: 2500,
            timerProgressBar: true,
            background: _v('--surface-elevated'),
            color: _v('--text-main')
        });

        Toast.fire({
            icon: 'success',
            title: '¡Código ' + texto + ' copiado!'
        });
    }).catch(err => {
        console.error('Error al copiar: ', err);
    });
}