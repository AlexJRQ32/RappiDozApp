sessionStorage.removeItem('showLoader');

window.addEventListener('pageshow', function (e) {
    if (e.persisted) {
        var form = document.querySelector('.form');
        if (form) {
            form.style.animation = 'none';
            void form.offsetHeight;
            form.style.animation = '';
            form.querySelectorAll('section').forEach(function (s) {
                s.style.animation = 'none';
                void s.offsetHeight;
                s.style.animation = '';
            });
        }
    }
});
