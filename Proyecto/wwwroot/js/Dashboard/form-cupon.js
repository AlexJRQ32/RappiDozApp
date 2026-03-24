(function() {
    const v = document.getElementById('vD'),
          t = document.getElementById('tD'),
          b = document.getElementById('bD');

    const upd = () => {
        const val = v.value || '0';
        const simbolo = (t.value === 'true' || t.value === 'True') ? '%' : '₡';
        b.innerText = `${val}${simbolo}`;
    };

    v.oninput = upd;
    t.onchange = upd;
    upd();
})();
