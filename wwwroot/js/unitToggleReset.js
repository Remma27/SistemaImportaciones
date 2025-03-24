(function() {
    function resetToMetric() {
        try {
            localStorage.setItem('showingMetric', 'true');
            console.info('Estado de unidades restablecido a métrico (kilos)');
            
            if (typeof window.toggleUnitDisplay === 'function') {
                window.toggleUnitDisplay(false);
            }
            
            if (window.azureHelpers && typeof window.azureHelpers.applyToggle === 'function') {
                window.azureHelpers.applyToggle(false);
            }
        } catch (error) {
            console.warn('Error al restablecer estado de unidades:', error);
        }
    }
    
    window.addEventListener('beforeunload', resetToMetric);
    window.addEventListener('unload', resetToMetric);
    
    window.resetUnitToggleState = resetToMetric;
    
    document.addEventListener('DOMContentLoaded', function() {
        if (document.referrer === '' || 
            !document.referrer.includes(window.location.hostname)) {
            resetToMetric();
        }
    });
})();
