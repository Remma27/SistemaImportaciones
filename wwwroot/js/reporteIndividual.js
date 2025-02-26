$(document).ready(function () {
    console.log('ReporteIndividual.js inicializado')

    function adjustTableHeight() {
        const windowHeight = $(window).height()
        const headerHeight = $('.card-header').outerHeight(true) * 2
        const pageMargins = 80
        const availableHeight = windowHeight - headerHeight - pageMargins

        $('.table-scroll-container').css('height', availableHeight + 'px')
    }

    adjustTableHeight()

    $(window).resize(function () {
        adjustTableHeight()
    })

    const totalFilas = $('#tablaDetallada tbody tr').length
    console.log(`Tabla cargada con ${totalFilas} filas`)

    $('#btnExportarExcel').on('click', function () {
        console.log('Iniciando exportación a Excel...')

        const tabla = document.getElementById('tablaDetallada')
        const wb = XLSX.utils.book_new()

        const ws = XLSX.utils.table_to_sheet(tabla, { raw: true })

        const colWidths = []
        const range = XLSX.utils.decode_range(ws['!ref'])

        for (let C = range.s.c; C <= range.e.c; ++C) {
            let maxLength = 10

            for (let R = range.s.r; R <= range.e.r; ++R) {
                const cellAddress = XLSX.utils.encode_cell({ r: R, c: C })
                if (!ws[cellAddress]) continue

                const cellContent = String(ws[cellAddress].v || '')
                maxLength = Math.max(maxLength, cellContent.length * 1.1)
            }

            colWidths.push({ wch: maxLength })
        }

        ws['!cols'] = colWidths

        XLSX.utils.book_append_sheet(wb, ws, 'Reporte Detallado')

        const ahora = new Date()
        const fecha = ahora.toISOString().split('T')[0]
        const nombreArchivo = `Reporte_Detallado_${fecha}.xlsx`

        XLSX.writeFile(wb, nombreArchivo)
        console.log(`Archivo exportado como: ${nombreArchivo}`)
    })
})
