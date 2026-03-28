const _cs = getComputedStyle(document.documentElement);
const _swalBg = _cs.getPropertyValue('--section-bg-primary').trim() || '#1a1a1a';
const _swalColor = _cs.getPropertyValue('--text-main').trim() || '#ffffff';
const _swalBtn = _cs.getPropertyValue('--accent-main').trim() || '#d4af37';

window.onload = function () {
    const error = document.getElementById('errorMsg')?.value;
    const success = document.getElementById('successMsg')?.value;

    if (error) {
        Swal.fire({
            icon: 'warning',
            title: 'Aviso',
            text: error,
            confirmButtonColor: _swalBtn,
            background: _swalBg,
            color: _swalColor
        });
    }
    if (success) {
        Swal.fire({
            icon: 'success',
            title: 'Aplicado',
            text: success,
            timer: 1500,
            showConfirmButton: false,
            background: _swalBg,
            color: _swalColor
        });
    }
};

// Lógica para aplicar cupones
function aplicar(codigo) {
    const valInput = document.getElementById('valCupon');
    const form = document.getElementById('formCuponHidden');
    if (valInput && form) {
        valInput.value = codigo;
        form.submit();
    }
}

// Borrar ubicación
function borrarUbicacionSeleccionada() {
    const id = document.getElementById('selectUbicacionPedido')?.value;
    if (!id) {
        Swal.fire({ icon: 'info', title: 'Selecciona una dirección primero.', confirmButtonColor: _swalBtn, background: _swalBg, color: _swalColor });
        return;
    }

    Swal.fire({
        title: '¿Eliminar?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: _swalBtn,
        cancelButtonColor: '#d33',
        background: _swalBg,
        color: _swalColor
    }).then((result) => {
        if (result.isConfirmed) {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            fetch('/Ubicaciones/EliminarUbicacion/' + id, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token }
            }).then(() => location.reload());
        }
    });
}

// Checkout
document.getElementById('formCheckout')?.addEventListener('submit', function (e) {
    const ubiId = document.getElementById('selectUbicacionPedido')?.value;
    const pagoId = document.getElementById('selectMetodoPago')?.value;

    if (!ubiId || !pagoId) {
        e.preventDefault();
        Swal.fire({ icon: 'warning', title: 'Datos incompletos', text: 'Elige dirección y pago.', confirmButtonColor: _swalBtn, background: _swalBg, color: _swalColor });
        return;
    }

    document.getElementById('UbicacionIdFinal').value = ubiId;
    document.getElementById('MetodoPagoIdFinal').value = pagoId;

    Swal.fire({
        title: 'Procesando...',
        allowOutsideClick: false,
        showConfirmButton: false,
        background: _swalBg,
        color: _swalColor,
        willOpen: () => { Swal.showLoading(); }
    });
});