// Nombre de la caché
const CACHE_NAME = 'sgi-cache-v1';

// Recursos que queremos cachear para uso offline
const CACHE_ASSETS = [
  '/',
  '/css/site.css',
  '/css/toast.css',
  '/lib/bootstrap/dist/css/bootstrap.min.css',
  '/lib/fontawesome/css/all.min.css',
  '/lib/datatables/css/datatables.min.css',
  '/lib/jquery/dist/jquery.min.js',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
  '/lib/datatables/js/datatables.min.js',
  '/lib/datatables/js/dataTables.responsive.min.js',
  '/js/site.js',
  '/js/toast-service.js',
  '/js/diagnostico-offline.js',
  '/lib/jquery-validation/dist/jquery.validate.min.js',
  '/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.min.js',
  '/lib/fontawesome/webfonts/fa-solid-900.woff2',
  '/lib/fontawesome/webfonts/fa-regular-400.woff2',
  '/offline.html',
  '/css/loading.css',
  '/lib/xlsx/xlsx.full.min.js',
];

// Instalar el Service Worker
self.addEventListener('install', event => {
  
  // Esperar hasta que se completen todas las promesas
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => {
        return cache.addAll(CACHE_ASSETS);
      })
      .then(() => self.skipWaiting())
  );
});

// Activar el Service Worker
self.addEventListener('activate', event => {
  
  // Eliminar cachés antiguas
  event.waitUntil(
    caches.keys().then(cacheNames => {
      return Promise.all(
        cacheNames.map(cache => {
          if (cache !== CACHE_NAME) {
            return caches.delete(cache);
          }
        })
      );
    })
  );
});

// Interceptar peticiones de red
self.addEventListener('fetch', event => {
  
  // Si es una petición de navegación
  if (event.request.mode === 'navigate') {
    event.respondWith(
      fetch(event.request)
        .catch(() => {
          // Si falla la navegación, mostrar página offline
          return caches.match('/offline.html');
        })
    );
    return;
  }
  
  event.respondWith(
    // Primero intentar obtener de la caché
    caches.match(event.request)
      .then(cachedResponse => {
        // Si está en caché, devolver de la caché
        if (cachedResponse) {
          return cachedResponse;
        }
        
        // Si no está en caché, intentar obtener de la red
        return fetch(event.request)
          .then(response => {
            // No cachear si:
            // 1. No hay respuesta
            // 2. La respuesta no es OK (código 200)
            // 3. No es una petición GET
            if (!response || response.status !== 200 || event.request.method !== 'GET') {
              return response;
            }
            
            // Clonar la respuesta ya que la vamos a consumir dos veces
            const responseToCache = response.clone();
            
            // Abrir la caché y almacenar la respuesta
            caches.open(CACHE_NAME)
              .then(cache => {
                cache.put(event.request, responseToCache);
              });
            
            return response;
          })
          .catch(error => {
            console.error('Error fetching resource:', error);
            
            // Para recursos como imágenes, devolver una imagen de respaldo
            if (event.request.url.match(/\.(jpe?g|png|gif|svg)$/)) {
              return caches.match('/images/placeholder.png');
            }
            
            // No hay un fallback para este recurso
            throw error;
          });
      })
  );
});