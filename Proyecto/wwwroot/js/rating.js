// #region Rating
document.addEventListener('DOMContentLoaded', function () {
    const stars = document.querySelectorAll('.rating-stars i');
    const inputEstrellas = document.getElementById('inputEstrellas');

    if (stars.length > 0 && inputEstrellas) {
        stars.forEach(star => {
            star.addEventListener('click', () => {
                const val = star.getAttribute('data-value');
                inputEstrellas.value = val;

                stars.forEach(s => {
                    s.classList.remove('active');
                    if (parseInt(s.getAttribute('data-value')) <= parseInt(val)) {
                        s.classList.add('active');
                    }
                });
// #endregion
            });

            star.addEventListener('mouseover', () => {
                const val = star.getAttribute('data-value');
                stars.forEach(s => {
                    if (parseInt(s.getAttribute('data-value')) <= parseInt(val)) {
                        s.style.color = "#ffc107";
                    }
                });
            });

            star.addEventListener('mouseout', () => {
                stars.forEach(s => {
                    s.style.color = "";
                });
            });
        });
    }
});