function cargarImportaciones() {
    $.ajax({
        url: '/api/Importaciones/GetAll',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            let importaciones = response.value || response
            let select = $('#selectImportacion')
            select.empty()
            select.append($('<option>', { value: '', text: 'Seleccione una importación' }))
            $.each(importaciones, function (index, importacion) {
                select.append($('<option>', { value: importacion.Value, text: importacion.Text }))
            })
        },
        error: function (xhr, status, error) {},
    })
}

function cargarEmpresas(importacionId) {
    $.ajax({
        url: '/api/Empresas/GetAll',
        type: 'GET',
        dataType: 'json',
        data: { importacionId: importacionId },
        success: function (response) {
            let empresas = response.value || response
            let select = $('#selectEmpresa')
            select.empty()
            select.append($('<option>', { value: '', text: 'Seleccione una empresa' }))
            $.each(empresas, function (index, empresa) {
                select.append($('<option>', { value: empresa.Value, text: empresa.Text }))
            })
            select.prop('disabled', false)
        },
        error: function (xhr, status, error) {},
    })
}

function ajustarAnchoColumnas(tableId) {
    const $table = $(`#${tableId}`)
    if (!$table.length) return

    const $headers = $table.find('thead th')
    const $rows = $table.find('tbody tr:first td')

    if (!$rows.length) return

    $headers.each((index, header) => {
        const headerWidth = $(header).width()
        const cellWidth = $rows.eq(index).width()
        const maxWidth = Math.max(headerWidth, cellWidth)

        $table.find(`tr > *:nth-child(${index + 1})`).width(maxWidth)
    })
}

function optimizeTables() {
    const tables = document.querySelectorAll('.table')
    const windowWidth = window.innerWidth

    tables.forEach(table => {
        const container = table.closest('.table-responsive-custom, .table-container')
        if (!container) return

        table.style.width = '100%'

        if (windowWidth < 576) {
            if (table.id === 'tabla1') {
                table.style.minWidth = '600px'
            } else if (table.id === 'tabla2') {
                table.style.minWidth = '400px'
            } else if (table.id === 'tabla3') {
                table.style.minWidth = '300px'
            }
        } else if (windowWidth < 992) {
            if (table.id === 'tabla1') {
                table.style.minWidth = '800px'
            } else if (table.id === 'tabla2') {
                table.style.minWidth = '600px'
            } else if (table.id === 'tabla3') {
                table.style.minWidth = '500px'
            }
        } else if (windowWidth < 1200) {
            if (table.id === 'tabla1') {
                table.style.minWidth = '1000px'
            } else if (table.id === 'tabla2' || !table.id) {
                table.style.minWidth = '800px'
            } else if (table.id === 'tabla3') {
                table.style.minWidth = '600px'
            }
        } else {
            table.style.minWidth = 'auto'
        }
    })
}

function initializeTableScroll() {
    const tableContainers = document.querySelectorAll('.table-scroll')

    tableContainers.forEach(container => {
        const table = container.querySelector('table')
        if (!table) return

        table.style.width = '100%'

        container.addEventListener('scroll', function () {
            const hasVerticalScroll = container.scrollTop > 0
            const hasHorizontalScroll = container.scrollLeft > 0

            if (hasVerticalScroll || hasHorizontalScroll) {
                container.classList.add('table-shadow')
            } else {
                container.classList.remove('table-shadow')
            }
        })
    })
}

function handleResponsiveLayout() {
    const windowWidth = window.innerWidth
    const tables = document.querySelectorAll('.table')

    tables.forEach(table => {
        const headerCells = table.querySelectorAll('thead th')
        headerCells.forEach(cell => {
            if (windowWidth < 768) {
                cell.style.minWidth = '120px'
            } else {
                cell.style.minWidth = 'auto'
            }
        })
    })
}

function initializeServerData(selectedBarco, empresaId) {
    const btnAgregar = document.getElementById('btnAgregar')
    if (btnAgregar) {
        btnAgregar.dataset.selectedBarco = selectedBarco
        btnAgregar.dataset.empresaId = empresaId
    }
    window.selectedBarcoId = selectedBarco
    window.selectedEmpresaId = empresaId

    $('#btnAgregar').on('click', function () {
        if (selectedBarco && empresaId) {
            window.location.href = `/RegistroPesajes/Create?selectedBarco=${selectedBarco}&empresaId=${empresaId}`
        }
    })
}

function setupEscotillasToggle() {
    const btnToggle = $('#btnToggleEscotillas')
    const escotillasSection = $('.escotillas-card').closest('.col-lg-12')
    let isVisible = true

    btnToggle.on('click', function () {
        if (isVisible) {
            escotillasSection.slideUp(300)
            btnToggle.html('<i class="fas fa-eye"></i>')
            btnToggle.attr('title', 'Mostrar escotillas')
            btnToggle.removeClass('btn-secondary').addClass('btn-outline-secondary')
        } else {
            escotillasSection.slideDown(300)
            btnToggle.html('<i class="fas fa-eye-slash"></i>')
            btnToggle.attr('title', 'Ocultar escotillas')
            btnToggle.removeClass('btn-outline-secondary').addClass('btn-secondary')
        }
        isVisible = !isVisible
    })
}

function createExcelDataFromTable(table) {
    const headerCells = table.querySelectorAll('thead tr:first-child th')
    const headerInfo = getHeaderStructure(headerCells)
    const data = [headerInfo.headerRow]

    const rows = table.querySelectorAll('tbody tr')
    for (let i = 0; i < rows.length; i++) {
        const row = processTableRow(rows[i], headerInfo)
        data.push(row)
    }

    const footerRows = table.querySelectorAll('tfoot tr')
    if (footerRows.length > 0) {
        for (let i = 0; i < footerRows.length; i++) {
            if (footerRows[i].classList.contains('table-purple') || footerRows[i].classList.contains('no-export')) {
                continue
            }
            const row = processTableRow(footerRows[i], headerInfo)
            data.push(row)
        }
    }

    return { data, totalColumns: headerInfo.headerRow.length }
}

function getHeaderStructure(headerCells) {
    const headerRow = []
    const excludeColumnIndices = []
    const columnMapping = []
    let outputColIndex = 0

    const conversionesVisibles = document.querySelector('.unit-toggle-columns.visible') !== null

    headerCells.forEach((cell, index) => {
        const headerText = cell.innerText.trim()

        if (headerText === 'Acciones') {
            excludeColumnIndices.push(index)
            columnMapping[index] = -1
            return
        }

        if (headerText === 'Guía') {
            headerRow.push('Guía')
            headerRow.push('Guía Alterna')
            columnMapping[index] = outputColIndex
            outputColIndex += 2
            return
        }

        if (headerText === 'Placa') {
            headerRow.push('Placa')
            headerRow.push('Placa Alterna')
            columnMapping[index] = outputColIndex
            outputColIndex += 2
            return
        }

        if (cell.classList.contains('unit-toggle-columns')) {
            if (conversionesVisibles) {
                const headerName = cell.textContent.trim()

                headerRow.push(`${headerName} (Libras)`)
                headerRow.push(`${headerName} (Quintales)`)

                columnMapping[index] = [outputColIndex, outputColIndex + 1]
                outputColIndex += 2
            } else {
                excludeColumnIndices.push(index)
                columnMapping[index] = -1
            }
        } else {
            headerRow.push(headerText)
            columnMapping[index] = outputColIndex
            outputColIndex++
        }
    })

    return {
        headerRow,
        excludeColumnIndices,
        columnMapping,
        totalColumns: outputColIndex,
    }
}

function processTableRow(row, headerInfo) {
    const { headerRow, excludeColumnIndices, columnMapping } = headerInfo
    const rowData = Array(headerRow.length).fill('')
    const cells = row.querySelectorAll('td, th')

    let colIndex = 0
    for (let j = 0; j < cells.length; j++) {
        const cell = cells[j]
        const colspan = parseInt(cell.getAttribute('colspan') || '1')

        if (excludeColumnIndices.includes(colIndex)) {
            colIndex += colspan
            continue
        }

        if (cell.classList.contains('unit-toggle-columns')) {
            if (!excludeColumnIndices.includes(colIndex)) {
                const topValue = cell.querySelector('.top-value')?.textContent.trim() || cell.innerText.trim()
                const bottomValue = cell.querySelector('.bottom-value')?.textContent.trim() || ''

                const [lbsIndex, qqIndex] = Array.isArray(columnMapping[colIndex])
                    ? columnMapping[colIndex]
                    : [columnMapping[colIndex], columnMapping[colIndex] + 1]

                const lbsValue = topValue.replace(/[^\d.,\-]/g, '').trim()
                rowData[lbsIndex] = lbsValue

                const qqValue = bottomValue.replace(/[^\d.,\-]/g, '').trim()
                rowData[qqIndex] = qqValue
            }
        } else if (colspan > 1) {
            rowData[columnMapping[colIndex]] = cell.innerText.trim()
        } else {
            if (columnMapping[colIndex] !== undefined && columnMapping[colIndex] !== -1) {
                if (cell.querySelector('.data-alt-guia') || cell.querySelector('.data-alt-placa')) {
                    const mainText = cell.querySelector('div > span')?.textContent.trim() || cell.innerText.trim()
                    rowData[columnMapping[colIndex]] = mainText

                    if (cell.querySelector('.data-alt-guia')) {
                        const altText = cell.querySelector('.data-alt-guia').textContent.trim()
                        rowData[columnMapping[colIndex] + 1] = altText !== '' ? altText : '-'
                    }
                    else if (cell.querySelector('.data-alt-placa')) {
                        const altText = cell.querySelector('.data-alt-placa').textContent.trim()
                        rowData[columnMapping[colIndex] + 1] = altText !== '' ? altText : '-'
                    }
                } else {
                    rowData[columnMapping[colIndex]] = cell.innerText.trim()
                }
            }
        }

        colIndex += colspan
    }

    return rowData
}

function extraerNumero(texto) {
    if (!texto) return 0

    let cleaned = texto.toString().replace(/\s/g, '')

    if (cleaned.includes('.') && cleaned.includes(',')) {
        cleaned = cleaned.replace(/\./g, '').replace(',', '.')
    } else if (cleaned.includes('.') && !cleaned.includes(',')) {

        const parts = cleaned.split('.')
        if (parts.length > 2 || (parts.length === 2 && parts[1].length !== 2)) {
            cleaned = cleaned.replace(/\./g, '')
        }
    } else if (cleaned.includes(',')) {
        cleaned = cleaned.replace(',', '.')
    }

    cleaned = cleaned.replace(/[^\d.\-]/g, '')

    const value = parseFloat(cleaned)
    return isNaN(value) ? 0 : value
}

function formatNumbersInExcelSheet(worksheet, data) {
    const headers = data[0] || []

    for (let i = 1; i < data.length; i++) {
        for (let j = 0; j < data[i].length; j++) {
            const cellAddress = XLSX.utils.encode_cell({ r: i, c: j })
            const cellValue = data[i][j]

            if (!cellValue && cellValue !== 0) continue

            const header = headers[j] || ''
            const isPercentageColumn =
                header.includes('%') ||
                header.toLowerCase().includes('porc') ||
                header.toLowerCase().includes('desc.') ||
                header === '% Desc.'

            const isWeightColumn =
                header.includes('Kg') ||
                header.includes('Ton') ||
                header.includes('Lbs') ||
                header.includes('Qq') ||
                header.includes('Libras') ||
                header.includes('Quintales')

            if (typeof cellValue === 'string' && /^-?[\d.,]+%?$/.test(cellValue.trim())) {
                if (cellValue.includes('%')) {
                    const numValue = extraerNumero(cellValue) / 100
                    worksheet[cellAddress] = {
                        v: numValue,
                        t: 'n',
                        z: '0.00%',
                    }
                }
                else {
                    const numValue = extraerNumero(cellValue)
                    worksheet[cellAddress] = {
                        v: numValue,
                        t: 'n',
                        z: isPercentageColumn
                            ? '0.00%'
                            : isWeightColumn
                            ? numValue >= 1000
                                ? '#,##0.00'
                                : '0.00'
                            : '#,##0.00',
                    }
                }
            }
            else if (typeof cellValue === 'number') {
                worksheet[cellAddress] = {
                    v: cellValue,
                    t: 'n',
                    z: isPercentageColumn
                        ? '0.00%'
                        : isWeightColumn
                        ? cellValue >= 1000
                            ? '#,##0.00'
                            : '0.00'
                        : '#,##0.00',
                }

                if (isPercentageColumn && cellValue < 1) {
                    worksheet[cellAddress].v = cellValue
                } else if (isPercentageColumn) {
                    worksheet[cellAddress].v = cellValue / 100
                }
            }
        }
    }
}

function calculateColumnWidths(data) {
    const colWidths = data[0].map(header => Math.max(10, header.length * 1.2));
    
    for (let rowIdx = 1; rowIdx < data.length; rowIdx++) {
        const row = data[rowIdx];
        for (let colIdx = 0; colIdx < row.length; colIdx++) {
            const cellValue = row[colIdx];
            
            if (cellValue === undefined || cellValue === null || cellValue === '') {
                continue; 
            }
            
            let displayWidth;
            
            if (typeof cellValue === 'number') {
                const numStr = cellValue.toLocaleString('es-GT');
                displayWidth = numStr.length + 1;  
            } else {
                displayWidth = String(cellValue).length;
            }
            
            colWidths[colIdx] = Math.max(colWidths[colIdx], displayWidth);
        }
    }
    
    return colWidths.map((width, idx) => {
        const header = data[0][idx] || '';
        
        if (header.includes('Empresa')) {
            return { wch: Math.max(width, 25) }; 
        } else if (header.includes('Guía') || header.includes('Placa')) {
            return { wch: Math.max(width, 15) }; 
        } else if (header.includes('%')) {
            return { wch: Math.min(width + 2, 12) }; 
        } else if (header.includes('Libras') || header.includes('Quintales') || 
                  header.includes('Kg') || header.includes('Ton')) {
            return { wch: Math.min(width + 3, 15) }; 
        }
        
        return { wch: Math.min(Math.max(width, 8), 25) };
    });
}

function hasTableData(tableId) {
    const table = document.getElementById(tableId)
    if (!table) return false

    const tbody = table.querySelector('tbody')
    if (!tbody) return false

    const rows = tbody.querySelectorAll('tr')
    if (rows.length === 0) return false

    if (rows.length === 1) {
        const firstRow = rows[0]
        const cols = firstRow.querySelectorAll('td')
        if (cols.length === 1) {
            const cellContent = cols[0].innerText.trim().toLowerCase()
            const hasEmptyMessage =
                cellContent.includes('no data') ||
                cellContent.includes('no hay') ||
                cellContent.includes('sin datos') ||
                cellContent.includes('no registros') ||
                cellContent.includes('no records') ||
                cellContent === ''

            const hasAlertClass = cols[0].querySelector('.alert') !== null || cols[0].classList.contains('text-center')

            if (hasEmptyMessage || hasAlertClass) {
                return false
            }
        }
    }

    return true
}

function updateExcelButtonState() {
    const btnExport = $('#btnExportarExcel')
    const selectedBarco = $('select[name="selectedBarco"]').val()
    const hasData = hasTableData('tabla2')

    btnExport.prop('disabled', !selectedBarco || !hasData)

    const hasDataTabla1 = hasTableData('tabla1')
    $('#btnExportarExcelIndividuales').prop('disabled', !selectedBarco || !hasDataTabla1)
}

function formatNumber(num) {
    return new Intl.NumberFormat('es-GT').format(num)
}

function initializeWeightValues() {
    document.querySelectorAll('.unit-toggle-columns').forEach(el => {
        el.classList.remove('visible');
    });
    $('.unit-toggle-row').hide();
    $('.unit-toggle-element').hide();
    
    const savedMetricState = localStorage.getItem('showingMetric');
    if (savedMetricState === 'false') {
        window.showingMetric = false;
        toggleUnitDisplay(true);
    }
}

function toggleUnitDisplay(showAlternative) {
    $('.unit-toggle-row').each(function() {
        if (showAlternative) {
            $(this).slideDown(300);
        } else {
            $(this).slideUp(300);
        }
    });

    $('.unit-toggle-element').each(function() {
        if (showAlternative) {
            $(this).slideDown(300);
        } else {
            $(this).slideUp(300);
        }
    });

    const unitToggleColumns = document.querySelectorAll('.unit-toggle-columns');
    unitToggleColumns.forEach(el => {
        if (showAlternative) {
            el.classList.add('visible');
        } else {
            el.classList.remove('visible');
        }
    });
    
    window.dispatchEvent(new CustomEvent('unitToggleChanged', { 
        detail: { showAlternative: showAlternative } 
    }));
}

function setupUnitToggle() {
    const savedMetricState = localStorage.getItem('showingMetric');
    window.showingMetric = savedMetricState !== null ? savedMetricState === 'true' : true;

    $('#btnToggleUnidad').off('click').on('click', function () {
        if ($(this).prop('disabled')) return;
        window.showingMetric = !window.showingMetric;
        
        localStorage.setItem('showingMetric', window.showingMetric);

        const buttonText = $(this).find('span');
        buttonText.text(window.showingMetric ? 'Libras' : 'Kilogramos');
        $(this).attr('title', window.showingMetric ? 'Ver en Libras' : 'Ver en Kilogramos');
        $(this).find('i').toggleClass('fa-weight fa-balance-scale');
        
        $(this).attr('data-showing-metric', window.showingMetric);

        toggleUnitDisplay(!window.showingMetric);
    });

    const btnToggleUnidad = $('#btnToggleUnidad');
    if (btnToggleUnidad.length) {
        btnToggleUnidad.find('span').text(window.showingMetric ? 'Libras' : 'Kilogramos');
        btnToggleUnidad.attr('title', window.showingMetric ? 'Ver en Libras' : 'Ver en Kilogramos');
        btnToggleUnidad.find('i').toggleClass('fa-weight fa-balance-scale', window.showingMetric);
        btnToggleUnidad.attr('data-showing-metric', window.showingMetric);
    }
    
    toggleUnitDisplay(!window.showingMetric);

    const selectedBarco = $('select[name="selectedBarco"]').val();
    const hasData = hasTableData('tabla2');
    $('#btnToggleUnidad').prop('disabled', !selectedBarco || !hasData);
}

$(document).ready(function () {
    setupUnitToggle()
})

function adjustTableLayout() {
    $('.table-scroll').css('display', 'none').height()
    $('.table-scroll').css('display', '')
}

function initializeReporteIndividualLink() {
    $('#btnTablaDetallada').on('click', function (e) {
        const selectedBarco = $('select[name="selectedBarco"]').val()
        const hasData = hasTableData('tabla2')

        if ($(this).hasClass('disabled')) {
            e.preventDefault()
            return false
        }
    })

    $('select[name="selectedBarco"]').on('change', function () {
        const hasBarco = $(this).val() !== ''
        const hasData = hasTableData('tabla2')

        const btnTablaDetallada = $('#btnTablaDetallada')
        if (hasBarco && hasData) {
            btnTablaDetallada.removeClass('disabled')
        } else {
            btnTablaDetallada.addClass('disabled')
        }
    })
}

function handleBarcoChange(selectElement) {
    $('#barcoLoadingIndicator').removeClass('d-none')

    const empresaSelect = $('#empresaId')
    if (selectElement.value) {
        empresaSelect.prop('disabled', false)
    } else {
        empresaSelect.prop('disabled', true)
        empresaSelect.val('')
    }

    setTimeout(() => {
        $('#selectionForm').submit()
    }, 100)
}

function handleEmpresaChange(selectElement) {
    $('#empresaLoadingIndicator').removeClass('d-none')

    setTimeout(() => {
        $('#selectionForm').submit()
    }, 100)
}

function exportResumenAgregadoToExcel(tableId, filename) {
    const table = document.getElementById(tableId)
    if (!table) {
        return
    }

    try {
        const wb = XLSX.utils.book_new()

        const data = []

        const conversionesVisibles = document.querySelector('.unit-toggle-columns.visible') !== null

        const headerRow = []
        const headerCells = table.querySelectorAll('thead th')
        const unitToggleColumns = [] 
        const columnTypes = [] 

        headerCells.forEach((cell, index) => {
            if (cell.classList.contains('unit-toggle-columns')) {
                if (conversionesVisibles) {
                    const headerName =
                        cell.querySelector('div:first-child')?.textContent.trim() || cell.textContent.split('\n')[0].trim()
                    headerRow.push(`${headerName} (Quintales)`)
                    headerRow.push(`${headerName} (Libras)`)
                    unitToggleColumns.push(index)
                    columnTypes.push('quintales')
                    columnTypes.push('libras')
                }
            } else if (cell.textContent.includes('Acciones')) {
                return
            } else {
                const headerText = cell.textContent.trim();
                headerRow.push(headerText)
                
                if (headerText.includes('%')) {
                    columnTypes.push('percentage')
                } else if (headerText.includes('Kg') || headerText.includes('Ton')) {
                    columnTypes.push('weight')
                } else if (headerText === 'Empresa') {
                    columnTypes.push('text')
                } else {
                    columnTypes.push('numeric')
                }
            }
        })

        data.push(headerRow)

        function processNumericText(text) {
            const originalText = text.trim()

            if (!originalText || originalText === '-') {
                return { text: originalText, value: '', isNumeric: false }
            }

            const numberPattern = /[-+]?[\d.,]+/;
            const match = originalText.match(numberPattern);
            
            if (!match) {
                return { text: originalText, value: '', isNumeric: false }
            }
            
            const numStr = match[0];
            
            const isEuropeanFormat = /\d{1,3}(?:\.\d{3})+(?:,\d+)?/.test(numStr);

            let numericValue;
            if (isEuropeanFormat) {
                numericValue = numStr.replace(/\./g, '').replace(',', '.')
            } else {
                numericValue = numStr.replace(',', '.')
            }

            const value = parseFloat(numericValue);
            return {
                text: originalText,
                value: isNaN(value) ? '' : value,
                isNumeric: !isNaN(value)
            }
        }

        const rows = table.querySelectorAll('tbody tr')
        rows.forEach(row => {
            const rowData = []
            const cells = row.querySelectorAll('td')

            cells.forEach((cell, index) => {
                if (unitToggleColumns.includes(index)) {
                    if (conversionesVisibles) {
                        const qqText = cell.querySelector('.top-value')?.textContent.trim() || ''
                        const lbsText = cell.querySelector('.bottom-value')?.textContent.trim() || ''

                        const qqProcessed = processNumericText(qqText.replace('qq', '').trim())
                        rowData.push(qqProcessed.isNumeric ? qqProcessed.value : qqProcessed.text)

                        const lbsProcessed = processNumericText(lbsText.replace('lbs', '').trim())
                        rowData.push(lbsProcessed.isNumeric ? lbsProcessed.value : lbsProcessed.text)
                    }
                } else {
                    if (headerRow[rowData.length] !== "Acciones") { 
                        const cellText = cell.textContent.trim();
                        const processed = processNumericText(cellText)
                        rowData.push(processed.isNumeric ? processed.value : processed.text)
                    }
                }
            })

            data.push(rowData)
        })

        const ws = XLSX.utils.aoa_to_sheet(data)

        for (let r = 1; r < data.length; r++) {
            for (let c = 0; c < data[r].length; c++) {
                const cellRef = XLSX.utils.encode_cell({ r, c })
                if (!ws[cellRef]) continue;

                const cellValue = data[r][c];
                
                if (cellValue === '' || cellValue === undefined || cellValue === null) {
                    continue;
                }
                
                if (typeof cellValue === 'number') {
                    ws[cellRef].t = 'n';
                    
                    const columnType = columnTypes[c];
                    if (columnType === 'percentage') {
                        ws[cellRef].z = '0.00%';
                        if (cellValue > 1) {
                            ws[cellRef].v = cellValue / 100;
                        }
                    } else if (columnType === 'quintales') {
                        ws[cellRef].z = '#,##0.00';
                    } else if (columnType === 'libras') {
                        ws[cellRef].z = '#,##0';
                    } else if (columnType === 'weight') {
                        ws[cellRef].z = '#,##0.00';
                    } else {
                        ws[cellRef].z = '#,##0.00';
                    }
                }

                if (!ws[cellRef].s) ws[cellRef].s = {};

                if (c === 0) {
                    ws[cellRef].s.alignment = { horizontal: 'left' };
                } else {
                    ws[cellRef].s.alignment = { horizontal: 'right' };
                }

                if (r === 0) {
                    ws[cellRef].s.font = { bold: true };
                }
            }
        }

        ws['!cols'] = calculateColumnWidths(data);

        XLSX.utils.book_append_sheet(wb, ws, 'Resumen')

        const dateStr = new Date().toISOString().slice(0, 10)
        XLSX.writeFile(wb, `${filename}_${dateStr}.xlsx`)
    } catch (error) {
        alert('Error al exportar a Excel: ' + error.message)
    }
}

function exportTableToExcel(tableId, filename) {
    const table = document.getElementById(tableId)
    if (!table) {
        return
    }

    try {
        const { data } = createExcelDataFromTable(table)
        const wb = XLSX.utils.book_new()
        const ws = XLSX.utils.aoa_to_sheet(data)

        formatNumbersInExcelSheet(ws, data)

        ws['!cols'] = calculateColumnWidths(data)

        XLSX.utils.book_append_sheet(wb, ws, 'Resumen')
        XLSX.writeFile(wb, filename + '_' + new Date().toISOString().slice(0, 10) + '.xlsx')
    } catch (error) {
        alert('Error al exportar a Excel: ' + error.message)
    }
}

function updateAllExcelButtonStates() {
    const selectedBarco = $('select[name="selectedBarco"]').val()
    const hasDataTabla2 = hasTableData('tabla2')
    const hasDataTabla1 = hasTableData('tabla1')

    $('#btnExportarExcel').prop('disabled', !selectedBarco || !hasDataTabla2)
    $('#btnExportarExcelIndividuales').prop('disabled', !selectedBarco || !hasDataTabla1)

    $('#btnExportarExcelResumen').prop('disabled', !hasDataTabla2)
}

$(document).ready(function () {
    cargarImportaciones()

    $('#selectImportacion').on('change', function () {
        var importacionId = $(this).val()
        if (importacionId) {
            cargarEmpresas(importacionId)
        } else {
            $('#selectEmpresa')
                .empty()
                .append($('<option>', { value: '', text: 'Seleccione una empresa' }))
                .prop('disabled', true)
        }
    })

    $('#btnAgregar').on('click', function () {
        var selectedBarco = $(this).data('selected-barco')
        var empresaId = $(this).data('empresa-id')

        if (!selectedBarco || !empresaId) {
            alert('Por favor seleccione una importación y una empresa')
            return
        }

        window.location.href = `/RegistroPesajes/Create?selectedBarco=${selectedBarco}&empresaId=${empresaId}`
    })

    initializeTableScroll()
    handleResponsiveLayout()
    ;['tabla1', 'tabla2', 'tabla3'].forEach(tableId => {
        if ($(`#${tableId}`).length) {
            ajustarAnchoColumnas(tableId)
        }
    })

    let resizeTimeout
    $(window).resize(function () {
        clearTimeout(resizeTimeout)
        resizeTimeout = setTimeout(function () {
            optimizeTables()
            handleResponsiveLayout()
            ;['tabla1', 'tabla2', 'tabla3'].forEach(tableId => {
                if ($(`#${tableId}`).length) {
                    ajustarAnchoColumnas(tableId)
                }
            })
        }, 250)
    })

    $(document).ajaxComplete(function () {
        initializeTableScroll()
        updateAllExcelButtonStates()
    })

    setupEscotillasToggle()

    if (typeof initializeServerData === 'function') {
        initializeServerData('@Context.Request.Query["selectedBarco"]', '@Context.Request.Query["empresaId"]')
    }

    updateAllExcelButtonStates()

    $('select[name="selectedBarco"]').on('change', function () {
        updateAllExcelButtonStates()
    })

    $('#btnExportarExcel').on('click', function () {
        exportTableToExcel('tabla2', 'Resumen_Agregado')
    })

    $('#btnToggleUnidad').prop('disabled', true)

    setupUnitToggle()

    initializeWeightValues()

    initializeReporteIndividualLink()

    $('#btnExportarExcelIndividuales')
        .off('click')
        .on('click', function () {
            if (typeof XLSX === 'undefined') {
                alert('La librería XLSX no está cargada correctamente')
                return
            }

            const tabla = document.getElementById('tabla1')
            if (!tabla) {
                alert('No se encontró la tabla de registros individuales')
                return
            }

            try {
                const wb = XLSX.utils.book_new()

                const headers = [
                    '#',
                    'Esc.',
                    'Bodega',
                    'Guía',
                    'Guía Alterna',
                    'Placa',
                    'Placa Alterna',
                    'Peso Requerido',
                    'Peso Entregado',
                    'Entreg. (Lbs)',
                    'Entreg. (Qq)',
                    'Peso Faltante',
                    'Porcentaje',
                ]

                const data = [headers]

                const bodyRows = tabla.querySelectorAll('tbody tr')

                Array.from(bodyRows).forEach((row, idx) => {
                    try {
                        const cells = row.querySelectorAll('td')
                        if (cells.length < 9) return

                        const numfila = cells[0].textContent.trim()

                        const escotilla = cells[1].textContent.trim()

                        const bodega = cells[2].textContent.trim()

                        const guia =
                            cells[3].querySelector('div > span')?.textContent.trim() || cells[3].textContent.trim()
                        const guiaAlt = cells[3].querySelector('.data-alt-guia')?.textContent.trim() || ''

                        const placa =
                            cells[4].querySelector('div > span')?.textContent.trim() || cells[4].textContent.trim()
                        const placaAlt = cells[4].querySelector('.data-alt-placa')?.textContent.trim() || ''

                        const pesoRequerido = extraerNumero(cells[5].textContent)
                        const pesoEntregado = extraerNumero(cells[6].textContent)

                        let libras = 0,
                            quintales = 0
                        const conversionCell = cells[7]
                        if (conversionCell) {
                            const lbsText = conversionCell.querySelector('.top-value')?.textContent || ''
                            const qqText = conversionCell.querySelector('.bottom-value')?.textContent || ''
                            libras = extraerNumero(lbsText)
                            quintales = extraerNumero(qqText)
                        }

                        const pesoFaltante = extraerNumero(cells[8].textContent)
                        const porcentaje = extraerNumero(cells[9].textContent) / 100

                        const rowData = [
                            numfila,
                            escotilla,
                            bodega,
                            guia,
                            guiaAlt,
                            placa,
                            placaAlt,
                            pesoRequerido,
                            pesoEntregado,
                            libras,
                            quintales,
                            pesoFaltante,
                            porcentaje,
                        ]

                        data.push(rowData)
                    } catch (rowError) {}
                })

                const footerRow = tabla.querySelector('tfoot tr')
                if (footerRow) {
                    const cells = footerRow.querySelectorAll('td')

                    const totalRow = Array(headers.length).fill('')
                    totalRow[0] = ''
                    totalRow[1] = ''
                    totalRow[2] = 'Totales'
                    totalRow[3] = ''
                    totalRow[4] = ''
                    totalRow[5] = ''
                    totalRow[6] = ''

                    totalRow[7] = extraerNumero(cells[5]?.textContent || '0') 
                    totalRow[8] = extraerNumero(cells[6]?.textContent || '0') 

                    const conversionCell = cells[7]
                    if (conversionCell) {
                        totalRow[9] = extraerNumero(conversionCell.querySelector('.top-value')?.textContent || '0')
                        totalRow[10] = extraerNumero(conversionCell.querySelector('.bottom-value')?.textContent || '0')
                    }

                    totalRow[11] = extraerNumero(cells[8]?.textContent || '0')
                    totalRow[12] = extraerNumero(cells[9]?.textContent || '0') / 100

                    data.push(totalRow)
                }

                const ws = XLSX.utils.aoa_to_sheet(data)

                for (let i = 1; i < data.length; i++) {
                    for (let j of [7, 8, 9, 10, 11]) {
                        const cellRef = XLSX.utils.encode_cell({ r: i, c: j })
                        if (ws[cellRef] && typeof data[i][j] === 'number') {
                            ws[cellRef].t = 'n'
                            ws[cellRef].z = '#,##0.00'
                        }
                    }

                    const percentRef = XLSX.utils.encode_cell({ r: i, c: 12 })
                    if (ws[percentRef] && typeof data[i][12] === 'number') {
                        ws[percentRef].t = 'n'
                        ws[percentRef].z = '0.00%'
                    }
                }

                ws['!cols'] = headers.map(h => ({ wch: Math.max(h.length * 1.2, 10) }))

                XLSX.utils.book_append_sheet(wb, ws, 'Registros Individuales')
                XLSX.writeFile(wb, 'Registros_Individuales_' + new Date().toISOString().slice(0, 10) + '.xlsx')
            } catch (error) {
                alert('Error durante la exportación: ' + error.message)
            }
        })

    $('#btnExportarExcelResumen')
        .off('click')
        .on('click', function () {
            exportResumenAgregadoToExcel('tabla2', 'Resumen_Agregado')
        })

    window.exportTableToExcel = exportTableToExcel
    window.extraerNumero = extraerNumero
})
