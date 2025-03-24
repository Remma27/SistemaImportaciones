$(document).ready(function () {
    $('#btnExportarExcel').prop('disabled', false).removeClass('disabled');
    
    const checkInterval = setInterval(function() {
        if ($('#btnExportarExcel').is(':disabled')) {
            console.log('Rehabilitando el botón de exportar');
            $('#btnExportarExcel').prop('disabled', false).removeClass('disabled');
        }
    }, 1000);
    
    setTimeout(function() {
        clearInterval(checkInterval);
    }, 10000);
    
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

    $(window).on('beforeunload', function() {
        localStorage.setItem('showingMetric', 'true');
    });
    
    $('#btnToggleConversiones').on('click', function () {
        $('.columna-conversion').toggleClass('visible-conversion');
        
        const isVisible = $('.columna-conversion').hasClass('visible-conversion');
        
        $(this).html(isVisible ? 
            '<i class="fas fa-calculator me-1"></i>Ocultar Conversiones' : 
            '<i class="fas fa-calculator me-1"></i>Mostrar Conversiones');
        
        $(this).attr('title', isVisible ? 'Ocultar Conversiones' : 'Mostrar Conversiones');
        
        if (isVisible) {
            $(this).removeClass('btn-info').addClass('btn-warning');
        } else {
            $(this).removeClass('btn-warning').addClass('btn-info');
        }
        
        console.log('Columnas de conversión:', isVisible ? 'mostradas' : 'ocultadas');
    });

    const totalFilas = $('#tablaDetallada tbody tr').length

    $('#btnExportarExcel').on('click', function () {
        $(this).prop('disabled', false).removeClass('disabled');
        
        const tabla = document.getElementById('tablaDetallada')
        const wb = XLSX.utils.book_new()

        const data = []
        const headers = []
        const columnTypes = [] 

        const conversionesVisibles = $('.columna-conversion').hasClass('visible-conversion')

        const headerRow = tabla.querySelector('thead tr')
        if (headerRow) {
            const headerCells = headerRow.querySelectorAll('th')
            headerCells.forEach(cell => {
                if (!cell.classList.contains('columna-conversion') || conversionesVisibles) {
                    const headerText = cell.innerText.trim()
                    headers.push(headerText)
                    
                    if (headerText.includes('%')) {
                        columnTypes.push('percentage')
                    } else if (headerText.includes('Peso') || headerText.includes('Kg') || 
                               headerText.includes('Ton') || headerText.includes('Libras') || 
                               headerText.includes('Quintales')) {
                        columnTypes.push('numeric')
                    } else {
                        columnTypes.push('text')
                    }
                }
            })
            data.push(headers)
        }

        const columnWidths = headers.map(header => Math.max(8, header.length * 1.2))

        const rows = tabla.querySelectorAll('tbody tr')
        rows.forEach(row => {
            const rowData = []
            const cells = row.querySelectorAll('td')
            let visColIndex = 0; 
            
            cells.forEach((cell, colIndex) => {
                const isConversionCell = cell.classList.contains('columna-conversion')
                if (!isConversionCell || conversionesVisibles) {
                    let cellValue = cell.innerText.trim()
                    
                    if (cellValue === '') {
                        rowData.push('')
                        visColIndex++
                        return
                    }

                    if ([7, 8, 9, 10, 11, 12, 13].includes(colIndex)) {
                        if (cellValue.includes(',') && cellValue.indexOf(',') > cellValue.lastIndexOf('.')) {
                            cellValue = parseFloat(cellValue.replace(/\./g, '').replace(',', '.'))
                        } else if (cellValue.includes('.') || cellValue.includes(',')) {
                            cellValue = parseFloat(cellValue.replace(/,/g, ''))
                        } else if (/^-?\d+$/.test(cellValue)) {
                            cellValue = parseInt(cellValue, 10)
                        }
                        if (isNaN(cellValue)) {
                            cellValue = cell.innerText.trim()
                        }
                    }
                    if (typeof cellValue === 'string') {
                        columnWidths[visColIndex] = Math.max(
                            columnWidths[visColIndex] || 0, 
                            cellValue.length * 1.2
                        );
                    } else if (typeof cellValue === 'number') {
                        const numStr = cellValue.toLocaleString('es-GT');
                        columnWidths[visColIndex] = Math.max(
                            columnWidths[visColIndex] || 0, 
                            numStr.length + 2
                        );
                    }
                    
                    rowData.push(cellValue)
                    visColIndex++;
                }
            })
            data.push(rowData)
        })

        const ws = XLSX.utils.aoa_to_sheet(data)

        for (let R = 1; R < data.length; R++) {
            for (let C = 0; C < data[R].length; C++) {
                const cellAddress = XLSX.utils.encode_cell({ r: R, c: C })
                const cellValue = data[R][C];
                
                if (cellValue === '' || cellValue === null || isNaN(cellValue)) continue;
                
                if (typeof cellValue === 'number') {
                    ws[cellAddress].t = 'n';
                    
                    if (columnTypes[C] === 'percentage') {
                        ws[cellAddress].z = '0.00%';
                        if (cellValue > 1) {
                            ws[cellAddress].v = cellValue / 100;
                        }
                    } else if (columnTypes[C] === 'numeric') {
                        ws[cellAddress].z = '#,##0.00';
                    }
                }
            }
        }

        ws['!cols'] = columnWidths.map(width => ({
            wch: Math.min(Math.max(width, 8), 50)
        }));

        XLSX.utils.book_append_sheet(wb, ws, 'Reporte Detallado')

        const ahora = new Date()
        const fecha = ahora.toISOString().split('T')[0]
        const nombreArchivo = `Reporte_Detallado_${fecha}.xlsx`

        XLSX.writeFile(wb, nombreArchivo)
    })
})
