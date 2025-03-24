$(document).ready(function () {
    // Asegurar que el botón esté habilitado desde el inicio
    $('#btnExportarExcel').prop('disabled', false).removeClass('disabled');
    
    // Intervalos para verificar que el botón se mantenga habilitado
    const checkInterval = setInterval(function() {
        if ($('#btnExportarExcel').is(':disabled')) {
            console.log('Rehabilitando el botón de exportar');
            $('#btnExportarExcel').prop('disabled', false).removeClass('disabled');
        }
    }, 1000);
    
    // Detener el intervalo después de 10 segundos
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

    // Registrar evento para restablecer a métrico al salir de la página
    $(window).on('beforeunload', function() {
        localStorage.setItem('showingMetric', 'true');
    });
    
    // Función para manejar el botón de mostrar/ocultar conversiones
    $('#btnToggleConversiones').on('click', function () {
        // Toggle the visibility class on conversion columns
        $('.columna-conversion').toggleClass('visible-conversion');
        
        // Check if columns are now visible
        const isVisible = $('.columna-conversion').hasClass('visible-conversion');
        
        // Update button text and appearance
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
        // Asegurar que el botón esté habilitado al hacer clic
        $(this).prop('disabled', false).removeClass('disabled');
        
        const tabla = document.getElementById('tablaDetallada')
        const wb = XLSX.utils.book_new()

        const data = []
        const headers = []
        const columnTypes = [] // Para almacenar el tipo de cada columna

        // Verificar si las columnas de conversión están visibles
        const conversionesVisibles = $('.columna-conversion').hasClass('visible-conversion')

        const headerRow = tabla.querySelector('thead tr')
        if (headerRow) {
            const headerCells = headerRow.querySelectorAll('th')
            headerCells.forEach(cell => {
                // Solo incluir columnas de conversión si están visibles
                if (!cell.classList.contains('columna-conversion') || conversionesVisibles) {
                    const headerText = cell.innerText.trim()
                    headers.push(headerText)
                    
                    // Determinar tipo de columna para formateo posterior
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

        // Array para almacenar la anchura máxima de contenido por columna
        const columnWidths = headers.map(header => Math.max(8, header.length * 1.2))

        const rows = tabla.querySelectorAll('tbody tr')
        rows.forEach(row => {
            const rowData = []
            const cells = row.querySelectorAll('td')
            let visColIndex = 0; // Índice de columna visible
            
            cells.forEach((cell, colIndex) => {
                // Solo procesar celdas de columnas visibles
                const isConversionCell = cell.classList.contains('columna-conversion')
                if (!isConversionCell || conversionesVisibles) {
                    let cellValue = cell.innerText.trim()
                    
                    // No convertir celdas vacías o con solo espacios a cero
                    if (cellValue === '') {
                        rowData.push('')
                        visColIndex++
                        return
                    }

                    // Tratar números según el tipo de columna
                    if ([7, 8, 9, 10, 11, 12, 13].includes(colIndex)) {
                        if (cellValue.includes(',') && cellValue.indexOf(',') > cellValue.lastIndexOf('.')) {
                            cellValue = parseFloat(cellValue.replace(/\./g, '').replace(',', '.'))
                        } else if (cellValue.includes('.') || cellValue.includes(',')) {
                            cellValue = parseFloat(cellValue.replace(/,/g, ''))
                        } else if (/^-?\d+$/.test(cellValue)) { // Solo si es un número entero
                            cellValue = parseInt(cellValue, 10)
                        }
                        // Si no es un número válido, mantenerlo como texto
                        if (isNaN(cellValue)) {
                            cellValue = cell.innerText.trim()
                        }
                    }

                    // Actualizar el ancho máximo de la columna
                    if (typeof cellValue === 'string') {
                        columnWidths[visColIndex] = Math.max(
                            columnWidths[visColIndex] || 0, 
                            cellValue.length * 1.2
                        );
                    } else if (typeof cellValue === 'number') {
                        // Estimar el ancho para números (considerando formato)
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

        // Formatear números y aplicar estilos
        for (let R = 1; R < data.length; R++) {
            for (let C = 0; C < data[R].length; C++) {
                const cellAddress = XLSX.utils.encode_cell({ r: R, c: C })
                const cellValue = data[R][C];
                
                // Skip empty cells or non-numeric values
                if (cellValue === '' || cellValue === null || isNaN(cellValue)) continue;
                
                if (typeof cellValue === 'number') {
                    ws[cellAddress].t = 'n';
                    
                    // Aplicar formato según tipo de columna
                    if (columnTypes[C] === 'percentage') {
                        ws[cellAddress].z = '0.00%';
                        // Convertir a decimal si es un porcentaje
                        if (cellValue > 1) {
                            ws[cellAddress].v = cellValue / 100;
                        }
                    } else if (columnTypes[C] === 'numeric') {
                        // Usar formato de número con decimales para pesos
                        ws[cellAddress].z = '#,##0.00';
                    }
                }
            }
        }

        // Ajustar anchos de columna usando los valores calculados
        ws['!cols'] = columnWidths.map(width => ({
            wch: Math.min(Math.max(width, 8), 50) // Limitar entre 8 y 50 caracteres de ancho
        }));

        XLSX.utils.book_append_sheet(wb, ws, 'Reporte Detallado')

        const ahora = new Date()
        const fecha = ahora.toISOString().split('T')[0]
        const nombreArchivo = `Reporte_Detallado_${fecha}.xlsx`

        XLSX.writeFile(wb, nombreArchivo)
    })
})
