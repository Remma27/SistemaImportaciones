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

    // Verificar si al menos una columna de conversión está visible
    const conversionesVisibles = document.querySelector('.unit-toggle-columns.visible') !== null

    headerCells.forEach((cell, index) => {
        const isHidden = !isElementVisible(cell)

        // Lógica para manejar columnas toggle
        if (cell.classList.contains('unit-toggle-columns')) {
            const shouldInclude = isElementVisible(cell)
            if (!shouldInclude) {
                excludeColumnIndices.push(index)
                return
            }

            // Si es visible, agregar las dos columnas (libras y quintales)
            const headerName = cell.textContent.trim()
            headerRow.push(`${headerName} (Libras)`)
            headerRow.push(`${headerName} (Quintales)`)
            columnMapping[index] = [outputColIndex, outputColIndex + 1]
            outputColIndex += 2
            return
        }

        // Para las otras columnas, continuar con la lógica existente
        if (isHidden) {
            excludeColumnIndices.push(index)
            columnMapping[index] = -1
            return
        }

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

        headerRow.push(headerText)
        columnMapping[index] = outputColIndex
        outputColIndex++
    })

    return {
        headerRow,
        excludeColumnIndices,
        columnMapping,
        totalColumns: outputColIndex,
    }
}

const columnFormatRules = {
    // Porcentajes
    '% Desc.': { type: 'percentage', decimals: 2 },
    Porcentaje: { type: 'percentage', decimals: 2 },

    // Unidades de peso
    Libras: { type: 'weight', unit: 'lbs', decimals: 2 },
    Quintales: { type: 'weight', unit: 'qq', decimals: 2 },
    Kg: { type: 'weight', unit: 'kg', decimals: 2 },
    Ton: { type: 'weight', unit: 'ton', decimals: 2 },

    // Campos monetarios
    Total: { type: 'currency', symbol: 'Q', decimals: 2 },
    Precio: { type: 'currency', symbol: 'USD', decimals: 2 },

    // Campos especiales
    Guía: { type: 'text', format: 'guia' },
    Placa: { type: 'text', format: 'placa' },

    // Valores numéricos grandes
    Cantidad: { type: 'number', format: 'comma', decimals: 0 },
}

function processTableRow(row, headerInfo) {
    const { headerRow, excludeColumnIndices, columnMapping } = headerInfo
    const rowData = Array(headerRow.length).fill('')
    const cells = row.querySelectorAll('td, th')

    let colIndex = 0
    for (let j = 0; j < cells.length; j++) {
        const cell = cells[j]
        const isHidden = !isElementVisible(cell)

        if (isHidden) {
            colIndex += colspan
            continue
        }

        // Verificar si la celda está oculta
        const $cell = $(cell)

        if (isHidden) {
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
                    } else if (cell.querySelector('.data-alt-placa')) {
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

// Improved number extraction and formatting for international compatibility
function extraerNumero(texto, mantenerDecimales = false) {
    if (!texto) return 0;
    
    // Check for negative numbers (both - and parentheses format)
    const esNegativo = texto.toString().trim().startsWith('-') || 
                       (texto.toString().trim().includes('(') && 
                        texto.toString().trim().includes(')'));
    
    let cleaned = texto.toString()
        .trim()
        .replace(/[\$€\s]/g, '')  // Remove currency symbols and spaces
        .replace(/\(([^)]+)\)/, '-$1');  // Convert (123) to -123
    
    if (cleaned === '' || cleaned === '-') return 0;
    
    // IMPROVED: Detect numbers with thousands separators like 9,318,790
    const comas = cleaned.match(/,/g) || [];
    const puntos = cleaned.match(/\./g) || [];
    
    // Check for American format (thousands with commas, decimal with period)
    // For example: 9,318,790 or 9,318,790.25
    const esFormatoAmericano = 
        (comas.length > 1) || // Multiple commas indicate thousands separators
        (comas.length === 1 && cleaned.match(/,\d{3}($|\D)/)) || // Comma followed by exactly 3 digits
        (puntos.length === 1 && cleaned.match(/\.\d{1,2}$/)); // Period followed by 1-2 digits at the end
    
    // Check for European format (thousands with periods, decimal with comma)
    // For example: 9.318.790 or 9.318.790,25
    const esFormatoEuropeo = 
        (puntos.length > 1) || // Multiple periods indicate thousands separators
        (puntos.length === 1 && cleaned.match(/\.\d{3}($|\D)/)) || // Period followed by exactly 3 digits
        (comas.length === 1 && cleaned.match(/,\d{1,2}$/)); // Comma followed by 1-2 digits at the end
    
    // Handle American format (9,318,790)
    if (esFormatoAmericano) {
        // Remove all commas (thousands separators)
        cleaned = cleaned.replace(/,/g, '');
    }
    // Handle European format (9.318.790,25)
    else if (esFormatoEuropeo) {
        cleaned = cleaned
            .replace(/\.(?=.*,)/g, '')  // Remove periods (thousands separators)
            .replace(',', '.');         // Convert decimal comma to period
    }
    // If there is only one comma or period, treat as decimal separator
    else if (cleaned.includes(',')) {
        cleaned = cleaned.replace(',', '.');
    }
    
    // Clean up multiple decimal points if they somehow remain
    const decimalPoints = cleaned.match(/\./g) || [];
    if (decimalPoints.length > 1) {
        cleaned = cleaned.replace(/\./g, (match, index, string) => 
            index === string.lastIndexOf('.') ? '.' : '');
    }
    
    let valor = parseFloat(cleaned);
    
    // Ensure negative values
    if (esNegativo && valor > 0) {
        valor = -valor;
    }
    
    if (!mantenerDecimales) {
        return isNaN(valor) ? 0 : valor;
    }
    
    // Preserve original decimal places if requested
    const decimalMatch = texto.toString().match(/[.,](\d+)(?!\d)/);
    if (decimalMatch && decimalMatch[1]) {
        const decimales = decimalMatch[1].length;
        return isNaN(valor) ? 0 : Number(valor.toFixed(decimales));
    }
    
    return isNaN(valor) ? 0 : valor;
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
                } else {
                    const numValue = extraerNumero(cellValue)
                    worksheet[cellAddress] = {
                        v: numValue,
                        t: 'n',
                        z: isPercentageColumn ? '0.00%' : '#,##0.00', // Always use 2 decimal places
                    }
                }
            } else if (typeof cellValue === 'number') {
                worksheet[cellAddress] = {
                    v: cellValue,
                    t: 'n',
                    z: isPercentageColumn ? '0.00%' : '#,##0.00', // Always use 2 decimal places
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

function calculateColumnWidths(headers) {
    return headers.map(header => {
        const rule = getFormatRule(header)

        // Anchuras basadas en tipo de dato
        if (rule) {
            switch (rule.type) {
                case 'text':
                    return 20
                case 'percentage':
                    return 10
                case 'currency':
                    return 15
                case 'weight':
                    return rule.unit === 'qq' ? 14 : 12
                default:
                    return 18
            }
        }

        // Anchura por defecto basada en contenido
        return Math.min(Math.max(header.length * 1.3, 12), 25)
    })
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
        el.classList.remove('visible')
    })
    $('.unit-toggle-row').hide()
    $('.unit-toggle-element').hide()

    const savedMetricState = localStorage.getItem('showingMetric')
    if (savedMetricState === 'false') {
        window.showingMetric = false
        toggleUnitDisplay(true)
    }
}

function toggleUnitDisplay(showAlternative) {
    $('.unit-toggle-row').each(function () {
        if (showAlternative) {
            $(this).slideDown(300)
        } else {
            $(this).slideUp(300)
        }
    })

    $('.unit-toggle-element').each(function () {
        if (showAlternative) {
            $(this).slideDown(300)
        } else {
            $(this).slideUp(300)
        }
    })

    const unitToggleColumns = document.querySelectorAll('.unit-toggle-columns')
    unitToggleColumns.forEach(el => {
        if (showAlternative) {
            el.classList.add('visible')
        } else {
            el.classList.remove('visible')
        }
    })

    window.dispatchEvent(
        new CustomEvent('unitToggleChanged', {
            detail: { showAlternative: showAlternative },
        }),
    )
}

function setupUnitToggle() {
    const savedMetricState = localStorage.getItem('showingMetric')
    window.showingMetric = savedMetricState !== null ? savedMetricState === 'true' : true

    $('#btnToggleUnidad')
        .off('click')
        .on('click', function () {
            if ($(this).prop('disabled')) return
            window.showingMetric = !window.showingMetric

            localStorage.setItem('showingMetric', window.showingMetric)

            const buttonText = $(this).find('span')
            buttonText.text(window.showingMetric ? 'Libras' : 'Kilogramos')
            $(this).attr('title', window.showingMetric ? 'Ver en Libras' : 'Ver en Kilogramos')
            $(this).find('i').toggleClass('fa-weight fa-balance-scale')

            $(this).attr('data-showing-metric', window.showingMetric)

            toggleUnitDisplay(!window.showingMetric)
        })

    const btnToggleUnidad = $('#btnToggleUnidad')
    if (btnToggleUnidad.length) {
        btnToggleUnidad.find('span').text(window.showingMetric ? 'Libras' : 'Kilogramos')
        btnToggleUnidad.attr('title', window.showingMetric ? 'Ver en Libras' : 'Ver en Kilogramos')
        btnToggleUnidad.find('i').toggleClass('fa-weight fa-balance-scale', window.showingMetric)
        btnToggleUnidad.attr('data-showing-metric', window.showingMetric)
    }

    toggleUnitDisplay(!window.showingMetric)

    const selectedBarco = $('select[name="selectedBarco"]').val()
    const hasData = hasTableData('tabla2')
    $('#btnToggleUnidad').prop('disabled', !selectedBarco || !hasData)
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

                const headerCells = tabla.querySelectorAll('thead th')
                const visibleColumns = []
                const columnHeaders = []

                const conversionesVisibles = document.querySelector('.unit-toggle-columns.visible') !== null

                headerCells.forEach((cell, index) => {
                    const $cell = $(cell)

                    if (cell.classList.contains('unit-toggle-columns')) {
                        if (cell.classList.contains('visible')) {
                            visibleColumns.push(index)
                            const headerText = cell.textContent.trim()
                            columnHeaders.push(`${headerText} (Libras)`, `${headerText} (Quintales)`)
                        }
                        return
                    }

                    const isHidden =
                        !$cell.is(':visible') ||
                        cell.offsetParent === null ||
                        $cell.css('display') === 'none' ||
                        $cell.hasClass('d-none') ||
                        $cell.closest('th, td').css('display') === 'none'

                    if (!isHidden && !cell.textContent.includes('Acciones')) {
                        visibleColumns.push(index)
                        let headerText = cell.textContent.trim()

                        if (headerText === 'Guía') {
                            columnHeaders.push('Guía', 'Guía Alterna')
                        } else if (headerText === 'Placa') {
                            columnHeaders.push('Placa', 'Placa Alterna')
                        } else {
                            columnHeaders.push(headerText)
                        }
                    }
                })

                const data = [columnHeaders]

                const bodyRows = tabla.querySelectorAll('tbody tr')

                Array.from(bodyRows).forEach((row, idx) => {
                    try {
                        const cells = row.querySelectorAll('td')
                        if (cells.length < Math.min(9, visibleColumns.length)) return

                        const rowData = []

                        visibleColumns.forEach(colIndex => {
                            if (colIndex >= cells.length) return

                            const cell = cells[colIndex]
                            const $cell = $(cell)
                            const isHidden =
                                !$cell.is(':visible') ||
                                cell.offsetParent === null ||
                                $cell.css('display') === 'none' ||
                                $cell.hasClass('d-none')

                            if (isHidden) return

                            if (headerCells[colIndex].textContent.trim() === 'Guía') {
                                const guia =
                                    cell.querySelector('div > span')?.textContent.trim() || cell.textContent.trim()
                                const guiaAlt = cell.querySelector('.data-alt-guia')?.textContent.trim() || ''
                                rowData.push(guia, guiaAlt !== '' ? guiaAlt : '-')
                            } else if (headerCells[colIndex].textContent.trim() === 'Placa') {
                                const placa =
                                    cell.querySelector('div > span')?.textContent.trim() || cell.textContent.trim()
                                const placaAlt = cell.querySelector('.data-alt-placa')?.textContent.trim() || ''
                                rowData.push(placa, placaAlt !== '' ? placaAlt : '-')
                            } else if (cell.classList.contains('unit-toggle-columns') && conversionesVisibles) {
                                const lbsText = cell.querySelector('.top-value')?.textContent || ''
                                const qqText = cell.querySelector('.bottom-value')?.textContent || ''
                                rowData.push(extraerNumero(lbsText), extraerNumero(qqText))
                            } else {
                                const cellText = cell.textContent.trim()
                                if (
                                    ['Peso Requerido', 'Peso Entregado', 'Peso Faltante'].includes(
                                        headerCells[colIndex].textContent.trim(),
                                    )
                                ) {
                                    rowData.push(extraerNumero(cellText))
                                } else if (headerCells[colIndex].textContent.trim().includes('Porcentaje')) {
                                    rowData.push(extraerNumero(cellText) / 100)
                                } else {
                                    rowData.push(cellText)
                                }
                            }
                        })

                        data.push(rowData)
                    } catch (rowError) {
                        console.error('Error procesando fila:', rowError)
                    }
                })

                const footerRow = tabla.querySelector('tfoot tr')
                if (footerRow) {
                    const cells = footerRow.querySelectorAll('td')
                    const totalRow = Array(columnHeaders.length).fill('')

                    let currentCol = 0
                    visibleColumns.forEach(colIndex => {
                        if (colIndex >= cells.length) return

                        const cell = cells[colIndex]
                        const $cell = $(cell)
                        const isHidden =
                            !$cell.is(':visible') ||
                            cell.offsetParent === null ||
                            $cell.css('display') === 'none' ||
                            $cell.hasClass('d-none')

                        if (isHidden) return

                        if (headerCells[colIndex].textContent.trim() === 'Bodega') {
                            totalRow[currentCol] = 'Totales'
                            currentCol++
                        } else if (
                            headerCells[colIndex].textContent.trim() === 'Guía' ||
                            headerCells[colIndex].textContent.trim() === 'Placa'
                        ) {
                            totalRow[currentCol] = ''
                            totalRow[currentCol + 1] = ''
                            currentCol += 2
                        } else if (cell.classList.contains('unit-toggle-columns') && conversionesVisibles) {
                            totalRow[currentCol] = extraerNumero(cell.querySelector('.top-value')?.textContent || '0')
                            totalRow[currentCol + 1] = extraerNumero(
                                cell.querySelector('.bottom-value')?.textContent || '0',
                            )
                            currentCol += 2
                        } else if (
                            ['Peso Requerido', 'Peso Entregado', 'Peso Faltante'].includes(
                                headerCells[colIndex].textContent.trim(),
                            )
                        ) {
                            totalRow[currentCol] = extraerNumero(cell.textContent || '0')
                            currentCol++
                        } else if (headerCells[colIndex].textContent.trim().includes('Porcentaje')) {
                            totalRow[currentCol] = extraerNumero(cell.textContent || '0') / 100
                            currentCol++
                        } else {
                            totalRow[currentCol] = cell.textContent.trim()
                            currentCol++
                        }
                    })

                    data.push(totalRow)
                }

                const ws = XLSX.utils.aoa_to_sheet(data)

                for (let i = 1; i < data.length; i++) {
                    for (let j = 0; j < data[i].length; j++) {
                        const cellRef = XLSX.utils.encode_cell({ r: i, c: j })
                        if (!ws[cellRef]) continue

                        if (typeof data[i][j] === 'number') {
                            ws[cellRef].t = 'n'

                            const headerText = data[0][j] || ''

                            if (headerText.includes('Porcentaje')) {
                                ws[cellRef].z = '0.00%'
                            } else {
                                ws[cellRef].z = '#,##0.00'
                            }
                        }
                    }
                }

                ws['!cols'] = calcColumnWidths(data)

                XLSX.utils.book_append_sheet(wb, ws, 'Registros Individuales')
                XLSX.writeFile(wb, 'Registros_Individuales_' + new Date().toISOString().slice(0, 10) + '.xlsx')
            } catch (error) {
                console.error('Error completo:', error)
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
function exportTableToExcel(tableId, filename) {
    const table = document.getElementById(tableId)
    if (!table) {
        alert('Tabla no encontrada')
        return
    }

    try {
        const wb = XLSX.utils.book_new()
        const data = []
        const headers = []
        const columnRules = []

        // Configuración de formatos
        const columnFormatRules = {
            // Porcentajes
            '% Desc.': { type: 'percentage', decimals: 2 },
            Porcentaje: { type: 'percentage', decimals: 2 },

            // Unidades de peso
            Libras: { type: 'weight', unit: 'lbs', decimals: 0 },
            Quintales: { type: 'weight', unit: 'qq', decimals: 2 },
            Kg: { type: 'weight', unit: 'kg', decimals: 0 },
            Ton: { type: 'weight', unit: 'ton', decimals: 3 },

            // Campos especiales
            Guía: { type: 'text', format: 'guia' },
            'Guía Alterna': { type: 'text', format: 'guia' },
            Placa: { type: 'text', format: 'placa' },
            'Placa Alterna': { type: 'text', format: 'placa' },

            // Numéricos
            Peso: { type: 'number', format: 'comma', decimals: 2 },
            Faltante: { type: 'number', format: 'comma', decimals: 2 },
            Requerido: { type: 'number', format: 'comma', decimals: 2 },
        }

        // Procesar encabezados
        const headerCells = Array.from(table.querySelectorAll('thead th')).filter(
            cell => isElementVisible(cell) && !cell.classList.contains('no-export'),
        )

        headerCells.forEach(cell => {
            const headerText = cell.textContent.trim()

            // Manejar columnas de conversión
            if (cell.classList.contains('unit-toggle-columns') && isElementVisible(cell)) {
                headers.push(`${headerText} (Libras)`, `${headerText} (Quintales)`)
                columnRules.push({ ...columnFormatRules['Libras'] }, { ...columnFormatRules['Quintales'] })
            } else {
                headers.push(headerText)
                columnRules.push(getFormatRule(headerText, columnFormatRules))
            }
        })

        data.push(headers)

        // Procesar filas del cuerpo
        const bodyRows = table.querySelectorAll('tbody tr')
        bodyRows.forEach(row => {
            const rowData = []
            const cells = Array.from(row.querySelectorAll('td')).filter(
                cell => isElementVisible(cell) && !cell.closest('.no-export'),
            )

            cells.forEach((cell, index) => {
                const headerRule = columnRules[index]
                let cellValue = cell.textContent.trim()

                // Manejar celdas especiales
                if (cell.classList.contains('unit-toggle-columns')) {
                    const lbs = cell.querySelector('.top-value')?.textContent.trim() || '0'
                    const qq = cell.querySelector('.bottom-value')?.textContent.trim() || '0'

                    rowData.push(applyNumberFormat(lbs, headerRule), applyNumberFormat(qq, columnRules[index + 1]))
                    return
                }

                // Manejar campos con valores alternos
                if (cell.querySelector('.data-alt-guia') || cell.querySelector('.data-alt-placa')) {
                    const main = cell.querySelector('div > span')?.textContent.trim() || ''
                    const alt = cell.querySelector('.data-alt-guia, .data-alt-placa')?.textContent.trim() || ''

                    rowData.push(main)
                    if (headerRule?.type === 'text') {
                        rowData.push(alt || '-')
                    }
                    return
                }

                // Aplicar formato según tipo
                if (headerRule) {
                    switch (headerRule.type) {
                        case 'percentage':
                            rowData.push(extraerNumero(cellValue) / 100)
                            break

                        case 'weight':
                        case 'number':
                            rowData.push(applyNumberFormat(cellValue, headerRule))
                            break

                        default:
                            rowData.push(cellValue)
                    }
                } else {
                    rowData.push(cellValue)
                }
            })

            data.push(rowData)
        })

        // Procesar totales
        const footerRows = table.querySelectorAll('tfoot tr')
        footerRows.forEach(row => {
            if (row.classList.contains('no-export')) return

            const rowData = []
            const cells = Array.from(row.querySelectorAll('td')).filter(
                cell => isElementVisible(cell) && !cell.closest('.no-export'),
            )

            cells.forEach((cell, index) => {
                const headerRule = columnRules[index]
                const cellValue = cell.textContent.trim()

                if (headerRule?.type === 'percentage') {
                    rowData.push(extraerNumero(cellValue) / 100)
                } else if (headerRule?.type === 'number' || headerRule?.type === 'weight') {
                    rowData.push(applyNumberFormat(cellValue, headerRule))
                } else {
                    rowData.push(cellValue)
                }
            })

            data.push(rowData)
        })

        // Crear hoja de cálculo
        const ws = XLSX.utils.aoa_to_sheet(data)

        // Aplicar formatos
        data.forEach((row, rowIndex) => {
            row.forEach((value, colIndex) => {
                const cellRef = XLSX.utils.encode_cell({ r: rowIndex, c: colIndex })
                formatExcelCell(ws, cellRef, value, headers[colIndex], columnRules[colIndex])
            })
        })

        // Configurar anchos de columna
        ws['!cols'] = calculateColumnWidths(headers, columnRules).map(w => ({ wch: w }))

        XLSX.utils.book_append_sheet(wb, ws, 'Datos')
        XLSX.writeFile(wb, `${filename}_${new Date().toISOString().slice(0, 10)}.xlsx`)
    } catch (error) {
        console.error('Error en exportación:', error)
        alert(`Error al exportar: ${error.message}`)
    }
}

// Funciones auxiliares
function isElementVisible(el) {
    if (!el) return false
    let current = el
    while (current) {
        if (getComputedStyle(current).display === 'none') return false
        current = current.parentElement
    }
    return el.offsetWidth > 0 && el.offsetHeight > 0
}

function getFormatRule(headerText, rules) {
    const match = Object.entries(rules).find(([key]) => headerText.toLowerCase().includes(key.toLowerCase()))
    return match ? match[1] : null
}

function applyNumberFormat(value, rule) {
    const number = extraerNumero(value)
    if (!rule) return number

    return parseFloat(number.toFixed(rule.decimals))
}

function formatExcelCell(ws, cellRef, value, header, rule) {
    if (typeof value !== 'number' || !rule) return

    ws[cellRef].t = 'n'

    switch (rule.type) {
        case 'percentage':
            ws[cellRef].z = '0.00%'
            break

        case 'weight':
            ws[cellRef].z = rule.decimals > 0 ? `#,##0.${'0'.repeat(rule.decimals)}` : '#,##0'
            break

        case 'number':
            ws[cellRef].z = rule.format === 'comma' ? '#,##0.00' : '0.00'
            break
    }
}

function calculateColumnWidths(headers, rules) {
    return headers.map((header, index) => {
        const rule = rules[index]
        if (rule) {
            switch (rule.type) {
                case 'percentage':
                    return 10
                case 'weight':
                    return rule.unit === 'qq' ? 14 : 12
                case 'number':
                    return 15
                case 'text':
                    return 20
            }
        }
        return Math.min(Math.max(header.length * 1.3, 12), 25)
    })
}

// Función mejorada de extracción numérica
function extraerNumero(texto) {
    const cleanText = texto
        .toString()
        .replace(/[^\d.,\-()]/g, '')
        .replace(/\s/g, '')
        .replace(/\((.*)\)/, '-$1')
        .replace(/(\d)\.(?=\d{3})/g, '$1')

    const isEuropean = /,\d+$/.test(cleanText)
    const parts = cleanText.split(isEuropean ? /[.,]/ : /[,.]/)

    let integer = parts[0].replace(/\D/g, '')
    let decimal = parts[1] || ''

    if (isEuropean) {
        decimal = parts[1] || ''
        integer = integer.replace(/\./g, '')
    }

    const number = parseFloat(`${integer}.${decimal}`)
    return cleanText.startsWith('-') ? -number : number
}
function calcColumnWidths(data) {
    const colWidths = data[0].map(header => Math.max(12, header.length * 1.2))

    for (let rowIdx = 1; rowIdx < data.length; rowIdx++) {
        const row = data[rowIdx]
        for (let colIdx = 0; colIdx < row.length; colIdx++) {
            const cellValue = row[colIdx]
            if (cellValue === undefined || cellValue === null || cellValue === '') continue

            let displayWidth

            if (typeof cellValue === 'number') {
                const numStr = cellValue.toLocaleString('es-GT')
                displayWidth = numStr.length + 2
            } else {
                displayWidth = String(cellValue).length
            }

            colWidths[colIdx] = Math.max(colWidths[colIdx], displayWidth)
        }
    }

    return colWidths.map((width, idx) => {
        const header = data[0][idx] || ''

        if (header.includes('Bodega')) {
            return { wch: Math.max(width, 15) }
        } else if (header.includes('Guía') || header.includes('Placa')) {
            return { wch: Math.max(width, 15) }
        } else if (header.includes('Porcentaje')) {
            return { wch: Math.min(Math.max(width, 10), 12) }
        } else if (header.includes('Libras')) {
            return { wch: Math.min(Math.max(width, 12), 16) }
        } else if (header.includes('Quintales')) {
            return { wch: Math.min(Math.max(width, 10), 14) }
        } else if (header.includes('Peso')) {
            return { wch: Math.min(Math.max(width, 12), 16) }
        } else if (header.includes('Esc')) {
            return { wch: Math.min(Math.max(width, 6), 8) }
        } else if (header === '#') {
            return { wch: 5 }
        }

        return { wch: Math.min(Math.max(width, 8), 20) }
    })
}

function calcResumenColumnWidths(data, columnTypes) {
    const colWidths = data[0].map(header => Math.max(12, header.length * 1.2))

    for (let rowIdx = 1; rowIdx < data.length; rowIdx++) {
        const row = data[rowIdx]
        for (let colIdx = 0; colIdx < row.length; colIdx++) {
            const cellValue = row[colIdx]
            if (cellValue === undefined || cellValue === null || cellValue === '') continue

            let displayWidth

            if (typeof cellValue === 'number') {
                let numStr
                const colType = columnTypes[colIdx] || ''

                if (colType === 'percentage') {
                    numStr = (cellValue * 100).toFixed(2) + '%'
                } else if (colType === 'quintales') {
                    numStr = cellValue.toLocaleString('es-GT', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
                } else if (colType === 'libras') {
                    numStr = cellValue.toLocaleString('es-GT', { maximumFractionDigits: 0 })
                } else {
                    numStr = cellValue.toLocaleString('es-GT')
                }

                displayWidth = numStr.length + 2
            } else {
                displayWidth = String(cellValue).length
            }

            colWidths[colIdx] = Math.max(colWidths[colIdx], displayWidth)
        }
    }

    return colWidths.map((width, idx) => {
        const header = data[0][idx] || ''
        const colType = columnTypes[idx] || ''

        if (header.includes('Empresa')) {
            return { wch: Math.max(width, 25) }
        } else if (colType === 'percentage') {
            return { wch: Math.min(Math.max(width, 10), 12) }
        } else if (colType === 'quintales') {
            return { wch: Math.min(Math.max(width, 10), 14) }
        } else if (colType === 'libras') {
            return { wch: Math.min(Math.max(width, 12), 16) }
        } else if (colType === 'weight') {
            return { wch: Math.min(Math.max(width, 12), 16) }
        } else if (colType === 'text') {
            return { wch: Math.min(Math.max(width, 8), 30) }
        }

        return { wch: Math.min(Math.max(width, 8), 20) }
    })
}

// Function to determine if an element should be considered for export
function shouldExportColumn(headerText) {
    // Columns that should always be included in export
    const criticalColumns = [
        'Empresa', 'Bodega', 'Guía', 'Camión', 'Placa', 
        'Req.', 'Ent.', 'Falt.', '% Desc.',
        'Peso', 'Peso Kg', 'Peso Ton'
    ];
    
    // Check if the header contains any of the critical column names
    return criticalColumns.some(name => headerText.includes(name));
}

// Función mejorada de visibilidad que también incluye la lógica de exportación
function isElementVisible(el) {
    if (!el) return false

    // Verificar cadena completa de elementos padres
    let current = el
    while (current) {
        if (getComputedStyle(current).display === 'none') return false
        current = current.parentElement
    }

    return el.offsetWidth > 0 && el.offsetHeight > 0 && getComputedStyle(el).visibility === 'visible'
}

// Actualizar todas las verificaciones de visibilidad
const isHidden = !isElementVisible(cell)

// Función para calcular el ancho de las columnas basado en el encabezado
function calculateHeaderWidth(headerText) {
    const defaultWidths = {
        'Empresa': 25,
        'Guía': 18,
        'Placa': 12,
        '% Desc.': 10,
        'Falt. (Kg)': 10,
        'Falt. (Ton)': 12,
        'Desc. Qq / Lbs (Quintales)': 18,
        'Falt. Qq / Lbs (Libras)': 16
    };
    return defaultWidths[headerText] || Math.min(Math.max(headerText.length * 1.2, 12), 20);
}

// Function to determine if the unit toggle is showing alternative units (libras/quintales)
function isUnitToggleVisible() {
    // Check if any unit-toggle column is visible
    const visibleToggleColumns = document.querySelector('.unit-toggle-columns.visible') !== null;
    
    // Or check if we've stored a preference for alternative units
    const showingMetricSetting = localStorage.getItem('showingMetric');
    const usingAlternativeUnits = showingMetricSetting === 'false';
    
    // Return true if either condition is met
    return visibleToggleColumns || usingAlternativeUnits;
}

function formatExcelCell(ws, cellRef, value, headerText) {
    const rule = getFormatRule(headerText)

    if (!rule || typeof value !== 'number') return

    switch (rule.type) {
        case 'percentage':
            ws[cellRef].z = '0.00%'
            ws[cellRef].v = value / 100
            break

        case 'weight':
            ws[cellRef].z = rule.decimals > 0 ? `#,##0.${'0'.repeat(rule.decimals)}` : '#,##0'
            break

        case 'currency':
            ws[cellRef].z = `"${rule.symbol}"#,##0.${'0'.repeat(rule.decimals)}`
            break

        case 'number':
            ws[cellRef].z = rule.format === 'comma' ? '#,##0' : '0.00'
            break

        default:
            ws[cellRef].z = '#,##0.00'
    }
}

function getFormatRule(headerText) {
    return Object.entries(columnFormatRules).find(([key]) => headerText.includes(key))?.[1]
}

function exportResumenAgregadoToExcel(tableId, filename) {
    const table = document.getElementById(tableId)
    if (!table) {
        alert('Tabla no encontrada')
        return
    }

    try {
        const wb = XLSX.utils.book_new()
        
        // Get visible columns and their header texts
        const headerCells = table.querySelectorAll('thead th')
        const columns = Array.from(headerCells).filter(cell => {
            return isElementVisible(cell) && 
                  !cell.classList.contains('no-export') && 
                  !cell.textContent.includes('Acciones')
        })
        
        const headers = []
        const columnTypes = []
        
        // Process headers, correctly handling unit-toggle columns that contain both libras and quintales
        columns.forEach(col => {
            const text = col.textContent.trim()
            
            if (col.classList.contains('unit-toggle-columns')) {
                // For dual-value columns, add both libras and quintales headers
                headers.push(`${text} (Libras)`, `${text} (Quintales)`)
                columnTypes.push('libras', 'quintales')
            } else {
                headers.push(text)
                if (text.includes('% Desc.') || text.includes('Porcentaje')) 
                    columnTypes.push('percentage')
                else if (text.includes('Peso') || text.includes('Kg') || text.includes('Ton')) 
                    columnTypes.push('weight')
                else
                    columnTypes.push('text')
            }
        })
        
        const data = [headers]
        
        // Process body rows
        const bodyRows = table.querySelectorAll('tbody tr')
        bodyRows.forEach(row => {
            if (row.classList.contains('no-export')) return
            
            const cells = row.querySelectorAll('td')
            if (cells.length === 0) return
            
            const rowData = []
            columns.forEach((col, idx) => {
                const cellIdx = Array.from(headerCells).indexOf(col)
                if (cellIdx >= 0 && cellIdx < cells.length) {
                    const cell = cells[cellIdx]
                    if (!isElementVisible(cell)) {
                        // If it's a dual column, add empty values for both
                        if (col.classList.contains('unit-toggle-columns')) {
                            rowData.push('', '')
                        } else {
                            rowData.push('')
                        }
                        return
                    }
                    
                    // Handle unit-toggle columns with dual values
                    if (col.classList.contains('unit-toggle-columns')) {
                        // Extract both the libras and quintales values
                        const lbsValue = cell.querySelector('.top-value')?.textContent.trim() || ''
                        const qqValue = cell.querySelector('.bottom-value')?.textContent.trim() || ''
                        
                        // IMPORTANT: Use the improved number extraction for large numbers
                        rowData.push(extraerNumero(lbsValue), extraerNumero(qqValue))
                    } else {
                        // Handle other columns
                        const type = columnTypes[rowData.length] // Use current length as index
                        let value = cell.textContent.trim()
                        
                        if (type === 'percentage') {
                            rowData.push(extraerNumero(value) / 100)
                        }
                        else if (type === 'weight' || type === 'libras') {
                            // IMPORTANT: For weight columns, ensure correct number parsing
                            rowData.push(extraerNumero(value))
                        }
                        else {
                            rowData.push(value)
                        }
                    }
                } else {
                    // If it's a dual column, add empty values for both
                    if (col.classList.contains('unit-toggle-columns')) {
                        rowData.push('', '')
                    } else {
                        rowData.push('')
                    }
                }
            })
            
            data.push(rowData)
        })
        
        // Process footer rows (totals)
        const footerRows = table.querySelectorAll('tfoot tr')
        footerRows.forEach(footerRow => {
            if (footerRow.classList.contains('no-export') || 
                footerRow.classList.contains('table-purple')) {
                return
            }
            
            const rowData = []
            columns.forEach((col, idx) => {
                const cellIdx = Array.from(headerCells).indexOf(col)
                if (cellIdx >= 0 && cellIdx < footerRow.cells.length) {
                    const cell = footerRow.cells[cellIdx]
                    if (!isElementVisible(cell)) {
                        // If it's a dual column, add empty values for both
                        if (col.classList.contains('unit-toggle-columns')) {
                            rowData.push('', '')
                        } else {
                            rowData.push('')
                        }
                        return
                    }
                    
                    // Handle unit-toggle columns with dual values in footer
                    if (col.classList.contains('unit-toggle-columns')) {
                        const lbsValue = cell.querySelector('.top-value')?.textContent.trim() || ''
                        const qqValue = cell.querySelector('.bottom-value')?.textContent.trim() || ''
                        
                        rowData.push(extraerNumero(lbsValue), extraerNumero(qqValue))
                    } else {
                        const type = columnTypes[rowData.length] // Use current length as index
                        let value = cell.textContent.trim()
                        
                        if (type === 'percentage') {
                            rowData.push(extraerNumero(value) / 100)
                        }
                        else if (type === 'libras' || type === 'quintales' || type === 'weight') {
                            rowData.push(extraerNumero(value))
                        }
                        else {
                            rowData.push(value)
                        }
                    }
                } else {
                    // If it's a dual column, add empty values for both
                    if (col.classList.contains('unit-toggle-columns')) {
                        rowData.push('', '')
                    } else {
                        rowData.push('')
                    }
                }
            })
            
            data.push(rowData)
        })
        
        const ws = XLSX.utils.aoa_to_sheet(data)
        
        // Format cells with explicit number formats to ensure proper display
        data.forEach((row, rowIndex) => {
            row.forEach((value, colIndex) => {
                if (rowIndex === 0) return // Skip headers
                
                const cellRef = XLSX.utils.encode_cell({ r: rowIndex, c: colIndex })
                if (!ws[cellRef]) return
                
                const headerText = data[0][colIndex]
                const tipo = columnTypes[colIndex]
                
                if (typeof value === 'number') {
                    ws[cellRef].t = 'n'
                    
                    // IMPROVED: Format numbers in a way that Excel will display correctly
                    if (tipo === 'percentage') {
                        ws[cellRef].z = '0.00%'
                    } else if (tipo === 'quintales') {
                        ws[cellRef].z = '#,##0.00' // Format with 2 decimals
                    } else if (tipo === 'libras') {
                        // CRITICAL FIX FOR LARGE NUMBERS:
                        ws[cellRef].z = '#,##0'    // No decimals for libras
                        // For large numbers, ensure they're stored as integers
                        if (value > 1000) {
                            ws[cellRef].v = Math.round(value) // Round to ensure no decimal issues
                        }
                    } else if (tipo === 'weight') {
                        ws[cellRef].z = '#,##0.00' // Format with 2 decimals
                    } else {
                        ws[cellRef].z = '#,##0.00' // Default format
                    }
                }
            })
        })
        
        // Set column widths
        const colWidths = calcResumenColumnWidths(data, columnTypes)
        ws['!cols'] = colWidths
        
        // Force Excel to interpret numbers as numbers with these options
        const workbookOpts = {
            bookType: 'xlsx',
            bookSST: false,
            type: 'binary',
            cellDates: false,
            cellNF: false,
            cellStyles: true
        };
        
        XLSX.utils.book_append_sheet(wb, ws, 'Resumen')
        
        const dateStr = new Date().toISOString().slice(0, 10)
        XLSX.writeFile(wb, `${filename}_${dateStr}.xlsx`, workbookOpts)
        console.log("Exportación completada con éxito")
    } catch (error) {
        console.error('Error detallado al exportar a Excel:', error)
        console.error('Stack trace:', error.stack)
        alert('Error al exportar a Excel: ' + error.message)
    }
}

// Also update the standard exportTableToExcel function for consistency
function exportTableToExcel(tableId, filename) {
    const table = document.getElementById(tableId)
    if (!table) {
        alert('Tabla no encontrada')
        return
    }

    try {
        const wb = XLSX.utils.book_new()
        const data = []
        const headers = []
        const columnRules = []

        // Configuración de formatos
        const columnFormatRules = {
            // Porcentajes
            '% Desc.': { type: 'percentage', decimals: 2 },
            Porcentaje: { type: 'percentage', decimals: 2 },

            // Unidades de peso
            Libras: { type: 'weight', unit: 'lbs', decimals: 0 },
            Quintales: { type: 'weight', unit: 'qq', decimals: 2 },
            Kg: { type: 'weight', unit: 'kg', decimals: 0 },
            Ton: { type: 'weight', unit: 'ton', decimals: 3 },

            // Campos especiales
            Guía: { type: 'text', format: 'guia' },
            'Guía Alterna': { type: 'text', format: 'guia' },
            Placa: { type: 'text', format: 'placa' },
            'Placa Alterna': { type: 'text', format: 'placa' },

            // Numéricos
            Peso: { type: 'number', format: 'comma', decimals: 2 },
            Faltante: { type: 'number', format: 'comma', decimals: 2 },
            Requerido: { type: 'number', format: 'comma', decimals: 2 },
        }

        // Procesar encabezados
        const headerCells = Array.from(table.querySelectorAll('thead th')).filter(
            cell => isElementVisible(cell) && !cell.classList.contains('no-export'),
        )

        headerCells.forEach(cell => {
            const headerText = cell.textContent.trim()

            // Manejar columnas de conversión
            if (cell.classList.contains('unit-toggle-columns') && isElementVisible(cell)) {
                headers.push(`${headerText} (Libras)`, `${headerText} (Quintales)`)
                columnRules.push({ ...columnFormatRules['Libras'] }, { ...columnFormatRules['Quintales'] })
            } else {
                headers.push(headerText)
                columnRules.push(getFormatRule(headerText, columnFormatRules))
            }
        })

        data.push(headers)

        // Procesar filas del cuerpo
        const bodyRows = table.querySelectorAll('tbody tr')
        bodyRows.forEach(row => {
            const rowData = []
            const cells = Array.from(row.querySelectorAll('td')).filter(
                cell => isElementVisible(cell) && !cell.closest('.no-export'),
            )

            cells.forEach((cell, index) => {
                const headerRule = columnRules[index]
                let cellValue = cell.textContent.trim()

                // Manejar celdas especiales
                if (cell.classList.contains('unit-toggle-columns')) {
                    const lbs = cell.querySelector('.top-value')?.textContent.trim() || '0'
                    const qq = cell.querySelector('.bottom-value')?.textContent.trim() || '0'

                    rowData.push(applyNumberFormat(lbs, headerRule), applyNumberFormat(qq, columnRules[index + 1]))
                    return
                }

                // Manejar campos con valores alternos
                if (cell.querySelector('.data-alt-guia') || cell.querySelector('.data-alt-placa')) {
                    const main = cell.querySelector('div > span')?.textContent.trim() || ''
                    const alt = cell.querySelector('.data-alt-guia, .data-alt-placa')?.textContent.trim() || ''

                    rowData.push(main)
                    if (headerRule?.type === 'text') {
                        rowData.push(alt || '-')
                    }
                    return
                }

                // Aplicar formato según tipo
                if (headerRule) {
                    switch (headerRule.type) {
                        case 'percentage':
                            rowData.push(extraerNumero(cellValue) / 100)
                            break

                        case 'weight':
                        case 'number':
                            rowData.push(applyNumberFormat(cellValue, headerRule))
                            break

                        default:
                            rowData.push(cellValue)
                    }
                } else {
                    rowData.push(cellValue)
                }
            })

            data.push(rowData)
        })

        // Procesar totales
        const footerRows = table.querySelectorAll('tfoot tr')
        footerRows.forEach(row => {
            if (row.classList.contains('no-export')) return

            const rowData = []
            const cells = Array.from(row.querySelectorAll('td')).filter(
                cell => isElementVisible(cell) && !cell.closest('.no-export'),
            )

            cells.forEach((cell, index) => {
                const headerRule = columnRules[index]
                const cellValue = cell.textContent.trim()

                if (headerRule?.type === 'percentage') {
                    rowData.push(extraerNumero(cellValue) / 100)
                } else if (headerRule?.type === 'number' || headerRule?.type === 'weight') {
                    rowData.push(applyNumberFormat(cellValue, headerRule))
                } else {
                    rowData.push(cellValue)
                }
            })

            data.push(rowData)
        })

        // Crear hoja de cálculo
        const ws = XLSX.utils.aoa_to_sheet(data)

        // Aplicar formatos
        data.forEach((row, rowIndex) => {
            row.forEach((value, colIndex) => {
                const cellRef = XLSX.utils.encode_cell({ r: rowIndex, c: colIndex })
                formatExcelCell(ws, cellRef, value, headers[colIndex], columnRules[colIndex])
            })
        })

        // Configurar anchos de columna
        ws['!cols'] = calculateColumnWidths(headers, columnRules).map(w => ({ wch: w }))

        // Add workbook options to ensure consistent decimal handling
        const workbookOpts = {
            bookType: 'xlsx',
            bookSST: false,
            type: 'binary'
        };

        XLSX.utils.book_append_sheet(wb, ws, 'Datos')
        XLSX.writeFile(wb, `${filename}_${new Date().toISOString().slice(0, 10)}.xlsx`, workbookOpts)
    } catch (error) {
        console.error('Error en exportación:', error)
        alert(`Error al exportar: ${error.message}`)
    }
}
