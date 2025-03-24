document.addEventListener('DOMContentLoaded', function () {
    const COLORS = {
        COMPLETE: '#28a745',
        IN_PROGRESS: '#0d6efd',
        PENDING: '#6c757d',
        BACKGROUND: '#f8f9fa',
        WARNING: '#ffc107',
        DANGER: '#dc3545',
        INFO: '#0d6efd' 
    };

    function determinarColor(estado, porcentaje) {
        estado = (estado || '').toLowerCase().trim();
    
        if (estado.includes('complet')) return COLORS.COMPLETE;
        if (estado.includes('proces') || estado.includes('descarg')) {
            return porcentaje >= 90 ? COLORS.IN_PROGRESS : COLORS.WARNING; 
        }
        if (estado.includes('sin') || estado.includes('inici')) return COLORS.PENDING;
    
        if (porcentaje >= 100) return COLORS.COMPLETE;
        if (porcentaje >= 90) return COLORS.IN_PROGRESS; 
        
        return COLORS.WARNING;
    }

    function initializeDonutCharts() {
        document.querySelectorAll('canvas[id^="donutChart"]').forEach(canvas => {
            const porcentaje = parseFloat(canvas.getAttribute('data-porcentaje') || 0);
            const estado = canvas.getAttribute('data-estado');
            const complemento = 100 - porcentaje;
        
            const color = determinarColor(estado, porcentaje);

            if (Chart.getChart(canvas)) {
                Chart.getChart(canvas).destroy();
            }

            new Chart(canvas.getContext('2d'), {
                type: 'doughnut',
                data: {
                    datasets: [{
                        data: [porcentaje, complemento],
                        backgroundColor: [color, COLORS.BACKGROUND],
                        borderWidth: 0,
                        borderRadius: 5,
                        hoverOffset: 4
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    cutout: '75%',
                    rotation: -90,
                    circumference: 360,
                    plugins: {
                        legend: { display: false },
                        tooltip: { enabled: false }
                    },
                    animation: {
                        animateRotate: true,
                        animateScale: true,
                        duration: 2000,
                        easing: 'easeOutQuart'
                    }
                }
            });

            const container = canvas.closest('.donut-chart-container');
            if (container) {
                const badge = container.querySelector('.badge');
                if (badge) {
                    badge.style.backgroundColor = color;
                    badge.style.color = 'white';
                }
            
                const porcentajeSpan = container.querySelector('.fw-bold');
                if (porcentajeSpan) {
                    porcentajeSpan.style.color = color; 
                }
            }
        });
    }

    // Inicializar las donas después de un breve retraso para asegurar que todos los elementos están creados
    setTimeout(initializeDonutCharts, 300);

    // Usar un MutationObserver para detectar cambios en el DOM y actualizar las gráficas
    const observer = new MutationObserver(() => {
        initializeDonutCharts();
    });

    const container = document.querySelector('.card-body');
    if (container) {
        observer.observe(container, { 
            childList: true, 
            subtree: true 
        });
    }

    // Inicializar la barra de progreso
    setTimeout(function() {
        const progressBar = document.getElementById('progressBarMain');
        if (progressBar) {
            const porcentaje = parseFloat(progressBar.getAttribute('data-porcentaje'));
            progressBar.style.width = porcentaje + '%';
            console.log('Barra de progreso actualizada:', porcentaje + '%');
        }
    }, 100);

    // Funcionalidad para descargar imagen
    document.getElementById('downloadBtn')?.addEventListener('click', async function() {
        try {
            this.disabled = true;
            this.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Procesando...';

            // Primero, obtener solo el contenedor del barco
            const barcoContainer = document.querySelector('.barco-container');
            
            // Crear un contenedor temporal con dimensiones fijas
            const tempContainer = document.createElement('div');
            tempContainer.style.width = '1600px';
            tempContainer.style.height = '437px';
            tempContainer.style.position = 'absolute';
            tempContainer.style.left = '-9999px';
            document.body.appendChild(tempContainer);

            // Clonar el contenido
            const clon = barcoContainer.cloneNode(true);
            tempContainer.appendChild(clon);

            // Forzar las posiciones correctas en el clon
            const escotillas = {
                5: { top: 217, left: 421 },
                4: { top: 217, left: 599 },
                3: { top: 217, left: 778 },
                2: { top: 217, left: 943 },
                1: { top: 217, left: 1085 }
            };

            // Ajustar posiciones de cada escotilla
            Object.entries(escotillas).forEach(([num, pos]) => {
                const escotilla = clon.querySelector(`#escotilla${num}`);
                if (escotilla) {
                    escotilla.style.position = 'absolute';
                    escotilla.style.top = `${pos.top}px`;
                    escotilla.style.left = `${pos.left}px`;
                    escotilla.style.zIndex = '10';
                }
            });

            // Ajustar posición de las etiquetas
            const labels = clon.querySelector('.escotilla-labels');
            if (labels) {
                labels.style.position = 'absolute';
                labels.style.top = '202px';
                labels.style.left = '227px';
                labels.style.zIndex = '6';
            }

            // Ajustar posición del resumen
            const resumen = clon.querySelector('.barco-resumen');
            if (resumen) {
                resumen.style.position = 'absolute';
                resumen.style.bottom = '124px';
                resumen.style.left = '1217px';
                resumen.style.zIndex = '20';
            }

            // Configuración de html2canvas
            const options = {
                width: 1600,
                height: 437,
                scale: 2,
                useCORS: true,
                allowTaint: true,
                backgroundColor: '#ffffff',
                logging: false,
                removeContainer: true,
                ignoreElements: (element) => {
                    return element.classList.contains('capturing') || 
                           element.id === 'downloadBtn';
                }
            };

            // Generar la imagen
            const canvas = await html2canvas(tempContainer, options);
            
            // Crear y descargar la imagen
            const link = document.createElement('a');
            link.download = `visualizacion-barco-${new Date().toISOString().slice(0,10)}.png`;
            link.href = canvas.toDataURL('image/png');
            link.click();

            // Limpiar
            document.body.removeChild(tempContainer);
            this.disabled = false;
            this.innerHTML = '<i class="fas fa-download me-2"></i>Descargar';

        } catch (error) {
            console.error('Error:', error);
            alert('Error al generar la imagen. Por favor, intente nuevamente.');
            this.disabled = false;
            this.innerHTML = '<i class="fas fa-download me-2"></i>Descargar';
        }
    });

    // Manejo de unidades (métrico vs. imperial)
    function syncUnitsToggleState() {
        const unitToggleElements = document.querySelectorAll('.unit-toggle-element');
        const unitToggleRows = document.querySelectorAll('.unit-toggle-row');
        
        // Verificar estado en localStorage
        try {
            const savedMetricState = localStorage.getItem('showingMetric');
            const showAlternative = savedMetricState === 'false';
            
            // Aplicar estado actual
            unitToggleElements.forEach(el => {
                if (showAlternative) {
                    $(el).fadeIn(300);
                    el.classList.add('show');
                } else {
                    $(el).fadeOut(300);
                    el.classList.remove('show');
                }
            });
            
            unitToggleRows.forEach(row => {
                if (showAlternative) {
                    $(row).fadeIn(300);
                    row.classList.add('show');
                } else {
                    $(row).fadeOut(300);
                    row.classList.remove('show');
                }
            });
        } catch (error) {
            console.warn("Error al sincronizar estado de unidades:", error);
        }
    }

    // Registrar eventos para restablecer el estado a métrico al salir
    window.addEventListener('beforeunload', function() {
        localStorage.setItem('showingMetric', 'true');
    });
    
    window.addEventListener('unload', function() {
        localStorage.setItem('showingMetric', 'true');
    });

    // Escuchar eventos globales de cambio de unidades
    window.addEventListener('unitToggleChanged', function(e) {
        const showAlternative = e.detail.showAlternative;
        
        const unitToggleElements = document.querySelectorAll('.unit-toggle-element');
        const unitToggleRows = document.querySelectorAll('.unit-toggle-row');
        
        unitToggleElements.forEach(el => {
            if (showAlternative) {
                $(el).fadeIn(300);
                el.classList.add('show');
            } else {
                $(el).fadeOut(300);
                el.classList.remove('show');
            }
        });
        
        unitToggleRows.forEach(row => {
            if (showAlternative) {
                $(row).fadeIn(300);
                row.classList.add('show');
            } else {
                $(row).fadeOut(300);
                row.classList.remove('show');
            }
        });
    });

    // Sincronizar estado inicial
    setTimeout(syncUnitsToggleState, 300);

    // Función auxiliar para centrar el contenido del barco
    function centrarContenidoBarco() {
        const barcoContainer = document.querySelector('.barco-container');
        if (barcoContainer) {
            barcoContainer.style.transform = 'none';
            barcoContainer.style.maxWidth = 'none';
            barcoContainer.style.width = '1600px';
        }
    }

    // Ajustar el contenido del barco después de cargar
    centrarContenidoBarco();
    
    // También ajustar cuando se redimensiona la ventana
    window.addEventListener('resize', centrarContenidoBarco);
});
