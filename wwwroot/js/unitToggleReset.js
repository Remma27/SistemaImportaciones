/**
 * Módulo para gestionar el restablecimiento del estado de visualización de unidades
 * Este script asegura que el sistema siempre vuelva a mostrar unidades métricas 
 * cuando el usuario abandona una página
 */
(function() {
    // Función para restablecer el estado a métrico (kilos)
    function resetToMetric() {
        try {
            // Establecer showingMetric a true para mostrar unidades métricas por defecto
            localStorage.setItem('showingMetric', 'true');
            console.info('Estado de unidades restablecido a métrico (kilos)');
            
            // Intentar actualizar UI si estamos aún en la página
            if (typeof window.toggleUnitDisplay === 'function') {
                window.toggleUnitDisplay(false); // false = no mostrar alternativas
            }
            
            // Si existe el helper de Azure, usarlo también
            if (window.azureHelpers && typeof window.azureHelpers.applyToggle === 'function') {
                window.azureHelpers.applyToggle(false);
            }
        } catch (error) {
            console.warn('Error al restablecer estado de unidades:', error);
        }
    }
    
    // Registrar eventos de salida de página para restablecer el estado
    window.addEventListener('beforeunload', resetToMetric);
    window.addEventListener('unload', resetToMetric);
    
    // Hacer la función disponible globalmente para uso manual
    window.resetUnitToggleState = resetToMetric;
    
    // Comprobar al cargar si necesitamos restablecer
    document.addEventListener('DOMContentLoaded', function() {
        // Si viene de una página externa, asegurarse de que esté en métrico
        if (document.referrer === '' || 
            !document.referrer.includes(window.location.hostname)) {
            resetToMetric();
        }
    });
})();
