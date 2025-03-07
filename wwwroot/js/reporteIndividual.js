$(document).ready(function () {
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

    $('#btnExportarExcel').on('click', function () {
        const tabla = document.getElementById('tablaDetallada')
        const wb = XLSX.utils.book_new()

        const data = []
        const headers = []

        const headerRow = tabla.querySelector('thead tr')
        if (headerRow) {
            const headerCells = headerRow.querySelectorAll('th')
            headerCells.forEach(cell => {
                headers.push(cell.innerText.trim())
            })
            data.push(headers)
        }

        const rows = tabla.querySelectorAll('tbody tr')
        rows.forEach(row => {
            const rowData = []
            const cells = row.querySelectorAll('td')
            cells.forEach((cell, colIndex) => {
                let cellValue = cell.innerText.trim()

                if ([7, 8, 9, 10, 11, 12, 13].includes(colIndex)) {
                    if (cellValue.includes(',') && cellValue.indexOf(',') > cellValue.lastIndexOf('.')) {
                        cellValue = parseFloat(cellValue.replace(/\./g, '').replace(',', '.'))
                    } else if (cellValue.includes('.') || cellValue.includes(',')) {
                        cellValue = parseFloat(cellValue.replace(/,/g, ''))
                    } else {
                        cellValue = parseInt(cellValue, 10)
                    }
                }

                rowData.push(cellValue)
            })
            data.push(rowData)
        })

        const ws = XLSX.utils.aoa_to_sheet(data)

        const numericColumns = [7, 8, 9, 10, 11, 12, 13]
        for (let R = 1; R < data.length; R++) {
            for (let C of numericColumns) {
                if (C < data[R].length) {
                    const cellAddress = XLSX.utils.encode_cell({ r: R, c: C })
                    if (ws[cellAddress]) {
                        ws[cellAddress].t = 'n'
                    }
                }
            }
        }

        const colWidths = headers.map(header => ({ wch: Math.max(10, header.length * 1.2) }))
        ws['!cols'] = colWidths

        XLSX.utils.book_append_sheet(wb, ws, 'Reporte Detallado')

        const ahora = new Date()
        const fecha = ahora.toISOString().split('T')[0]
        const nombreArchivo = `Reporte_Detallado_${fecha}.xlsx`

        XLSX.writeFile(wb, nombreArchivo)
    })
})
