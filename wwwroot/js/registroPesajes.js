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
            // Skip rows with 'table-purple' class (totals row) and 'no-export' class
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
                // Manejamos los campos de guía y placa con sus alternativas
                if (cell.querySelector('.data-alt-guia') || cell.querySelector('.data-alt-placa')) {
                    const mainText = cell.querySelector('div > span')?.textContent.trim() || cell.innerText.trim()
                    rowData[columnMapping[colIndex]] = mainText

                    // Si es la columna de Guía, la siguiente columna es para Guía Alterna
                    if (cell.querySelector('.data-alt-guia')) {
                        const altText = cell.querySelector('.data-alt-guia').textContent.trim()
                        rowData[columnMapping[colIndex] + 1] = altText !== '' ? altText : '-'
                    }
                    // Si es la columna de Placa, la siguiente columna es para Placa Alterna
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

// Versión unificada y mejorada de la función para extraer números
function extraerNumero(texto) {
    if (!texto) return 0

    // Handle European format (1.234,56) by removing dots and replacing comma with decimal point
    let cleaned = texto.toString().replace(/\s/g, '')

    if (cleaned.includes('.') && cleaned.includes(',')) {
        // European format: dots are thousand separators, comma is decimal
        cleaned = cleaned.replace(/\./g, '').replace(',', '.')
    } else if (cleaned.includes('.') && !cleaned.includes(',')) {
        // Format with only dots could be either thousand separators or decimal
        // If there are multiple dots or the dot is not near the end, treat as thousand separators
        const parts = cleaned.split('.')
        if (parts.length > 2 || (parts.length === 2 && parts[1].length !== 2)) {
            cleaned = cleaned.replace(/\./g, '')
        }
    } else if (cleaned.includes(',')) {
        // If only comma, treat as decimal point
        cleaned = cleaned.replace(',', '.')
    }

    // Remove any non-numeric characters except decimal point and minus sign
    cleaned = cleaned.replace(/[^\d.\-]/g, '')

    const value = parseFloat(cleaned)
    return isNaN(value) ? 0 : value
}

function formatNumbersInExcelSheet(worksheet, data) {
    // Get header row to understand column types
    const headers = data[0] || []

    for (let i = 1; i < data.length; i++) {
        for (let j = 0; j < data[i].length; j++) {
            const cellAddress = XLSX.utils.encode_cell({ r: i, c: j })
            const cellValue = data[i][j]

            // Skip empty cells
            if (!cellValue && cellValue !== 0) continue

            // Check header to determine column type
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

            // Convert string to number if it looks like a number
            if (typeof cellValue === 'string' && /^-?[\d.,]+%?$/.test(cellValue.trim())) {
                // Handle percentage values
                if (cellValue.includes('%')) {
                    const numValue = extraerNumero(cellValue) / 100
                    worksheet[cellAddress] = {
                        v: numValue,
                        t: 'n',
                        z: '0.00%',
                    }
                }
                // Handle regular number values
                else {
                    const numValue = extraerNumero(cellValue)
                    worksheet[cellAddress] = {
                        v: numValue,
                        t: 'n',
                        // Format based on column type
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
            // If it's already a number
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

                // Adjust percentage values that were passed as decimals
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

// Versión unificada de la función de exportación
function exportResumenAgregadoToExcel(tableId, filename) {
    const table = document.getElementById(tableId)
    if (!table) {
        console.error('Table not found: ' + tableId)
        return
    }

    try {
        console.log('Exportando tabla de resumen agregado')
        const wb = XLSX.utils.book_new()

        // Extraer los datos de la tabla asegurando consistencia de columnas
        const data = []

        // Procesar encabezados
        const headerRow = []
        const headerCells = table.querySelectorAll('thead th')
        const unitToggleColumns = [] // Índices de columnas con unidades duales

        headerCells.forEach((cell, index) => {
            if (cell.classList.contains('unit-toggle-columns')) {
                const headerName =
                    cell.querySelector('div:first-child')?.textContent.trim() || cell.textContent.split('\n')[0].trim()
                headerRow.push(`${headerName} (Quintales)`)
                headerRow.push(`${headerName} (Libras)`)
                unitToggleColumns.push(index)
            } else if (cell.textContent.includes('Acciones')) {
                // Ignorar columna de acciones
                return
            } else {
                headerRow.push(cell.textContent.trim())
            }
        })

        data.push(headerRow)

        // Función para preservar el texto original del número pero extraer un valor numérico para Excel
        function processNumericText(text) {
            // Guarda el texto original
            const originalText = text.trim()

            // Si el texto está vacío o no es un número, devolverlo tal cual
            if (!originalText || originalText === '-' || isNaN(originalText.replace(/[^\d,.-]/g, ''))) {
                return { text: originalText, value: originalText }
            }

            // Detecta si es un número con formato europeo (1.234,56)
            const isEuropeanFormat = /\d{1,3}(?:\.\d{3})+(?:,\d+)?/.test(originalText)

            let numericValue
            if (isEuropeanFormat) {
                // Convierte formato europeo a número: elimina puntos y reemplaza coma por punto
                numericValue = originalText.replace(/\./g, '').replace(',', '.')
            } else {
                // Asume formato estadounidense o simplemente número con coma decimal
                numericValue = originalText.replace(',', '.')
            }

            // Elimina cualquier carácter no numérico excepto punto decimal y signo negativo
            numericValue = numericValue.replace(/[^\d.-]/g, '')

            return {
                text: originalText,
                value: parseFloat(numericValue),
            }
        }

        // Procesar filas del cuerpo
        const rows = table.querySelectorAll('tbody tr')
        rows.forEach(row => {
            const rowData = []
            const cellValues = [] // Para almacenar los valores numéricos
            const cells = row.querySelectorAll('td')

            cells.forEach((cell, index) => {
                if (unitToggleColumns.includes(index)) {
                    // Para columnas con valores duales (quintales/libras)
                    const qqText = cell.querySelector('.top-value')?.textContent.trim() || ''
                    const lbsText = cell.querySelector('.bottom-value')?.textContent.trim() || ''

                    // Procesar quintales
                    const qqProcessed = processNumericText(qqText.replace('qq', '').trim())
                    rowData.push(qqProcessed.text)
                    cellValues.push(qqProcessed.value)

                    // Procesar libras
                    const lbsProcessed = processNumericText(lbsText.replace('lbs', '').trim())
                    rowData.push(lbsProcessed.text)
                    cellValues.push(lbsProcessed.value)
                } else {
                    // Para celdas normales
                    const processed = processNumericText(cell.textContent)
                    rowData.push(processed.text)
                    cellValues.push(processed.value)
                }
            })

            data.push(rowData)
            // También guardamos los valores numéricos para usarlos más tarde
            row.processedValues = cellValues
        })

        // Crear la hoja de cálculo
        const ws = XLSX.utils.aoa_to_sheet(data)

        // Aplicar formato a números
        for (let r = 1; r < data.length; r++) {
            let cellIndex = 0

            for (let c = 0; c < data[r].length; c++) {
                const cellRef = XLSX.utils.encode_cell({ r, c })
                if (!ws[cellRef]) continue

                const originalText = data[r][c]
                const headerText = data[0][c] || ''

                // Intentar convertir a número
                const numberMatch = originalText.match(/[-+]?[\d.,]+/)
                if (numberMatch) {
                    const numStr = numberMatch[0]
                    // Convertir a número para Excel
                    let numValue

                    // Si tiene puntos como separadores de miles y coma como decimal
                    if (/\d{1,3}(?:\.\d{3})+(?:,\d+)?/.test(numStr)) {
                        numValue = parseFloat(numStr.replace(/\./g, '').replace(',', '.'))
                    } else {
                        // Si no, asumimos que es un formato simple (puede tener coma como decimal)
                        numValue = parseFloat(numStr.replace(',', '.'))
                    }

                    if (!isNaN(numValue)) {
                        ws[cellRef].v = numValue
                        ws[cellRef].t = 'n'

                        // Aplicar formato según tipo de columna
                        if (headerText.includes('%')) {
                            ws[cellRef].z = '0.00%'
                            // Si es porcentaje y ya tiene el símbolo %
                            if (originalText.includes('%')) {
                                ws[cellRef].v = numValue / 100
                            }
                        } else if (headerText.includes('Ton')) {
                            ws[cellRef].z = '#,##0.00'
                        } else if (headerText.includes('Quintales')) {
                            ws[cellRef].z = '#,##0.00'
                        } else if (headerText.includes('Libras')) {
                            ws[cellRef].z = '#,##0'
                        } else if (headerText.includes('Kg')) {
                            ws[cellRef].z = '#,##0'
                        } else if (headerText.includes('Cam. Falt.')) {
                            ws[cellRef].z = '#,##0.00'
                        } else if (headerText.includes('Placas')) {
                            ws[cellRef].z = '0'
                        } else {
                            ws[cellRef].z = '#,##0.00'
                        }
                    }
                }

                // Alinear celdas
                if (!ws[cellRef].s) ws[cellRef].s = {}

                if (c === 0) {
                    // Primera columna (Empresa) - alineación izquierda
                    ws[cellRef].s.alignment = { horizontal: 'left' }
                } else {
                    // Otras columnas - alineación derecha
                    ws[cellRef].s.alignment = { horizontal: 'right' }
                }

                // Encabezados en negrita
                if (r === 0) {
                    ws[cellRef].s.font = { bold: true }
                }

                cellIndex++
            }
        }

        // Configurar anchos de columna
        ws['!cols'] = headerRow.map((header, index) => {
            // Determinar el ancho máximo para cada columna
            let maxChars = header.length

            for (let r = 1; r < data.length; r++) {
                if (data[r][index]) {
                    maxChars = Math.max(maxChars, String(data[r][index]).length)
                }
            }

            // Ajustar ancho según el tipo de columna
            if (index === 0) {
                // Columna Empresa
                return { wch: Math.max(30, maxChars) }
            } else if (header.includes('Quintales') || header.includes('Libras')) {
                return { wch: Math.max(18, maxChars) }
            } else if (header.includes('%')) {
                return { wch: Math.max(12, maxChars) }
            } else {
                return { wch: Math.max(15, maxChars) }
            }
        })

        // Añadir la hoja al libro
        XLSX.utils.book_append_sheet(wb, ws, 'Resumen')

        // Guardar el archivo
        const dateStr = new Date().toISOString().slice(0, 10)
        XLSX.writeFile(wb, `${filename}_${dateStr}.xlsx`)

        console.log('Excel export successful')
    } catch (error) {
        console.error('Error exporting to Excel:', error)
        alert('Error al exportar a Excel: ' + error.message)
    }
}

// Función para exportar tabla a Excel
function exportTableToExcel(tableId, filename) {
    const table = document.getElementById(tableId)
    if (!table) {
        console.error('Table not found: ' + tableId)
        return
    }

    try {
        const { data } = createExcelDataFromTable(table)
        const wb = XLSX.utils.book_new()
        const ws = XLSX.utils.aoa_to_sheet(data)

        // Format numbers in worksheet
        formatNumbersInExcelSheet(ws, data)

        // Set column widths
        ws['!cols'] = calculateColumnWidths(data)

        XLSX.utils.book_append_sheet(wb, ws, 'Resumen')
        XLSX.writeFile(wb, filename + '_' + new Date().toISOString().slice(0, 10) + '.xlsx')

        console.log('Excel export successful')
    } catch (error) {
        console.error('Error exporting to Excel:', error)
        alert('Error al exportar a Excel: ' + error.message)
    }
}

// Función unificada para actualizar estado de botones
function updateAllExcelButtonStates() {
    // Existing button updates
    const selectedBarco = $('select[name="selectedBarco"]').val()
    const hasDataTabla2 = hasTableData('tabla2')
    const hasDataTabla1 = hasTableData('tabla1')

    $('#btnExportarExcel').prop('disabled', !selectedBarco || !hasDataTabla2)
    $('#btnExportarExcelIndividuales').prop('disabled', !selectedBarco || !hasDataTabla1)

    // New button update
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

    // Manejador para exportar registros individuales
    $('#btnExportarExcelIndividuales')
        .off('click')
        .on('click', function () {
            console.log('Exportando registros individuales')

            if (typeof XLSX === 'undefined') {
                console.error('La librería XLSX no está cargada correctamente')
                alert('La librería XLSX no está cargada correctamente')
                return
            }

            const tabla = document.getElementById('tabla1')
            if (!tabla) {
                console.error('No se encontró la tabla de registros individuales')
                alert('No se encontró la tabla de registros individuales')
                return
            }

            try {
                const wb = XLSX.utils.book_new()

                // Obtenemos encabezados manualmente para manejar las columnas especiales
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
                console.log(`Procesando ${bodyRows.length} filas`)

                Array.from(bodyRows).forEach((row, idx) => {
                    try {
                        const cells = row.querySelectorAll('td')
                        if (cells.length < 9) return

                        // Extraemos el número de fila
                        const numfila = cells[0].textContent.trim()

                        // Extraemos la escotilla
                        const escotilla = cells[1].textContent.trim()

                        // Extraemos la bodega
                        const bodega = cells[2].textContent.trim()

                        // Extraemos la guía y guía alterna
                        const guia =
                            cells[3].querySelector('div > span')?.textContent.trim() || cells[3].textContent.trim()
                        const guiaAlt = cells[3].querySelector('.data-alt-guia')?.textContent.trim() || ''

                        // Extraemos la placa y placa alterna
                        const placa =
                            cells[4].querySelector('div > span')?.textContent.trim() || cells[4].textContent.trim()
                        const placaAlt = cells[4].querySelector('.data-alt-placa')?.textContent.trim() || ''

                        // Extraemos pesos
                        const pesoRequerido = extraerNumero(cells[5].textContent)
                        const pesoEntregado = extraerNumero(cells[6].textContent)

                        // Extraemos conversiones
                        let libras = 0,
                            quintales = 0
                        const conversionCell = cells[7]
                        if (conversionCell) {
                            const lbsText = conversionCell.querySelector('.top-value')?.textContent || ''
                            const qqText = conversionCell.querySelector('.bottom-value')?.textContent || ''
                            libras = extraerNumero(lbsText)
                            quintales = extraerNumero(qqText)
                        }

                        // Extraemos el faltante y porcentaje
                        const pesoFaltante = extraerNumero(cells[8].textContent)
                        const porcentaje = extraerNumero(cells[9].textContent) / 100

                        const rowData = [
                            numfila, // #
                            escotilla, // Escotilla
                            bodega, // Bodega
                            guia, // Guía
                            guiaAlt, // Guía Alterna
                            placa, // Placa
                            placaAlt, // Placa Alterna
                            pesoRequerido, // Peso Requerido
                            pesoEntregado, // Peso Entregado
                            libras, // Entregado (Lbs)
                            quintales, // Entregado (Qq)
                            pesoFaltante, // Peso Faltante
                            porcentaje, // Porcentaje
                        ]

                        data.push(rowData)
                    } catch (rowError) {
                        console.error(`Error procesando fila ${idx}:`, rowError)
                    }
                })

                // Procesar fila de totales
                const footerRow = tabla.querySelector('tfoot tr')
                if (footerRow) {
                    const cells = footerRow.querySelectorAll('td')

                    const totalRow = Array(headers.length).fill('')
                    totalRow[0] = '' // #
                    totalRow[1] = '' // Esc.
                    totalRow[2] = 'Totales' // Totales
                    // No hay valores para guías y placas en totales
                    totalRow[3] = ''
                    totalRow[4] = ''
                    totalRow[5] = ''
                    totalRow[6] = ''

                    totalRow[7] = extraerNumero(cells[5]?.textContent || '0') // Peso Requerido
                    totalRow[8] = extraerNumero(cells[6]?.textContent || '0') // Peso Entregado

                    // Conversiones
                    const conversionCell = cells[7]
                    if (conversionCell) {
                        totalRow[9] = extraerNumero(conversionCell.querySelector('.top-value')?.textContent || '0')
                        totalRow[10] = extraerNumero(conversionCell.querySelector('.bottom-value')?.textContent || '0')
                    }

                    totalRow[11] = extraerNumero(cells[8]?.textContent || '0') // Peso Faltante
                    totalRow[12] = extraerNumero(cells[9]?.textContent || '0') / 100 // Porcentaje

                    data.push(totalRow)
                }

                const ws = XLSX.utils.aoa_to_sheet(data)

                // Formatos para las celdas
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

                console.log('Exportación completada con éxito')
            } catch (error) {
                console.error('Error durante la exportación:', error)
                alert('Error durante la exportación: ' + error.message)
            }
        })

    // Manejador para el botón de resumen agregado
    $('#btnExportarExcelResumen')
        .off('click')
        .on('click', function () {
            console.log('Exportando resumen agregado')
            exportResumenAgregadoToExcel('tabla2', 'Resumen_Agregado')
        })

    // Make functions available globally if needed
    window.exportTableToExcel = exportTableToExcel
    window.extraerNumero = extraerNumero
})
