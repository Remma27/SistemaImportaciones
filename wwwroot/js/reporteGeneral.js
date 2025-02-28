$(document).ready(function () {
    console.log('ReporteGeneral.js inicializado')

    $('#btnExportarExcel').on('click', function () {
        console.log('Iniciando exportación a Excel...')
        const tabla = document.getElementById('tablaReporte')
        const wb = XLSX.utils.book_new()

        const filas = tabla.querySelectorAll('tr')
        const numFilas = filas.length
        let maxColumnas = 0

        filas.forEach(fila => {
            let colCount = 0
            const celdas = fila.querySelectorAll('th, td')
            celdas.forEach(celda => {
                const colspan = parseInt(celda.getAttribute('colspan') || '1', 10)
                colCount += colspan
            })
            maxColumnas = Math.max(maxColumnas, colCount)
        })

        console.log(`Dimensiones de tabla detectadas: ${numFilas} filas × ${maxColumnas} columnas`)

        const matrizCompleta = new Array(numFilas)
        for (let i = 0; i < numFilas; i++) {
            matrizCompleta[i] = new Array(maxColumnas).fill(null)
        }

        let filaActual = 0

        const encabezados = tabla.querySelectorAll('thead tr')
        encabezados.forEach(fila => {
            let colActual = 0
            const celdas = fila.querySelectorAll('th')

            celdas.forEach(celda => {
                while (matrizCompleta[filaActual][colActual] !== null) {
                    colActual++
                }

                const contenido = celda.innerText.trim()
                const colspan = parseInt(celda.getAttribute('colspan') || '1', 10)
                const rowspan = parseInt(celda.getAttribute('rowspan') || '1', 10)

                for (let i = 0; i < rowspan; i++) {
                    for (let j = 0; j < colspan; j++) {
                        if (filaActual + i < numFilas && colActual + j < maxColumnas) {
                            matrizCompleta[filaActual + i][colActual + j] = contenido
                        }
                    }
                }

                colActual += colspan
            })

            filaActual++
        })

        const cuerpo = tabla.querySelectorAll('tbody tr')
        cuerpo.forEach(fila => {
            let colActual = 0
            const celdas = fila.querySelectorAll('td')

            celdas.forEach(celda => {
                while (colActual < maxColumnas && matrizCompleta[filaActual][colActual] !== null) {
                    colActual++
                }

                if (colActual >= maxColumnas) return

                let contenido = celda.innerText.trim()
                const colspan = parseInt(celda.getAttribute('colspan') || '1', 10)
                const rowspan = parseInt(celda.getAttribute('rowspan') || '1', 10)

                if ([2, 3, 4, 5].includes(colActual)) {
                    if (contenido.includes(',') && contenido.indexOf(',') > contenido.lastIndexOf('.')) {
                        contenido = parseFloat(contenido.replace(/\./g, '').replace(',', '.'))
                    } else if (contenido.includes('.') || contenido.includes(',')) {
                        contenido = parseFloat(contenido.replace(/,/g, ''))
                    } else if (contenido !== '' && !isNaN(Number(contenido))) {
                        contenido = parseFloat(contenido)
                    }
                }

                for (let i = 0; i < rowspan; i++) {
                    for (let j = 0; j < colspan; j++) {
                        if (filaActual + i < numFilas && colActual + j < maxColumnas) {
                            matrizCompleta[filaActual + i][colActual + j] = contenido
                        }
                    }
                }

                colActual += colspan
            })

            filaActual++
        })

        const pie = tabla.querySelectorAll('tfoot tr')
        pie.forEach(fila => {
            let colActual = 0
            const celdas = fila.querySelectorAll('td')

            celdas.forEach(celda => {
                while (colActual < maxColumnas && matrizCompleta[filaActual][colActual] !== null) {
                    colActual++
                }

                if (colActual >= maxColumnas) return

                let contenido = celda.innerText.trim()
                const colspan = parseInt(celda.getAttribute('colspan') || '1', 10)

                if ([2, 3, 4, 5].includes(colActual)) {
                    if (contenido.includes(',') && contenido.indexOf(',') > contenido.lastIndexOf('.')) {
                        contenido = parseFloat(contenido.replace(/\./g, '').replace(',', '.'))
                    } else if (contenido.includes('.') || contenido.includes(',')) {
                        contenido = parseFloat(contenido.replace(/,/g, ''))
                    } else if (contenido !== '' && !isNaN(Number(contenido))) {
                        contenido = parseFloat(contenido)
                    }
                }

                for (let j = 0; j < colspan; j++) {
                    if (colActual + j < maxColumnas) {
                        matrizCompleta[filaActual][colActual + j] = j === 0 ? contenido : null
                    }
                }

                colActual += colspan
            })

            filaActual++
        })

        const datosFinales = matrizCompleta.map(fila => fila.map(celda => (celda === null ? '' : celda)))

        const ws = XLSX.utils.aoa_to_sheet(datosFinales)

        const numericColumns = [2, 3, 4, 5]
        for (let R = 2; R < datosFinales.length; R++) {
            for (let C of numericColumns) {
                if (C < datosFinales[R].length) {
                    const cellAddress = XLSX.utils.encode_cell({ r: R, c: C })
                    if (ws[cellAddress] && typeof ws[cellAddress].v === 'number') {
                        ws[cellAddress].t = 'n'
                    }
                }
            }
        }

        const colWidths = []
        for (let c = 0; c < maxColumnas; c++) {
            let maxWidth = 10

            for (let r = 0; r < datosFinales.length; r++) {
                if (datosFinales[r][c]) {
                    const value = String(datosFinales[r][c])
                    maxWidth = Math.max(maxWidth, value.length + 2)
                }
            }

            colWidths.push({ wch: maxWidth })
        }

        ws['!cols'] = colWidths

        XLSX.utils.book_append_sheet(wb, ws, 'Reporte')
        const nombreArchivo = 'Reporte_Escotillas_Por_Empresa_' + new Date().toISOString().slice(0, 10)
        XLSX.writeFile(wb, nombreArchivo + '.xlsx')

        console.log(`Archivo exportado como: ${nombreArchivo}.xlsx`)
    })
})
