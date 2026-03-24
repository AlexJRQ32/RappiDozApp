(function() {
    const loader = document.getElementById('page-loader');

    if (sessionStorage.getItem('showLoader') === 'true') {
        loader.style.transition = 'none';
        loader.classList.add('loader-active');
        sessionStorage.removeItem('showLoader');

        setTimeout(() => {
            loader.style.transition = 'transform 0.6s cubic-bezier(0.8, 0, 0.2, 1)';
            loader.classList.remove('loader-active');
        }, 500);
    }

    document.addEventListener("click", function(e) {
      const link = e.target.closest('a');
      if (!link) return;

      const href = link.getAttribute('href');
      const target = link.getAttribute('target');

      if (!href || href.startsWith('#') || href.includes("javascript") || target === "_blank") {
        return; 
      }

      sessionStorage.setItem('showLoader', 'true');

      loader.classList.add('loader-active');
    });

    window.addEventListener("load", function() {
        if (!sessionStorage.getItem('showLoader')) {
            loader.classList.remove('loader-active');
        }
    });
})();
