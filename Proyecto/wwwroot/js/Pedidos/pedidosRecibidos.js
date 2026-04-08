const _sv = n => getComputedStyle(document.documentElement).getPropertyValue(n).trim();

function cambiarEstado(pedidoId, nuevoEstado) {
    const token = document.querySelector('#formEstado input[name="__RequestVerificationToken"]')?.value ?? '';

    fetch('/Pedidos/ActualizarEstado', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: `id=${pedidoId}&estado=${encodeURIComponent(nuevoEstado)}&__RequestVerificationToken=${encodeURIComponent(token)}`
    })
        .then(r => r.json())
        .then(res => {
            if (res.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Actualizado',
                    text: res.message,
                    timer: 1500,
                    showConfirmButton: false,
                    background: _sv('--modal-shell-bg'),
                    color: _sv('--modal-text')
                }).then(() => location.reload());
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Error',
                    text: res.message,
                    confirmButtonColor: _sv('--swal-btn'),
                    background: _sv('--modal-shell-bg'),
                    color: _sv('--modal-text')
                });
            }
        })
        .catch(() => {
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'No se pudo conectar con el servidor.',
                confirmButtonColor: _sv('--swal-btn'),
                background: _sv('--modal-shell-bg'),
                color: _sv('--modal-text')
            });
        });
}
