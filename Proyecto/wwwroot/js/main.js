function loadComponent(id, path) {
    fetch(path)
        .then(response => {
            if (!response.ok) throw new Error("No se pudo cargar el componente");
            return response.text();
        })
        .then(data => {
            document.getElementById(id).innerHTML = data;
        })
        .catch(err => console.error(err));
}
