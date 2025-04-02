// Añadir al final del archivo site.js

// Función para configurar tablas de respaldo cuando DataTables falla
function setupFallbackTables() {
    $('table').each(function () {
        if (!$(this).hasClass('dataTable')) {
            $(this).addClass('table table-striped table-bordered')
        }
    })
}

// Ejecuta esto si DataTables falla
$(document).ready(function () {
    if (typeof $.fn.DataTable === 'undefined') {
        console.log('DataTables no disponible, usando fallback')
        setupFallbackTables()
    }
})
