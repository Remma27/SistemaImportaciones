document.addEventListener('DOMContentLoaded', function () {
    // Verificar si Chart está disponible
    if (typeof Chart === 'undefined') {
        console.error('Chart.js no está cargado correctamente');
        return;
    }

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

    // Objeto para almacenar instancias de gráficas
    const chartInstances = {};

    function initializeDonutCharts() {
        document.querySelectorAll('canvas[id^="donutChart"]').forEach(canvas => {
            const porcentaje = parseFloat(canvas.getAttribute('data-porcentaje') || 0);
            const estado = canvas.getAttribute('data-estado');
            const complemento = 100 - porcentaje;
        
            const color = determinarColor(estado, porcentaje);
            
            // Método más seguro para verificar y destruir gráficos existentes
            try {
                // Si tenemos una instancia guardada para este canvas
                if (chartInstances[canvas.id]) {
                    chartInstances[canvas.id].destroy();
                    delete chartInstances[canvas.id];
                }
                // Alternativa usando Chart.getChart si está disponible
                else if (typeof Chart.getChart === 'function' && Chart.getChart(canvas)) {
                    Chart.getChart(canvas).destroy();
                }
            } catch (e) {
                console.warn('Error al intentar destruir gráfico existente:', e);
            }

            try {
                // Crear nueva instancia y guardarla
                chartInstances[canvas.id] = new Chart(canvas.getContext('2d'), {
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
            } catch (e) {
                console.error('Error al crear gráfico de dona:', e);
            }
        });
    }

    setTimeout(initializeDonutCharts, 300);

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

    setTimeout(function() {
        const progressBar = document.getElementById('progressBarMain');
        if (progressBar) {
            const porcentaje = parseFloat(progressBar.getAttribute('data-porcentaje'));
            progressBar.style.width = porcentaje + '%';
            console.log('Barra de progreso actualizada:', porcentaje + '%');
        }
    }, 100);

    document.getElementById('downloadBtn')?.addEventListener('click', async function() {
        try {
            this.disabled = true;
            this.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Procesando...';

            const barcoContainer = document.querySelector('.barco-container');
            
            const tempContainer = document.createElement('div');
            tempContainer.style.width = '1600px';
            tempContainer.style.height = '437px';
            tempContainer.style.position = 'absolute';
            tempContainer.style.left = '-9999px';
            document.body.appendChild(tempContainer);

            const clon = barcoContainer.cloneNode(true);
            tempContainer.appendChild(clon);

            const escotillas = {
                5: { top: 217, left: 421 },
                4: { top: 217, left: 599 },
                3: { top: 217, left: 778 },
                2: { top: 217, left: 943 },
                1: { top: 217, left: 1085 }
            };

            Object.entries(escotillas).forEach(([num, pos]) => {
                const escotilla = clon.querySelector(`#escotilla${num}`);
                if (escotilla) {
                    escotilla.style.position = 'absolute';
                    escotilla.style.top = `${pos.top}px`;
                    escotilla.style.left = `${pos.left}px`;
                    escotilla.style.zIndex = '10';
                }
            });

            const labels = clon.querySelector('.escotilla-labels');
            if (labels) {
                labels.style.position = 'absolute';
                labels.style.top = '202px';
                labels.style.left = '227px';
                labels.style.zIndex = '6';
            }

            const resumen = clon.querySelector('.barco-resumen');
            if (resumen) {
                resumen.style.position = 'absolute';
                resumen.style.bottom = '124px';
                resumen.style.left = '1217px';
                resumen.style.zIndex = '20';
            }

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

            const canvas = await html2canvas(tempContainer, options);
            
            const link = document.createElement('a');
            link.download = `visualizacion-barco-${new Date().toISOString().slice(0,10)}.png`;
            link.href = canvas.toDataURL('image/png');
            link.click();

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

    function syncUnitsToggleState() {
        const unitToggleElements = document.querySelectorAll('.unit-toggle-element');
        const unitToggleRows = document.querySelectorAll('.unit-toggle-row');
        
        try {
            const savedMetricState = localStorage.getItem('showingMetric');
            const showAlternative = savedMetricState === 'false';
            
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

    window.addEventListener('beforeunload', function() {
        localStorage.setItem('showingMetric', 'true');
    });
    
    window.addEventListener('unload', function() {
        localStorage.setItem('showingMetric', 'true');
    });

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

    setTimeout(syncUnitsToggleState, 300);

    function centrarContenidoBarco() {
        const barcoContainer = document.querySelector('.barco-container');
        if (barcoContainer) {
            barcoContainer.style.transform = 'none';
            barcoContainer.style.maxWidth = 'none';
            barcoContainer.style.width = '1600px';
        }
    }

    centrarContenidoBarco();
    
    window.addEventListener('resize', centrarContenidoBarco);
});
