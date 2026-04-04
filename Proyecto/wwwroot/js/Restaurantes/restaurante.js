function agregarConAnimacion(button) {
    const form = button.closest('form');
    const bolsaBase = document.getElementById('bolsa-fancy-vuelo');
    const navCarrito = document.querySelector('.fa-shopping-cart') || document.querySelector('.fa-cart-shopping') || button;

    const rectBtn = button.getBoundingClientRect();
    const rectNav = navCarrito.getBoundingClientRect();

    const clon = bolsaBase.cloneNode(true);
    clon.style.display = 'block';
    clon.style.left = (rectBtn.left + rectBtn.width / 2) + 'px';
    clon.style.top = rectBtn.top + 'px';

    const xDist = rectNav.left - (rectBtn.left + rectBtn.width / 2);
    const yDist = rectNav.top - rectBtn.top;

    clon.style.setProperty('--x-dist', xDist + 'px');
    clon.style.setProperty('--y-dist', yDist + 'px');

    document.body.appendChild(clon);
    clon.classList.add('vuelo-kraft');

    setTimeout(() => clon.remove(), 900);

    const formData = new FormData(form);

    fetch(form.action, {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': form.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            let badge = document.querySelector('.cart-badge');
            if (data.totalItems > 0) {
                if (!badge) {
                    badge = document.createElement('span');
                    badge.className = 'cart-badge';
                    const cartContainer = document.querySelector('.cart-container');
                    if (cartContainer) cartContainer.appendChild(badge);
                }
                if (badge) badge.textContent = data.totalItems;
            }

            navCarrito.classList.add('fa-bounce');
            setTimeout(() => navCarrito.classList.remove('fa-bounce'), 1000);
        }
    })
    .catch(err => console.error("Error al añadir:", err));
}
