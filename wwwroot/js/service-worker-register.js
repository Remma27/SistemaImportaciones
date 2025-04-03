// En un archivo /wwwroot/js/service-worker-register.js
if ('serviceWorker' in navigator) {
    window.addEventListener('load', () => {
        navigator.serviceWorker
            .register('/service-worker.js') // ¡RUTA CORREGIDA!
            .then(registration => {
                console.log('Service Worker registrado con éxito:', registration);
            })
            .catch(error => {
                console.log('Error al registrar Service Worker:', error);
            });
    });
}
