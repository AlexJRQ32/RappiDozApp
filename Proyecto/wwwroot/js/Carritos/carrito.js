// #region Variables
const _cs = getComputedStyle(document.documentElement);
const _swalBg = _cs.getPropertyValue('--section-bg-primary').trim() || '#1a1a1a';
const _swalColor = _cs.getPropertyValue('--text-main').trim() || '#ffffff';
const _swalBtn = _cs.getPropertyValue('--accent-main').trim() || '#d4af37';
const _swalDanger = _cs.getPropertyValue('--danger-strong').trim() || '#d33';
// #endregion

// #region Persistencia de selecciones
function guardarSelecciones() {
    var ubi = document.getElementById('selectUbicacionPedido');
    var pago = document.getElementById('selectMetodoPago');
    if (ubi && ubi.value) sessionStorage.setItem('carritoUbicacionId', ubi.value);
    if (pago && pago.value) sessionStorage.setItem('carritoMetodoPagoId', pago.value);
}

window.addEventListener('beforeunload', guardarSelecciones);

// #endregion

// #region Inicialización
window.onload = function () {
    var error = document.getElementById('errorMsg')?.value;
    var success = document.getElementById('successMsg')?.value;

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

    var selUbi
    var selPago = document.getElementById('selectMetodoPago');
    var savedUbi = sessionStorage.getItem('carritoUbicacionId');
    var savedPago = sessionStorage.getItem('carritoMetodoPagoId');

    if (selUbi && savedUbi) {
        var existe = Array.from(selUbi.options).some(function (o) { return o.value === savedUbi; });
        if (existe) selUbi.value = savedUbi;
    }
    if (selPago && savedPago) {
        var existe2 = Array.from(selPago.options).some(function (o) { return o.value === savedPago; });
        if (existe2) selPago.value = savedPago;
    }
};
// #endregion

// #region Cupones
function aplicar(codigo) {
    var valInput = document.getElementById('valCupon');
    var form = document.getElementById('formCuponHidden');
    if (valInput && form) {
        guardarSelecciones();
        valInput.value = codigo;
        form.submit();
    }
}
// #endregion

// #region Ubicaciones
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
        cancelButtonColor: _swalDanger,
        background: _swalBg,
        color: _swalColor
    }).then((result) => {
        if (result.isConfirmed) {
            sessionStorage.removeItem('carritoUbicacionId');
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            fetch('/Ubicaciones/EliminarUbicacion/' + id, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token }
            }).then(() => location.reload());
        }
    });
}
// #endregion

// #region Checkout
document.getElementById('formCheckout')?.addEventListener('submit', function (e) {
    const ubiId = document.getElementById('selectUbicacionPedido')?.value;
    const pagoId = document.getElementById('selectMetodoPago')?.value;

    if (!ubiId || !pagoId) {
        e.preventDefault();
        Swal.fire({ icon: 'warning', title: 'Datos incompletos', text: 'Elige dirección y pago.', confirmButtonColor: _swalBtn, background: _swalBg, color: _swalColor });
        return;
    }

    sessionStorage.removeItem('carritoUbicacionId');
    sessionStorage.removeItem('carritoMetodoPagoId');

    Swal.fire({
        title: 'Procesando...',
        allowOutsideClick: false,
        showConfirmButton: false,
        background: _swalBg,
        color: _swalColor,
        willOpen: () => { Swal.showLoading(); }
    });
});
// #endregion