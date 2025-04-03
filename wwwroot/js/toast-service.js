class ToastService {
    constructor() {
        this.toastContainer = document.getElementById('toast-container')
        if (!this.toastContainer) {
            this.toastContainer = document.createElement('div')
            this.toastContainer.id = 'toast-container'
            this.toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3'
            document.body.appendChild(this.toastContainer)
        }
    }

    show(message, type = 'info', duration = 5000) {
        const id = 'toast-' + Date.now()
        const iconClass = this._getIconClass(type)
        const bgClass = this._getBgClass(type)

        const toastHtml = `
            <div id="${id}" class="toast ${bgClass} text-white" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-header ${bgClass} text-white">
                    <i class="me-2 ${iconClass}"></i>
                    <strong class="me-auto">${this._getTitle(type)}</strong>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `

        this.toastContainer.insertAdjacentHTML('beforeend', toastHtml)

        const toastElement = document.getElementById(id)
        const toast = new bootstrap.Toast(toastElement, {
            autohide: true,
            delay: duration,
        })

        toast.show()

        toastElement.addEventListener('hidden.bs.toast', () => {
            toastElement.remove()
        })
    }

    success(message, duration = 5000) {
        this.show(message, 'success', duration)
    }

    error(message, duration = 5000) {
        this.show(message, 'error', duration)
    }

    warning(message, duration = 5000) {
        this.show(message, 'warning', duration)
    }

    info(message, duration = 5000) {
        this.show(message, 'info', duration)
    }

    _getIconClass(type) {
        switch (type) {
            case 'success':
                return 'fas fa-check-circle'
            case 'error':
                return 'fas fa-exclamation-circle'
            case 'warning':
                return 'fas fa-exclamation-triangle'
            case 'info':
                return 'fas fa-info-circle'
            default:
                return 'fas fa-info-circle'
        }
    }

    _getBgClass(type) {
        switch (type) {
            case 'success':
                return 'bg-success'
            case 'error':
                return 'bg-danger'
            case 'warning':
                return 'bg-warning'
            case 'info':
                return 'bg-info'
            default:
                return 'bg-info'
        }
    }

    _getTitle(type) {
        switch (type) {
            case 'success':
                return 'Éxito'
            case 'error':
                return 'Error'
            case 'warning':
                return 'Advertencia'
            case 'info':
                return 'Información'
            default:
                return 'Información'
        }
    }
}

window.toastService = window.toastService || new ToastService();
