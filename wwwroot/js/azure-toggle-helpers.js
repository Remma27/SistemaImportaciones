(function() {
    const isAzure = window.location.hostname.includes('azure') || 
                   window.location.hostname.includes('azurewebsites');
    
    if (!isAzure) return;
    
    const jQueryAvailable = typeof window.jQuery !== 'undefined';
    
    window.azureHelpers = {
        hasJQuery: jQueryAvailable,
        
        applyToggle: function(showAlternative) {
            try {
                if (typeof window.toggleUnitDisplay === 'function') {
                    window.toggleUnitDisplay(showAlternative);
                    return true;
                }
                
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
        
        getToggleState: function() {
            try {
                const savedMetricState = localStorage.getItem('showingMetric');
                return savedMetricState === 'false'; 
            } catch (error) {
                console.warn('Error al leer localStorage:', error);
                return false;
            }
        },
        
        syncAll: function() {
            const showAlternative = this.getToggleState();
            return this.applyToggle(showAlternative);
        },
        
        resetToMetric: function() {
            try {
                localStorage.setItem('showingMetric', 'true');
                this.applyToggle(false);
                return true;
            } catch (e) {
                console.error('Error reseteando a métrico:', e);
                return false;
            }
        }
    };
    
    document.addEventListener('DOMContentLoaded', function() {
        setTimeout(function() {
            if (window.azureHelpers) {
                window.azureHelpers.syncAll();
            }
        }, 1000);
    });
    
    window.addEventListener('beforeunload', function() {
        if (window.azureHelpers) {
            window.azureHelpers.resetToMetric();
        }
    });
    
    window.addEventListener('unload', function() {
        if (window.azureHelpers) {
            window.azureHelpers.resetToMetric();
        }
    });
    
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
