/**
 * Diagnóstico de recursos offline
 * Este script permite verificar qué recursos están disponibles y cuáles fallan
 * Presiona Alt+D para iniciar el diagnóstico
 */

(function() {
    'use strict';

    // Recursos a verificar - mantener actualizado según las dependencias del proyecto
    const RECURSOS = [
        { tipo: 'CSS', ruta: '/lib/bootstrap/dist/css/bootstrap.min.css', nombre: 'Bootstrap CSS' },
        { tipo: 'CSS', ruta: '/lib/fontawesome/css/all.min.css', nombre: 'FontAwesome CSS' },
        { tipo: 'CSS', ruta: '/lib/datatables/css/datatables.min.css', nombre: 'DataTables CSS' },
        { tipo: 'CSS', ruta: '/css/site.css', nombre: 'Site CSS' },
        { tipo: 'CSS', ruta: '/css/toast.css', nombre: 'Toast CSS' },
        
        { tipo: 'JS', ruta: '/lib/jquery/dist/jquery.min.js', nombre: 'jQuery' },
        { tipo: 'JS', ruta: '/lib/bootstrap/dist/js/bootstrap.bundle.min.js', nombre: 'Bootstrap JS' },
        { tipo: 'JS', ruta: '/lib/datatables/js/datatables.min.js', nombre: 'DataTables JS' },
        { tipo: 'JS', ruta: '/lib/datatables/js/dataTables.responsive.min.js', nombre: 'DataTables Responsive' },
        { tipo: 'JS', ruta: '/js/site.js', nombre: 'Site JS' },
        { tipo: 'JS', ruta: '/js/toast-service.js', nombre: 'Toast Service' },
        { tipo: 'JS', ruta: '/js/diagnostico-offline.js', nombre: 'Diagnóstico Offline' },
        { tipo: 'JS', ruta: '/js/service-worker-register.js', nombre: 'Service Worker Register' },
        { tipo: 'JS', ruta: '/service-worker.js', nombre: 'Service Worker' },
        
        { tipo: 'FONT', ruta: '/lib/fontawesome/webfonts/fa-solid-900.woff2', nombre: 'FontAwesome Solid' },
        { tipo: 'FONT', ruta: '/lib/fontawesome/webfonts/fa-regular-400.woff2', nombre: 'FontAwesome Regular' }
    ];

    // Verificar si un recurso está disponible
    async function verificarRecurso(recurso) {
        try {
            const response = await fetch(recurso.ruta, { method: 'HEAD' });
            return {
                ...recurso,
                status: response.status,
                ok: response.ok
            };
        } catch (error) {
            return {
                ...recurso,
                status: 0,
                ok: false,
                error: error.message
            };
        }
    }

    // Generar reporte HTML
    function generarReporte(resultados) {
        const okCount = resultados.filter(r => r.ok).length;
        const totalCount = resultados.length;
        const porcentaje = Math.round((okCount / totalCount) * 100);
        
        let html = `
            <div class="diagnostico-modal">
                <div class="diagnostico-header">
                    <h2>Diagnóstico de Recursos Offline</h2>
                    <div class="progress mb-3">
                        <div class="progress-bar ${porcentaje < 70 ? 'bg-danger' : porcentaje < 90 ? 'bg-warning' : 'bg-success'}" 
                             style="width: ${porcentaje}%;" 
                             aria-valuenow="${porcentaje}" aria-valuemin="0" aria-valuemax="100">
                            ${porcentaje}%
                        </div>
                    </div>
                    <p>${okCount} de ${totalCount} recursos disponibles</p>
                    <button id="cerrar-diagnostico" class="btn btn-sm btn-secondary float-end">Cerrar</button>
                    <button id="descargar-reporte" class="btn btn-sm btn-primary float-end me-2">Descargar Reporte</button>
                </div>
                <div class="diagnostico-body">
                    <table class="table table-sm table-striped">
                        <thead>
                            <tr>
                                <th>Estado</th>
                                <th>Tipo</th>
                                <th>Nombre</th>
                                <th>Ruta</th>
                            </tr>
                        </thead>
                        <tbody>
        `;
        
        resultados.forEach(r => {
            html += `
                <tr class="${r.ok ? 'table-success' : 'table-danger'}">
                    <td>
                        <span class="${r.ok ? 'text-success' : 'text-danger'}">
                            <i class="fas ${r.ok ? 'fa-check-circle' : 'fa-times-circle'}"></i>
                            ${r.ok ? 'OK' : 'ERROR ' + r.status}
                        </span>
                    </td>
                    <td>${r.tipo}</td>
                    <td>${r.nombre}</td>
                    <td class="text-muted"><small>${r.ruta}</small></td>
                </tr>
            `;
        });
        
        html += `
                        </tbody>
                    </table>
                </div>
                <div class="diagnostico-footer">
                    <p class="small text-muted">
                        Diagnóstico generado el ${new Date().toLocaleString()}
                    </p>
                </div>
            </div>
        `;
        
        return html;
    }

    // Crear modal para mostrar el reporte
    function mostrarModal(html) {
        // Estilos para el modal
        const style = document.createElement('style');
        style.textContent = `
            .diagnostico-overlay {
                position: fixed;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background-color: rgba(0, 0, 0, 0.5);
                z-index: 9999;
                display: flex;
                align-items: center;
                justify-content: center;
            }
            .diagnostico-modal {
                background-color: white;
                border-radius: 8px;
                width: 90%;
                max-width: 800px;
                max-height: 90vh;
                overflow: hidden;
                display: flex;
                flex-direction: column;
            }
            .diagnostico-header {
                padding: 15px;
                border-bottom: 1px solid #e9ecef;
            }
            .diagnostico-body {
                padding: 15px;
                overflow-y: auto;
                flex-grow: 1;
            }
            .diagnostico-footer {
                padding: 15px;
                border-top: 1px solid #e9ecef;
                text-align: center;
            }
        `;
        document.head.appendChild(style);
        
        // Crear overlay y añadir el HTML
        const overlay = document.createElement('div');
        overlay.className = 'diagnostico-overlay';
        overlay.innerHTML = html;
        document.body.appendChild(overlay);
        
        // Manejar el cierre del modal
        document.getElementById('cerrar-diagnostico').addEventListener('click', function() {
            document.body.removeChild(overlay);
            document.head.removeChild(style);
        });
        
        // Manejar la descarga del reporte
        document.getElementById('descargar-reporte').addEventListener('click', function() {
            const fecha = new Date().toISOString().split('T')[0];
            const nombreArchivo = `diagnostico-recursos-${fecha}.html`;
            const contenido = `
                <!DOCTYPE html>
                <html lang="es">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>Diagnóstico de Recursos - ${fecha}</title>
                    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.3/dist/css/bootstrap.min.css" rel="stylesheet">
                    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css">
                    <style>
                        body { padding: 20px; }
                        .header { margin-bottom: 30px; }
                    </style>
                </head>
                <body>
                    <div class="container">
                        <div class="header">
                            <h1>Diagnóstico de Recursos Offline</h1>
                            <p class="lead">Reporte generado el ${new Date().toLocaleString()}</p>
                        </div>
                        ${overlay.querySelector('.diagnostico-body').outerHTML}
                    </div>
                </body>
                </html>
            `;
            
            const blob = new Blob([contenido], { type: 'text/html' });
            const url = URL.createObjectURL(blob);
            
            const a = document.createElement('a');
            a.href = url;
            a.download = nombreArchivo;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(url);
        });
    }

    // Función principal de diagnóstico
    async function iniciarDiagnostico() {
        console.log('Iniciando diagnóstico de recursos...');
        
        // Verificar todos los recursos
        const promesas = RECURSOS.map(verificarRecurso);
        const resultados = await Promise.all(promesas);
        
        // Generar y mostrar el reporte
        const html = generarReporte(resultados);
        mostrarModal(html);
        
        console.log('Diagnóstico completado.');
    }

    // Escuchar combinación de teclas Alt+D para iniciar el diagnóstico
    document.addEventListener('keydown', function(e) {
        if (e.altKey && e.key === 'd') {
            e.preventDefault();
            iniciarDiagnostico();
        }
    });

    // También podemos exponer la función para poder llamarla desde otro lugar
    window.iniciarDiagnostico = iniciarDiagnostico;
    
    console.log('Diagnóstico offline listo. Presiona Alt+D para iniciar.');
})();