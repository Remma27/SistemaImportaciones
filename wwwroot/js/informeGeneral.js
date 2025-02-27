$(document).ready(function () {
    $('#btnToggleEscotillas').on('click', function () {
        $('#cardEscotillas').slideToggle()
        $(this).find('i').toggleClass('fa-eye fa-eye-slash')
    })

    $('#exportToExcel').on('click', function () {
        exportTableToExcel('tablaInforme', 'Informe_General_' + new Date().toISOString().slice(0, 10))
    })

    function exportTableToExcel(tableId, filename = '') {
        const table = document.querySelector('table')
        if (!table) {
            console.error(`Table not found`)
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

        const headerRow = []
        const headerCells = table.querySelectorAll('thead tr:first-child th')

        headerCells.forEach(cell => {
            if (cell.classList.contains('unit-toggle-columns')) {
                const headerName =
                    cell.querySelector('div:first-child')?.textContent.trim() || cell.innerText.split('\n')[0].trim()

                headerRow.push(`${headerName} (Libras)`)
                headerRow.push(`${headerName} (Quintales)`)
            } else {
                headerRow.push(cell.innerText.trim())
            }
        })

        ws_data.push(headerRow)

        const rows = table.querySelectorAll('tbody tr')
        for (let i = 0; i < rows.length; i++) {
            const row_data = []
            const cells = rows[i].querySelectorAll('td')

            for (let j = 0; j < cells.length; j++) {
                const cell = cells[j]

                if (cell.classList.contains('unit-toggle-columns')) {
                    const topValue = cell.querySelector('.top-value')?.textContent || ''
                    const bottomValue = cell.querySelector('.bottom-value')?.textContent || ''

                    const lbsValue = topValue.replace(/[^\d.,\-]/g, '').trim()
                    row_data.push(lbsValue)

                    const qqValue = bottomValue.replace(/[^\d.,\-]/g, '').trim()
                    row_data.push(qqValue)
                } else {
                    row_data.push(cell.innerText.trim())
                }
            }

            ws_data.push(row_data)
        }

        const footerCells = table.querySelectorAll('tfoot tr:first-child td')
        if (footerCells.length > 0) {
            const footerRow = []

            footerCells.forEach(cell => {
                if (cell.classList.contains('unit-toggle-columns')) {
                    const topValue = cell.querySelector('.top-value')?.textContent || ''
                    const bottomValue = cell.querySelector('.bottom-value')?.textContent || ''

                    const lbsValue = topValue.replace(/[^\d.,\-]/g, '').trim()
                    footerRow.push(lbsValue)

                    const qqValue = bottomValue.replace(/[^\d.,\-]/g, '').trim()
                    footerRow.push(qqValue)
                } else {
                    footerRow.push(cell.innerText.trim())
                }
            })

            ws_data.push(footerRow)
        }

        const ws = XLSX.utils.aoa_to_sheet(ws_data)

        for (let i = 1; i < ws_data.length; i++) {
            for (let j = 0; j < ws_data[i].length; j++) {
                const cellAddress = XLSX.utils.encode_cell({ r: i, c: j })
                const cellValue = ws_data[i][j]

                if (/^-?[\d.,]+$/.test(cellValue)) {
                    const numValue = parseFloat(cellValue.replace(/,/g, ''))
                    if (!isNaN(numValue)) {
                        ws[cellAddress] = { v: numValue, t: 'n' }
                    }
                }
            }
        }

        const colWidth = ws_data.reduce((acc, row) => {
            for (let i = 0; i < row.length; i++) {
                const cellValue = row[i] || ''
                acc[i] = Math.max(acc[i] || 8, cellValue.toString().length + 2)
            }
            return acc
        }, [])

        ws['!cols'] = colWidth.map(w => ({ width: w }))

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
