const _cs = getComputedStyle(document.documentElement);
const _swalBg = _cs.getPropertyValue('--section-bg-primary').trim();
const _swalColor = _cs.getPropertyValue('--text-main').trim();
const _swalBtn = _cs.getPropertyValue('--accent-main').trim();

window.onload = function() {
    const error = document.getElementById('errorMsg').value;
    const success = document.getElementById('successMsg').value;
    if (error) Swal.fire({ icon: 'warning', title: 'Aviso', text: error, confirmButtonColor: _swalBtn, background: _swalBg, color: _swalColor });
    if (success) Swal.fire({ icon: 'success', title: 'Aplicado', text: success, timer: 1500, showConfirmButton: false, background: _swalBg, color: _swalColor });
};

function aplicar(codigo) {
    document.getElementById('valCupon').value = codigo;
    document.getElementById('formCuponHidden').submit();
}

function borrarUbicacionSeleccionada() {
    const id = document.getElementById('selectUbicacionPedido').value;
    if (!id) {
        Swal.fire({ icon: 'info', title: 'Selecciona una', text: 'Primero elige la dirección que quieres borrar.', confirmButtonColor: _swalBtn, background: _swalBg, color: _swalColor });
        return;
    }

    Swal.fire({
        title: '¿Eliminar dirección?',
        text: "No podrás deshacer esto.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: _swalBtn,
        cancelButtonColor: '#d33',
        confirmButtonText: 'Sí, eliminar',
        cancelButtonText: 'Cancelar',
        background: _swalBg,
        color: _swalColor
    }).then((result) => {
        if (result.isConfirmed) {
            fetch('/Ubicaciones/EliminarUbicacion/' + id, {
                method: 'POST',
                headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value }
            }).then(response => {
                if (response.ok) {
                    Swal.fire({ icon: 'success', title: 'Borrado', showConfirmButton: false, timer: 1000 }).then(() => {
                        location.reload();
                    });
                }
            });
        }
    });
}

document.getElementById('formCheckout')?.addEventListener('submit', function(e) {
    const comboUbi = document.getElementById('selectUbicacionPedido');
    const ubicacionId = comboUbi ? comboUbi.value : "";

    if (!ubicacionId) {
        e.preventDefault();
        Swal.fire({
            icon: 'warning',
            title: 'Falta Dirección',
            text: 'Por favor, selecciona a dónde debemos enviar tu pedido.',
            confirmButtonColor: _swalBtn,
            background: _swalBg,
            color: _swalColor
        });
        if (comboUbi) comboUbi.style.borderColor = "#e74c3c";
        return;
    }

    document.getElementById('UbicacionIdFinal').value = ubicacionId;

    Swal.fire({
        title: 'Confirmando...',
        text: 'Estamos enviando tu pedido a la cocina',
        allowOutsideClick: false,
        showConfirmButton: false,
        background: _swalBg,
        willOpen: () => { Swal.showLoading(); }
    });
});
