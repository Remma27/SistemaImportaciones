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
        
        const originalTable = document.getElementById('tablaDetallada');
        const clonedTable = originalTable.cloneNode(true);
        
        $(clonedTable).find('th.columna-conversion, td.columna-conversion').each(function() {
             if (!$(this).hasClass('visible-conversion')) {
                 $(this).remove();
             }
        });
        
        const columnTypes = [];
        const headerRow = clonedTable.querySelector('thead tr');
        let quintalesCols = [];
        let librasCols = [];
        
        if (headerRow) {
            const headerCells = headerRow.querySelectorAll('th');
            headerCells.forEach((cell, index) => {
                const headerText = cell.innerText.trim();
                if (headerText.includes('%')) {
                    columnTypes.push('percentage');
                } else if (
                    headerText.includes('Peso') || headerText.includes('Kg') ||
                    headerText.includes('Ton') || 
                    headerText.toLowerCase().includes('cantidad por retirar') ||
                    headerText.toLowerCase().includes('cantidad retirada')
                ) {
                    columnTypes.push('numeric');
                } else if (
                    headerText.includes('Quintales')
                ) {
                    columnTypes.push('quintales');
                    quintalesCols.push(index);
                } else if (
                    headerText.includes('Libras')
                ) {
                    columnTypes.push('libras');
                    librasCols.push(index);
                } else {
                    columnTypes.push('text');
                }
            });
        }
        
        const wb = XLSX.utils.book_new();
        const ws = XLSX.utils.table_to_sheet(clonedTable, { raw: true });
        
        const range = XLSX.utils.decode_range(ws['!ref']);
        for (let R = range.s.r + 1; R <= range.e.r; ++R) {
            for (let C = range.s.c; C <= range.e.c; ++C) {
                const cellAddress = XLSX.utils.encode_cell({ r: R, c: C });
                const cell = ws[cellAddress];
                if (cell && typeof cell.v === 'string') {
                    const cellText = cell.v.trim();
                    const colType = columnTypes[C - range.s.c];
                    
                    if (colType === 'quintales' || colType === 'libras') {
                        let num = extraerNumeroRobusto(cellText);
                        if (!isNaN(num)) {
                            cell.v = num;
                            cell.t = 'n';
                            cell.z = '#,##0.00'; 
                        }
                    } else if (colType === 'numeric') {
                        let num = extraerNumeroRobusto(cellText);
                        if (!isNaN(num)) {
                            cell.v = num;
                            cell.t = 'n';
                            cell.z = '#,##0.00';
                        }
                    } else if (colType === 'percentage') {
                        let num = extraerNumeroRobusto(cellText.replace('%', ''));
                        if (!isNaN(num)) {
                            cell.v = num / 100;
                            cell.t = 'n';
                            cell.z = '0.00%';
                        }
                    }
                }
            }
        }
        
        const colWidths = [];
        for (let C = range.s.c; C <= range.e.c; ++C) {
            let maxWidth = 10;
            for (let R = range.s.r; R <= range.e.r; ++R) {
                const cellAddress = XLSX.utils.encode_cell({ r: R, c: C });
                if (!ws[cellAddress]) continue;
                const cellText = String(ws[cellAddress].v || '');
                maxWidth = Math.max(maxWidth, cellText.length * 1.2);
            }
            colWidths[C] = { wch: Math.min(maxWidth, 50) };
        }
        ws['!cols'] = colWidths;
        
        XLSX.utils.book_append_sheet(wb, ws, 'Reporte Detallado');
        
        const ahora = new Date();
        const fecha = ahora.toISOString().split('T')[0];
        const nombreArchivo = `Reporte_Detallado_${fecha}.xlsx`;
        
        XLSX.writeFile(wb, nombreArchivo);
    });
    
    function extraerNumeroRobusto(texto) {
        if (!texto || texto === '-' || texto === '') return NaN;
        
        let cleaned = texto.toString().trim().replace(/[$€\s]/g, '');
        
        const formatoEuropeo = /^\d{1,3}(?:\.\d{3})+(?:,\d+)?$/.test(cleaned) || 
                              (cleaned.indexOf(',') > -1 && cleaned.indexOf('.') > -1 && cleaned.lastIndexOf(',') > cleaned.lastIndexOf('.'));
        
        const formatoAmericano = /^\d{1,3}(?:,\d{3})+(?:\.\d+)?$/.test(cleaned) || 
                               (cleaned.indexOf(',') > -1 && cleaned.indexOf('.') > -1 && cleaned.lastIndexOf('.') > cleaned.lastIndexOf(','));
        
        if (formatoEuropeo) {
            cleaned = cleaned.replace(/\./g, '').replace(',', '.');
        } else if (formatoAmericano) {
            cleaned = cleaned.replace(/,/g, '');
        } else if (cleaned.indexOf(',') > -1 && cleaned.indexOf('.') === -1) {
            cleaned = cleaned.replace(',', '.');
        }

        return parseFloat(cleaned);
    }
});
