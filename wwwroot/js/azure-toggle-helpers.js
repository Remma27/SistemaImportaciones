/**
 * Helpers para asegurar compatibilidad de toggle de unidades en Azure
 */
(function() {
    // Detectar si estamos en entorno Azure
    const isAzure = window.location.hostname.includes('azure') || 
                   window.location.hostname.includes('azurewebsites');
    
    // Solo aplicar en Azure
    if (!isAzure) return;
    
    // Verificar si jQuery está disponible
    const jQueryAvailable = typeof window.jQuery !== 'undefined';
    
    // Funciones auxiliares que no interfieren con las globales
    window.azureHelpers = {
        // Estado de jQuery
        hasJQuery: jQueryAvailable,
        
        // Función segura para aplicar toggle
        applyToggle: function(showAlternative) {
            try {
                // Intentar usar función global si existe
                if (typeof window.toggleUnitDisplay === 'function') {
                    window.toggleUnitDisplay(showAlternative);
                    return true;
                }
                
                // Fallback seguro sin jQuery
                const elements = document.querySelectorAll('.unit-toggle-element');
                const rows = document.querySelectorAll('.unit-toggle-row');
                
                elements.forEach(function(el) {
                    el.style.display = showAlternative ? 'block' : 'none';
                    if (showAlternative) el.classList.add('show');
                    else el.classList.remove('show');
                });
                
                rows.forEach(function(row) {
                    row.style.display = showAlternative ? 'table-row' : 'none';
                    if (showAlternative) row.classList.add('show');
                    else row.classList.remove('show');
                });
                
                // Intentar disparar evento para otros componentes
                try {
                    window.dispatchEvent(new CustomEvent('unitToggleChanged', {
                        detail: { showAlternative: showAlternative }
                    }));
                } catch (e) {
                    console.warn('Error al disparar evento unitToggleChanged:', e);
                }
                
                return true;
            } catch (error) {
                console.error('Error en applyToggle:', error);
                return false;
            }
        },
        
        // Leer estado actual
        getToggleState: function() {
            try {
                const savedMetricState = localStorage.getItem('showingMetric');
                return savedMetricState === 'false'; // true = mostrar alternativas
            } catch (error) {
                console.warn('Error al leer localStorage:', error);
                return false;
            }
        },
        
        // Disparar sincronización
        syncAll: function() {
            const showAlternative = this.getToggleState();
            return this.applyToggle(showAlternative);
        }
    };
    
    // Asegurar sincronización inicial
    document.addEventListener('DOMContentLoaded', function() {
        setTimeout(function() {
            if (window.azureHelpers) {
                window.azureHelpers.syncAll();
            }
        }, 1000);
    });
    
    // Monitorear cambios en localStorage para componentes que no reciben eventos
    try {
        let lastKnownState = localStorage.getItem('showingMetric');
        setInterval(function() {
            try {
                const currentState = localStorage.getItem('showingMetric');
                if (currentState !== lastKnownState) {
                    lastKnownState = currentState;
                    if (window.azureHelpers) {
                        window.azureHelpers.syncAll();
                    }
                }
            } catch (e) {}
        }, 2000);
    } catch (e) {}
})();
