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

        const headerRow = [
            'Empresa',
            'Req. (Kg)',
            'Req. (Ton)',
            'Desc. (Kg)',
            'Falt. (Kg)',
            'Falt. (Ton)',
            'Cam. Falt.',
            'Placas',
            '% Desc.',
        ]
        ws_data.push(headerRow)

        const rows = table.querySelectorAll('tbody tr')
        for (let i = 0; i < rows.length; i++) {
            const row_data = []
            const cells = rows[i].querySelectorAll('td')

            row_data.push(cells[0].innerText.trim()) // Empresa
            row_data.push(cells[1].innerText.trim()) // Req. (Kg)
            row_data.push(cells[2].innerText.trim()) // Req. (Ton)
            row_data.push(cells[3].innerText.trim()) // Desc. (Kg)
            row_data.push(cells[5].innerText.trim()) // Falt. (Kg)
            row_data.push(cells[6].innerText.trim()) // Falt. (Ton)
            row_data.push(cells[8].innerText.trim()) // Cam. Falt.
            row_data.push(cells[9].innerText.trim()) // Placas
            row_data.push(cells[10].innerText.trim()) // % Desc.

            ws_data.push(row_data)
        }

        const footerCells = table.querySelectorAll('tfoot tr:first-child td')
        if (footerCells.length > 0) {
            const footerRow = []
            footerRow.push(footerCells[0].innerText.trim()) // Total
            footerRow.push(footerCells[1].innerText.trim()) // Req. (Kg)
            footerRow.push(footerCells[2].innerText.trim()) // Req. (Ton)
            footerRow.push(footerCells[3].innerText.trim()) // Desc. (Kg)
            footerRow.push(footerCells[5].innerText.trim()) // Falt. (Kg)
            footerRow.push(footerCells[6].innerText.trim()) // Falt. (Ton)
            footerRow.push(footerCells[8].innerText.trim()) // Cam. Falt.
            footerRow.push(footerCells[9].innerText.trim()) // Placas
            footerRow.push(footerCells[10].innerText.trim()) // % Desc.
            ws_data.push(footerRow)
        }

        const ws = XLSX.utils.aoa_to_sheet(ws_data)
        for (let i = 1; i < ws_data.length; i++) {
            for (let j = 1; j < ws_data[i].length; j++) {
                const cellAddress = XLSX.utils.encode_cell({ r: i, c: j })
                const cellValue = ws_data[i][j]

                if (typeof cellValue === 'string') {
                    const isPercentage = cellValue.includes('%')

                    if (isPercentage) {
                        const numericValue = parseFloat(cellValue.replace(/[^\d.,\-]/g, '')) / 100
                        ws[cellAddress] = {
                            v: numericValue,
                            t: 'n',
                            z: '0.00%',
                        }
                    } else {
                        let rawValue = cellValue
                            .replace(/[^\d.,\-]/g, '')
                            .replace(/\./g, '')
                            .replace(',', '.')

                        if (!isNaN(rawValue) && rawValue.length > 0) {
                            ws[cellAddress] = {
                                v: parseFloat(rawValue),
                                t: 'n',
                                z: '@',
                            }
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
