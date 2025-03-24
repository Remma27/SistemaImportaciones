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
        
        const showingMetric = !document.querySelector('.unit-toggle-columns.visible')
        
        const headerCells = table.querySelectorAll('thead th')
        const headerRow = []
        const skipColumns = []
        const columnTypes = [] 
        
        headerCells.forEach((cell, index) => {
            if (cell.classList.contains('unit-toggle-columns') && !cell.classList.contains('visible')) {
                skipColumns.push(index)
                return
            }
            
            const headerText = cell.innerText.trim()
            headerRow.push(headerText)
            
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
        
        const columnWidths = headerRow.map(header => Math.max(10, header.length * 1.2))

        const rows = table.querySelectorAll('tbody tr')
        for (let i = 0; i < rows.length; i++) {
            const row_data = []
            const cells = rows[i].querySelectorAll('td')
            let visibleColIdx = 0

            for (let j = 0; j < cells.length; j++) {
                if (skipColumns.includes(j)) continue;
                
                const cellText = cells[j].innerText.trim()
                row_data.push(cellText)
                
                columnWidths[visibleColIdx] = Math.max(
                    columnWidths[visibleColIdx], 
                    cellText.length * 1.1
                )
                visibleColIdx++
            }

            ws_data.push(row_data)
        }

        const footerCells = table.querySelectorAll('tfoot tr:first-child td')
        if (footerCells.length > 0) {
            const footerRow = []
            let visibleColIdx = 0
            
            for (let j = 0; j < footerCells.length; j++) {
                if (skipColumns.includes(j)) continue;
                
                const cellText = footerCells[j].innerText.trim()
                footerRow.push(cellText)
                
                columnWidths[visibleColIdx] = Math.max(
                    columnWidths[visibleColIdx], 
                    cellText.length * 1.1
                )
                visibleColIdx++
            }
            
            ws_data.push(footerRow)
        }

        const ws = XLSX.utils.aoa_to_sheet(ws_data)
        
        function extractNumber(text) {
            if (!text || text === '-') return null;
            
            let numStr = text.replace(/[^\d.,\-]/g, '');
            
            if (numStr === '') return null;
            
            if (numStr.includes('.') && numStr.includes(',')) {
                return parseFloat(numStr.replace(/\./g, '').replace(',', '.'));
            } else if (numStr.includes(',')) {
                return parseFloat(numStr.replace(',', '.'));
            } else {
                return parseFloat(numStr);
            }
        }
        
        for (let i = 1; i < ws_data.length; i++) {
            for (let j = 0; j < ws_data[i].length; j++) {
                const cellAddress = XLSX.utils.encode_cell({ r: i, c: j })
                const cellValue = ws_data[i][j]
                
                if (!cellValue || cellValue === '-') continue;
                
                const numValue = extractNumber(cellValue);
                
                if (numValue !== null) {
                    ws[cellAddress] = {
                        v: numValue,
                        t: 'n'
                    };
                    
                    const colType = columnTypes[j];
                    
                    if (colType === 'percentage') {
                        ws[cellAddress].z = '0.00%';
                        if (numValue > 1 && cellValue.includes('%')) {
                            ws[cellAddress].v = numValue / 100;
                        }
                    } else if (colType === 'weight') {
                        ws[cellAddress].z = '#,##0.00';
                    } else if (colType === 'camion') {
                        ws[cellAddress].z = '#,##0.00';
                    } else if (colType === 'placas') {
                        ws[cellAddress].z = '0';
                    } else {
                        ws[cellAddress].z = '#,##0.00';
                    }
                    
                    if (!ws[cellAddress].s) ws[cellAddress].s = {};
                    ws[cellAddress].s.alignment = { horizontal: j === 0 ? 'left' : 'right' };
                }
            }
        }

        for (let j = 0; j < headerRow.length; j++) {
            const cellAddress = XLSX.utils.encode_cell({ r: 0, c: j })
            if (!ws[cellAddress].s) ws[cellAddress].s = {};
            ws[cellAddress].s.font = { bold: true };
            ws[cellAddress].s.alignment = { horizontal: 'center' };
        }

        ws['!cols'] = columnWidths.map((width, index) => {
            if (index === 0) {
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

        localStorage.setItem('showingMetric', showingMetric);

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

    $(window).on('unload beforeunload', function() {
        localStorage.setItem('showingMetric', 'true');
    });

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
