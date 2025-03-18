$(document).ready(function () {
    $('#btnToggleEscotillas').on('click', function () {
        const cardEscotillas = $('#cardEscotillas')

        const icon = $(this).find('i')

        cardEscotillas.slideToggle(300, function () {
            const isVisible = cardEscotillas.is(':visible')
            icon.toggleClass('fa-eye fa-eye-slash')
            $('#btnToggleEscotillas')
                .attr('title', isVisible ? 'Ocultar escotillas' : 'Mostrar escotillas')
                .toggleClass('btn-secondary btn-outline-secondary')
        })
    })

    $('#exportToExcel').on('click', function () {
        exportTableToExcel('tablaInforme', 'Informe_General_' + new Date().toISOString().slice(0, 10))
    })

    function exportTableToExcel(tableId, filename = '') {
        const table = document.querySelector('table')
        if (!table) {
            return
        }

        if (typeof XLSX === 'undefined') {
            const script = document.createElement('script')
            script.src = 'https://cdn.sheetjs.com/xlsx-0.20.0/package/dist/xlsx.full.min.js'
            script.onload = function () {
                exportTableToExcelWithXLSX(table, filename)
            }
            document.head.appendChild(script)
            return
        }

        exportTableToExcelWithXLSX(table, filename)
    }

    function exportTableToExcelWithXLSX(table, filename = '') {
        if (!table) return

        if (!filename) {
            const now = new Date()
            const dateStr = now.toISOString().split('T')[0]
            filename = `Informe_General_${dateStr}`
        }

        const wb = XLSX.utils.book_new()
        const ws_data = []
        
        // Verificar si las columnas de conversión están visibles
        const showingMetric = !document.querySelector('.unit-toggle-columns.visible')
        
        // Construir encabezados dinámicamente en base a columnas visibles
        const headerCells = table.querySelectorAll('thead th')
        const headerRow = []
        const skipColumns = []
        const columnTypes = [] // Para determinar el formato de cada columna
        
        headerCells.forEach((cell, index) => {
            // Si la columna es una columna de toggle de unidades y están ocultas, la saltamos
            if (cell.classList.contains('unit-toggle-columns') && !cell.classList.contains('visible')) {
                skipColumns.push(index)
                return
            }
            
            const headerText = cell.innerText.trim()
            headerRow.push(headerText)
            
            // Determinar el tipo de columna para aplicar formato correcto después
            if (headerText.includes('%')) {
                columnTypes.push('percentage')
            } else if (headerText.includes('Req.') || headerText.includes('Desc.') || 
                      headerText.includes('Falt.') || headerText.includes('Kg') || headerText.includes('Ton')) {
                columnTypes.push('weight')
            } else if (headerText.includes('Cam. Falt.')) {
                columnTypes.push('camion')
            } else if (headerText.includes('Placas')) {
                columnTypes.push('placas')
            } else {
                columnTypes.push('text')
            }
        })
        
        ws_data.push(headerRow)
        
        // Inicializar array para anchos de columna
        const columnWidths = headerRow.map(header => Math.max(10, header.length * 1.2))

        // Procesar filas teniendo en cuenta columnas ocultas
        const rows = table.querySelectorAll('tbody tr')
        for (let i = 0; i < rows.length; i++) {
            const row_data = []
            const cells = rows[i].querySelectorAll('td')
            let visibleColIdx = 0

            for (let j = 0; j < cells.length; j++) {
                // Saltarse columnas ocultas
                if (skipColumns.includes(j)) continue;
                
                const cellText = cells[j].innerText.trim()
                row_data.push(cellText)
                
                // Actualizar ancho de columna
                columnWidths[visibleColIdx] = Math.max(
                    columnWidths[visibleColIdx], 
                    cellText.length * 1.1
                )
                visibleColIdx++
            }

            ws_data.push(row_data)
        }

        // Procesar pie de tabla si existe
        const footerCells = table.querySelectorAll('tfoot tr:first-child td')
        if (footerCells.length > 0) {
            const footerRow = []
            let visibleColIdx = 0
            
            for (let j = 0; j < footerCells.length; j++) {
                // Saltarse columnas ocultas
                if (skipColumns.includes(j)) continue;
                
                const cellText = footerCells[j].innerText.trim()
                footerRow.push(cellText)
                
                // Actualizar ancho de columna
                columnWidths[visibleColIdx] = Math.max(
                    columnWidths[visibleColIdx], 
                    cellText.length * 1.1
                )
                visibleColIdx++
            }
            
            ws_data.push(footerRow)
        }

        const ws = XLSX.utils.aoa_to_sheet(ws_data)
        
        // Mejorar la función para extraer números
        function extractNumber(text) {
            if (!text || text === '-') return null;
            
            // Limpiar el texto para obtener solo números
            let numStr = text.replace(/[^\d.,\-]/g, '');
            
            // Si está vacío después de limpiar, no es un número
            if (numStr === '') return null;
            
            // Caso especial para números europeos (1.234,56)
            if (numStr.includes('.') && numStr.includes(',')) {
                return parseFloat(numStr.replace(/\./g, '').replace(',', '.'));
            } else if (numStr.includes(',')) {
                return parseFloat(numStr.replace(',', '.'));
            } else {
                return parseFloat(numStr);
            }
        }
        
        // Formatear números y aplicar estilos
        for (let i = 1; i < ws_data.length; i++) {
            for (let j = 0; j < ws_data[i].length; j++) {
                const cellAddress = XLSX.utils.encode_cell({ r: i, c: j })
                const cellValue = ws_data[i][j]
                
                // Omitir celdas vacías
                if (!cellValue || cellValue === '-') continue;
                
                // Determinar si es un número y extraerlo
                const numValue = extractNumber(cellValue);
                
                if (numValue !== null) {
                    ws[cellAddress] = {
                        v: numValue,
                        t: 'n'
                    };
                    
                    // Aplicar formato según el tipo de columna
                    const colType = columnTypes[j];
                    
                    if (colType === 'percentage') {
                        ws[cellAddress].z = '0.00%';
                        // Convertir a decimal si es porcentaje
                        if (numValue > 1 && cellValue.includes('%')) {
                            ws[cellAddress].v = numValue / 100;
                        }
                    } else if (colType === 'weight') {
                        ws[cellAddress].z = '#,##0.00';
                    } else if (colType === 'camion') {
                        // Formato para mostrar 2 decimales en camiones faltantes
                        ws[cellAddress].z = '#,##0.00';
                    } else if (colType === 'placas') {
                        // Para placas, enteros sin decimales
                        ws[cellAddress].z = '0';
                    } else {
                        // Valor por defecto para otros tipos numéricos
                        ws[cellAddress].z = '#,##0.00';
                    }
                    
                    // Agregar alineación
                    if (!ws[cellAddress].s) ws[cellAddress].s = {};
                    ws[cellAddress].s.alignment = { horizontal: j === 0 ? 'left' : 'right' };
                }
            }
        }

        // Aplicar estilos a encabezados
        for (let j = 0; j < headerRow.length; j++) {
            const cellAddress = XLSX.utils.encode_cell({ r: 0, c: j })
            if (!ws[cellAddress].s) ws[cellAddress].s = {};
            ws[cellAddress].s.font = { bold: true };
            ws[cellAddress].s.alignment = { horizontal: 'center' };
        }

        // Aplicar anchos de columna optimizados
        ws['!cols'] = columnWidths.map((width, index) => {
            // Ajustar según el tipo de columna
            if (index === 0) {
                // Columna de empresa
                return { wch: Math.max(25, width) };
            } else if (columnTypes[index] === 'percentage') {
                return { wch: Math.min(12, width) };
            } else if (columnTypes[index] === 'weight') {
                return { wch: Math.min(15, width) };
            } else if (columnTypes[index] === 'placas') {
                return { wch: Math.min(10, width) };
            } else {
                return { wch: Math.min(Math.max(8, width), 20) };
            }
        });

        XLSX.utils.book_append_sheet(wb, ws, 'Informe General')
        XLSX.writeFile(wb, filename + '.xlsx')
    }

    function adjustTableLayout() {
        $('.table-responsive').css('display', 'none').height()
        $('.table-responsive').css('display', '')
    }

    let showingMetric = true
    $('#toggleUnits').on('click', function () {
        showingMetric = !showingMetric

        const buttonText = document.getElementById('unitsButtonText')
        buttonText.textContent = showingMetric ? 'Libras' : 'Kilogramos'

        const toggleColumns = document.querySelectorAll('.unit-toggle-columns')

        toggleColumns.forEach(col => {
            if (!showingMetric) {
                col.style.display = 'table-cell'
            }
        })

        setTimeout(() => {
            toggleColumns.forEach(col => {
                if (showingMetric) {
                    $(col).removeClass('visible')
                } else {
                    $(col).addClass('visible')
                }
            })

            if (showingMetric) {
                setTimeout(() => {
                    toggleColumns.forEach(col => {
                        col.style.display = 'none'
                    })
                }, 300)
            }
        }, 50)
    })

    $('form').on('submit', function () {
        const select = this.querySelector('select')
        const currentText = select.options[select.selectedIndex].text
        select.disabled = true
        select.parentElement.insertAdjacentHTML(
            'beforeend',
            '<div class="position-absolute end-0 me-2 spinner-border spinner-border-sm text-primary"></div>',
        )
    })
})
