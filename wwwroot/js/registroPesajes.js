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
        error: function (xhr, status, error) {
            console.error('Error cargando importaciones: ' + error)
        },
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
        error: function (xhr, status, error) {
            console.error('Error cargando empresas: ' + error)
        },
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
    console.log('Initializing with barco:', selectedBarco, 'empresa:', empresaId)

    window.selectedBarcoId = selectedBarco
    window.selectedEmpresaId = empresaId

    console.log('Barcos dropdown options:', $('#selectedBarco option').length)
    console.log('Empresas dropdown options:', $('#empresaId option').length)

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

function exportTableToExcel(tableId, filename = '') {
    const table = document.getElementById(tableId)
    if (!table) {
        console.error(`Table with ID '${tableId}' not found`)
        return
    }

    if (typeof XLSX === 'undefined') {
        const script = document.createElement('script')
        script.src = 'https://cdn.sheetjs.com/xlsx-0.20.0/package/dist/xlsx.full.min.js'
        script.onload = function () {
            exportTableToExcelWithXLSX(tableId, filename)
        }
        document.head.appendChild(script)
        return
    }

    exportTableToExcelWithXLSX(tableId, filename)
}

function exportTableToExcelWithXLSX(tableId, filename = '') {
    const table = document.getElementById(tableId)
    if (!table) {
        console.error(`Table with ID '${tableId}' not found`)
        return
    }

    const wb = XLSX.utils.book_new()
    const ws_data = []

    const headerRow = []
    const headerCells = table.querySelectorAll('thead tr:first-child th')
    headerCells.forEach(cell => {
        if (!cell.classList.contains('unit-toggle-columns')) {
            headerRow.push(cell.innerText.trim())
        }
    })
    ws_data.push(headerRow)

    const rows = table.querySelectorAll('tbody tr')
    rows.forEach(row => {
        const rowData = []
        const cells = row.querySelectorAll('td')

        rowData.push(cells[0].innerText.trim())
        rowData.push(cells[1].innerText.trim())
        rowData.push(cells[2].innerText.trim())
        rowData.push(cells[3].innerText.trim())
        rowData.push(cells[5].innerText.trim())
        rowData.push(cells[6].innerText.trim())
        rowData.push(cells[8].innerText.trim())
        rowData.push(cells[9].innerText.trim())
        rowData.push(cells[10].innerText.trim())

        ws_data.push(rowData)
    })

    const footerCells = table.querySelectorAll('tfoot tr:first-child td')
    if (footerCells.length > 0) {
        const footerRow = [
            footerCells[0]?.innerText.trim() || '',
            '',
            '',
            '',
            footerCells[4]?.innerText.trim() || '',
            footerCells[5]?.innerText.trim() || '',
            footerCells[6]?.innerText.trim() || '',
            footerCells[8]?.innerText.trim() || '',
            footerCells[9]?.innerText.trim() || '',
        ]
        ws_data.push(footerRow)
    }

    const ws = XLSX.utils.aoa_to_sheet(ws_data)

    for (let i = 1; i < ws_data.length; i++) {
        for (let j = 0; j < ws_data[i].length; j++) {
            const cellRef = XLSX.utils.encode_cell({ r: i, c: j })
            let cellValue = ws_data[i][j]

            if (j === 0) continue

            if (typeof cellValue === 'string') {
                const isPercentage = cellValue.includes('%')
                let rawValue = cellValue
                    .replace(/[^\d.,\-]/g, '')
                    .replace(/\./g, '')
                    .replace(',', '.')

                if (!isNaN(rawValue) && rawValue.length > 0) {
                    const numValue = parseFloat(rawValue)
                    ws[cellRef] = {
                        v: numValue,
                        t: 'n',
                        z: isPercentage ? '0.00%' : j === 2 || j === 5 ? '0.00' : j === 6 ? '0.00' : '0',
                    }

                    if (isPercentage) {
                        ws[cellRef].v = numValue / 100
                    }
                }
            }
        }
    }

    ws['!cols'] = [
        { width: 30 }, // Empresa
        { width: 12 }, // Req. (Kg)
        { width: 10 }, // Req. (Ton)
        { width: 12 }, // Desc. (Kg)
        { width: 12 }, // Falt. (Kg)
        { width: 10 }, // Falt. (Ton)
        { width: 10 }, // Cam. Falt.
        { width: 8 }, // Placas
        { width: 10 }, // % Desc.
    ]

    XLSX.utils.book_append_sheet(wb, ws, 'Resumen Agregado')
    XLSX.writeFile(wb, filename + '.xlsx')
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

    headerCells.forEach((cell, index) => {
        const headerText = cell.innerText.trim()

        if (headerText === 'Acciones') {
            excludeColumnIndices.push(index)
            columnMapping[index] = -1
            return
        }

        if (cell.classList.contains('unit-toggle-columns')) {
            const headerName =
                cell.querySelector('div:first-child')?.textContent.trim() || headerText.split('\n')[0].trim()

            headerRow.push(`${headerName} (Libras)`)
            headerRow.push(`${headerName} (Quintales)`)

            columnMapping[index] = [outputColIndex, outputColIndex + 1]
            outputColIndex += 2
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
            const topValue = cell.querySelector('.top-value')?.textContent || ''
            const bottomValue = cell.querySelector('.bottom-value')?.textContent || ''

            const [lbsIndex, qqIndex] = Array.isArray(columnMapping[colIndex])
                ? columnMapping[colIndex]
                : [columnMapping[colIndex], columnMapping[colIndex] + 1]

            const lbsValue = topValue.replace(/[^\d.,\-]/g, '').trim()
            rowData[lbsIndex] = lbsValue

            const qqValue = bottomValue.replace(/[^\d.,\-]/g, '').trim()
            rowData[qqIndex] = qqValue
        } else if (colspan > 1) {
            rowData[columnMapping[colIndex]] = cell.innerText.trim()
        } else {
            if (columnMapping[colIndex] !== undefined && columnMapping[colIndex] !== -1) {
                rowData[columnMapping[colIndex]] = cell.innerText.trim()
            }
        }

        colIndex += colspan
    }

    return rowData
}

function formatNumbersInExcelSheet(worksheet, data) {
    for (let i = 1; i < data.length; i++) {
        for (let j = 0; j < data[i].length; j++) {
            const cellAddress = XLSX.utils.encode_cell({ r: i, c: j })
            const cellValue = data[i][j]

            if (/^-?[\d.,]+$/.test(cellValue)) {
                const numValue = parseFloat(cellValue.replace(/,/g, ''))
                if (!isNaN(numValue)) {
                    worksheet[cellAddress] = { v: numValue, t: 'n' }
                }
            }
        }
    }
}

function calculateColumnWidths(data) {
    const colWidths = data.reduce((acc, row) => {
        for (let i = 0; i < row.length; i++) {
            const cellValue = row[i] || ''
            acc[i] = Math.max(acc[i] || 8, cellValue.toString().length + 2)
        }
        return acc
    }, [])

    return colWidths.map(w => ({ width: w }))
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
    addConversionColumns()
}

function addConversionColumns() {
    if ($('.unit-toggle-columns').length > 0) {
        return
    }

    const tabla2 = $('#tabla2')
    if (tabla2.length === 0) return

    $('.unit-toggle-columns').hide()
}

let showingMetric = true
function setupUnitToggle() {
    $('#btnToggleUnidad').off('click')

    const selectedBarco = $('select[name="selectedBarco"]').val() || $('#selectImportacion').val()
    const hasData = hasTableData('tabla2')
    $('#btnToggleUnidad').prop('disabled', !selectedBarco || !hasData)

    $('#btnToggleUnidad').on('click', function () {
        if ($(this).prop('disabled')) return

        showingMetric = !showingMetric

        const buttonText = $(this).find('span')
        buttonText.text(showingMetric ? 'Unidades' : 'Métrico')
        $(this).attr('title', showingMetric ? 'Mostrar en Libras' : 'Mostrar en Kilogramos')
        $(this).find('i').toggleClass('fa-weight-scale fa-balance-scale')

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

            adjustTableLayout()
        }, 50)
    })

    document.addEventListener('dataLoaded', function () {
        const selectedBarco = $('select[name="selectedBarco"]').val() || $('#selectImportacion').val()
        const hasData = hasTableData('tabla2')
        $('#btnToggleUnidad').prop('disabled', !selectedBarco || !hasData)
    })
}

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
    console.log('Barco changed to:', selectElement.value)

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
    console.log('Empresa changed to:', selectElement.value)

    $('#empresaLoadingIndicator').removeClass('d-none')

    setTimeout(() => {
        $('#selectionForm').submit()
    }, 100)
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
        updateExcelButtonState()
    })

    setupEscotillasToggle()

    if (typeof initializeServerData === 'function') {
        initializeServerData('@Context.Request.Query["selectedBarco"]', '@Context.Request.Query["empresaId"]')
    }

    updateExcelButtonState()

    $('select[name="selectedBarco"]').on('change', function () {
        updateExcelButtonState()
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
            console.log('Botón de exportación de registros individuales clickeado')

            if (typeof XLSX === 'undefined') {
                alert('La librería XLSX no está cargada correctamente')
                return
            }

            const tabla = document.getElementById('tabla1')
            if (!tabla) {
                alert('No se encontró la tabla de registros individuales')
                return
            }

            const wb = XLSX.utils.book_new()
            const ws_data = []

            const headerRow = []
            const headers = tabla.querySelectorAll('thead th')
            headers.forEach(header => {
                if (!header.classList.contains('unit-toggle-columns') && header.innerText.trim() !== 'Acciones') {
                    headerRow.push(header.innerText.trim())
                }
            })
            ws_data.push(headerRow)

            const rows = tabla.querySelectorAll('tbody tr')
            if (
                rows.length === 0 ||
                (rows.length === 1 && rows[0].querySelector('td').innerText.includes('No hay registros'))
            ) {
                alert('No hay datos para exportar')
                return
            }

            rows.forEach(row => {
                const cells = row.querySelectorAll('td')
                if (cells.length > 1) {
                    const rowData = []

                    rowData.push(cells[0].innerText.trim())
                    rowData.push(cells[1].innerText.trim())
                    rowData.push(cells[2].innerText.trim())
                    rowData.push(cells[3].innerText.trim())
                    rowData.push(cells[4].innerText.trim())
                    rowData.push(cells[5].innerText.trim())
                    rowData.push(cells[6].innerText.trim())
                    rowData.push(cells[8].innerText.trim())

                    rowData.push(cells[9].innerText.trim())

                    ws_data.push(rowData)
                }
            })

            const ws = XLSX.utils.aoa_to_sheet(ws_data)

            for (let i = 1; i < ws_data.length; i++) {
                for (let j = 5; j < 9; j++) {
                    const cellRef = XLSX.utils.encode_cell({ r: i, c: j })
                    if (ws[cellRef]) {
                        let value = ws_data[i][j]
                        if (typeof value === 'string') {
                            value = value
                                .replace(/[^\d.,\-]/g, '')
                                .replace(/\./g, '')
                                .replace(',', '.')

                            if (!isNaN(value) && value.length > 0) {
                                ws[cellRef] = {
                                    v: parseFloat(value),
                                    t: 'n',
                                    z: j === 8 ? '0.00%' : '0',
                                }

                                if (j === 8) {
                                    ws[cellRef].v = parseFloat(value) / 100
                                }
                            }
                        }
                    }
                }
            }

            ws['!cols'] = [
                { width: 15 }, // Bodega
                { width: 12 }, // Guía
                { width: 12 }, // Guía Alterna
                { width: 12 }, // Placa
                { width: 12 }, // Placa Alterna
                { width: 15 }, // Peso Requerido
                { width: 15 }, // Peso Entregado
                { width: 15 }, // Peso Faltante
                { width: 12 }, // Porcentaje
            ]

            XLSX.utils.book_append_sheet(wb, ws, 'Registros Individuales')
            XLSX.writeFile(wb, 'Registros_Individuales_' + new Date().toISOString().slice(0, 10) + '.xlsx')
        })

    updateExcelButtonState()

    $('#selectImportacion, select[name="selectedBarco"], select[name="empresaId"]').on('change', function () {
        updateExcelButtonState()
    })

    function adjustNoteWidth() {
        $('.table-scroll').each(function () {
            var $tableContainer = $(this)
            var $table = $tableContainer.find('table')
            var $note = $tableContainer.find('.border-top.text-center')

            if ($table.length && $note.length) {
                $note.width($table.outerWidth())
                console.log('Note width adjusted to: ' + $table.outerWidth() + 'px')
            }
        })
    }

    adjustNoteWidth()

    $(window).resize(function () {
        adjustNoteWidth()
    })

    $('.table-scroll').each(function () {
        var $container = $(this)
        if ($container[0].scrollWidth > $container.innerWidth()) {
            console.log('Horizontal scrollbar detected')
            $container.addClass('has-h-scroll')
        }
    })

    function adjustNoteWidth() {
        $('.table-scroll').each(function () {
            var $tableContainer = $(this)
            var $table = $tableContainer.find('table')
            var $note = $tableContainer.find('.p-3.bg-light.border-top')

            if ($table.length && $note.length) {
                $note.width($table.width())
                console.log('Note width adjusted to: ' + $table.width() + 'px')
            }
        })
    }

    adjustNoteWidth()

    $(window).resize(function () {
        adjustNoteWidth()
    })

    $('.table-scroll').on('scroll', function () {
        var $tableContainer = $(this)
        var $table = $tableContainer.find('table')
        var $note = $tableContainer.find('.p-3.bg-light.border-top')

        if ($table.length && $note.length) {
            $note.css({
                left: -1 * $(this).scrollLeft() + 'px',
                position: 'relative',
                width: $table.width() + 'px',
            })
        }
    })

    function adjustNoteWidth() {
        $('.table-scroll').each(function () {
            var $table = $(this).find('table')
            var $note = $(this).find('.p-3.bg-light.border-top')

            if ($table.length && $note.length) {
                $note.width($table.width())

                console.log('Table width:', $table.width(), 'Note width:', $note.width())
            }
        })
    }

    adjustNoteWidth()

    $(window).resize(function () {
        adjustNoteWidth()
    })

    $('.table-scroll').on('scroll', function () {
        adjustNoteWidth()
    })

    setNoteWidths()

    $(window).resize(setNoteWidths)

    function setNoteWidths() {
        $('.table-scroll').each(function () {
            const $tableScroll = $(this)
            const $table = $tableScroll.find('table')
            const $note = $tableScroll.find('.p-3.bg-light.border-top.text-center')

            if ($table.length && $note.length) {
                const tableWidth = $table[0].offsetWidth

                $tableScroll.css('--table-width', tableWidth + 'px')

                $note.width(tableWidth)

                console.log(`Table width: ${tableWidth}px, Note width set`)
            }
        })
    }

    $('.table-scroll').on('scroll', function () {
        setNoteWidths()
    })

    $('.table-scroll').off('scroll.notefix')
    $(window).off('resize.notefix')

    function fixNoteWidth() {
        $('.table-scroll').each(function () {
            const $container = $(this)
            const $table = $container.find('table')
            const $note = $container.find('.p-3.bg-light.border-top')

            if ($table.length && $note.length) {
                const tableWidth = $table[0].offsetWidth

                $note.width(tableWidth)
                $note.css({
                    'min-width': tableWidth + 'px',
                    'max-width': tableWidth + 'px',
                })

                console.log(`Fixed note width: ${tableWidth}px`)
            }
        })
    }

    setTimeout(fixNoteWidth, 100)

    $(window).on('resize.notefix', fixNoteWidth)

    $('.table-scroll').on('scroll.notefix', function () {
        fixNoteWidth()
    })

    function updateNoteWidth() {
        $('.table-scroll').each(function () {
            const $table = $(this).find('table')
            const $note = $(this).find('.p-3.bg-light.border-top')

            if ($table.length && $note.length) {
                $note.css('left', '')

                const tableWidth = $table.outerWidth()
                $note.width(tableWidth)
                $note.css('min-width', tableWidth + 'px')
            }
        })
    }

    updateNoteWidth()

    $(window).on('resize', updateNoteWidth)

    if ($('#tabla1 tbody tr').length > 0 && !$('#tabla1 tbody tr:first td:first').text().includes('No hay registros')) {
        $('#btnExportarExcelIndividuales').prop('disabled', false)
    }

    $('#btnExportarExcelIndividuales')
        .off('click')
        .on('click', function () {
            console.log('Botón de exportación de registros individuales clickeado')

            if (typeof XLSX === 'undefined') {
                alert('La librería XLSX no está cargada correctamente')
                return
            }

            const tabla = document.getElementById('tabla1')
            if (!tabla) {
                alert('No se encontró la tabla de registros individuales')
                return
            }

            const wb = XLSX.utils.book_new()
            const ws_data = []

            const headerRow = []
            const headers = tabla.querySelectorAll('thead th')
            headers.forEach(header => {
                if (!header.classList.contains('unit-toggle-columns') && header.innerText.trim() !== 'Acciones') {
                    headerRow.push(header.innerText.trim())
                }
            })
            ws_data.push(headerRow)

            const rows = tabla.querySelectorAll('tbody tr')
            if (
                rows.length === 0 ||
                (rows.length === 1 && rows[0].querySelector('td').innerText.includes('No hay registros'))
            ) {
                alert('No hay datos para exportar')
                return
            }

            rows.forEach(row => {
                const cells = row.querySelectorAll('td')
                if (cells.length > 1) {
                    const rowData = []

                    rowData.push(cells[0].innerText.trim())
                    rowData.push(cells[1].innerText.trim())
                    rowData.push(cells[2].innerText.trim())
                    rowData.push(cells[3].innerText.trim())
                    rowData.push(cells[4].innerText.trim())
                    rowData.push(cells[5].innerText.trim())
                    rowData.push(cells[6].innerText.trim())
                    rowData.push(cells[8].innerText.trim())
                    rowData.push(cells[9].innerText.trim())

                    ws_data.push(rowData)
                }
            })

            const ws = XLSX.utils.aoa_to_sheet(ws_data)

            for (let i = 1; i < ws_data.length; i++) {
                for (let j = 5; j < 9; j++) {
                    const cellRef = XLSX.utils.encode_cell({ r: i, c: j })
                    if (ws[cellRef]) {
                        let value = ws_data[i][j]
                        if (typeof value === 'string') {
                            value = value
                                .replace(/[^\d.,\-]/g, '')
                                .replace(/\./g, '')
                                .replace(',', '.')

                            if (!isNaN(value) && value.length > 0) {
                                ws[cellRef] = {
                                    v: parseFloat(value),
                                    t: 'n',
                                    z: j === 8 ? '0.00%' : '0',
                                }

                                if (j === 8) {
                                    ws[cellRef].v = parseFloat(value) / 100
                                }
                            }
                        }
                    }
                }
            }

            ws['!cols'] = [
                { width: 15 }, // Bodega
                { width: 12 }, // Guía
                { width: 12 }, // Guía Alterna
                { width: 12 }, // Placa
                { width: 12 }, // Placa Alterna
                { width: 15 }, // Peso Requerido
                { width: 15 }, // Peso Entregado
                { width: 15 }, // Peso Faltante
                { width: 12 }, // Porcentaje
            ]

            XLSX.utils.book_append_sheet(wb, ws, 'Registros Individuales')
            XLSX.writeFile(wb, 'Registros_Individuales_' + new Date().toISOString().slice(0, 10) + '.xlsx')
        })

    setTimeout(function () {
        if (
            $('#tabla1 tbody tr').length > 0 &&
            !$('#tabla1 tbody tr:first td:first').text().includes('No hay registros')
        ) {
            $('#btnExportarExcelIndividuales').prop('disabled', false)
        }
    }, 500)
})
