// Añadir al final del archivo site.js

// Función para configurar tablas de respaldo cuando DataTables falla
function setupFallbackTables() {
    $('table').each(function () {
        if (!$(this).hasClass('dataTable')) {
            $(this).addClass('table table-striped table-bordered')
        }
    })
}

// Ejecuta esto si DataTables falla
$(document).ready(function () {
    if (typeof $.fn.DataTable === 'undefined') {
        console.log('DataTables no disponible, usando fallback')
        setupFallbackTables()
    }
})

// Funciones para controlar el sidebar
document.addEventListener('DOMContentLoaded', function() {
    // Detectar si estamos en la página principal
    const path = window.location.pathname;
    if (path === '/' || path === '/Home' || path === '/Home/Index') {
        document.body.classList.add('home-page');
    }
    
    const sidebar = document.getElementById('sidebar');
    const sidebarOverlay = document.getElementById('sidebarOverlay');
    const openSidebarBtn = document.getElementById('openSidebar');
    const closeSidebarBtn = document.getElementById('closeSidebar');
    
    // Abrir sidebar
    if (openSidebarBtn) {
        openSidebarBtn.addEventListener('click', function() {
            sidebar.classList.add('active');
            sidebarOverlay.style.display = 'block';
            document.body.style.overflow = 'hidden'; // Evita scroll en el body
        });
    }
    
    // Cerrar sidebar (botón)
    if (closeSidebarBtn) {
        closeSidebarBtn.addEventListener('click', function() {
            sidebar.classList.remove('active');
            sidebarOverlay.style.display = 'none';
            document.body.style.overflow = '';
        });
    }
    
    // Cerrar sidebar (overlay)
    if (sidebarOverlay) {
        sidebarOverlay.addEventListener('click', function() {
            sidebar.classList.remove('active');
            sidebarOverlay.style.display = 'none';
            document.body.style.overflow = '';
        });
    }
    
    // Adaptación para responsive
    window.addEventListener('resize', function() {
        if (window.innerWidth >= 992) { // Cambiado de 768 a 992 para coincidir con el breakpoint lg
            sidebar.classList.remove('active');
            sidebarOverlay.style.display = 'none';
            document.body.style.overflow = '';
        }
    });
});

// Manejar los desplegables del menú horizontal
document.addEventListener('DOMContentLoaded', function() {
    // Detectar si estamos en la página principal
    const path = window.location.pathname;
    if (path === '/' || path === '/Home' || path === '/Home/Index') {
        document.body.classList.add('home-page');
    }
    
    // Detectar clicks fuera de los menús desplegables para cerrarlos
    document.addEventListener('click', function(event) {
        const isClickInsideDropdown = event.target.closest('.dropdown-menu-item');
        
        if (!isClickInsideDropdown) {
            const openDropdowns = document.querySelectorAll('.dropdown-content');
            openDropdowns.forEach(dropdown => {
                if (getComputedStyle(dropdown).display === 'block') {
                    dropdown.style.display = 'none';
                }
            });
        }
    });
    
    // Para dispositivos táctiles, permitir tap para abrir/cerrar
    const dropdownItems = document.querySelectorAll('.dropdown-menu-item > a');
    
    dropdownItems.forEach(item => {
        item.addEventListener('click', function(e) {
            if (window.innerWidth >= 992) {
                e.preventDefault();
                const parent = this.parentElement;
                const dropdown = parent.querySelector('.dropdown-content');
                
                if (getComputedStyle(dropdown).display === 'block') {
                    dropdown.style.display = 'none';
                } else {
                    // Cerrar todos los otros dropdowns
                    document.querySelectorAll('.dropdown-content').forEach(el => {
                        if (el !== dropdown) {
                            el.style.display = 'none';
                        }
                    });
                    dropdown.style.display = 'block';
                }
            }
        });
    });
});