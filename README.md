# 🚢 Sistema de Gestión de Importaciones

Sistema web para la gestión y control de importaciones marítimas desarrollado con ASP.NET Core 9.0.

## 💻 Tecnologías Utilizadas

-   ASP.NET Core 9.0
-   Entity Framework Core
-   MySQL
-   BCrypt.NET
-   EPPlus
-   Swagger
-   Bootstrap 5
-   jQuery
-   DataTables
-   Chart.js
-   SheetJS

## ⚙️ Requisitos Previos

-   .NET 9.0 SDK
-   MySQL Server
-   Visual Studio 2022 o VS Code
-   Node.js y npm (opcional, para desarrollo frontend)

## 🔧 Configuración

1. Clone el repositorio
2. Actualice la cadena de conexión en `appsettings.json`
3. Configure las variables de entorno (opcional):
   - `JWT_EXPIRY_MINUTES`: Tiempo de expiración del token JWT (defecto: 60)
   - `API_BASE_URL`: URL base de la API (defecto: http://localhost:5079)
   - `API_KEY_1` y `API_KEY_2`: Claves de API para autenticación
   - `DATA_PROTECTION_PATH`: Ruta para almacenar claves de protección de datos
4. Ejecute las migraciones:

```bash
dotnet ef database update
```

## ✨ Características

-   🚢 Gestión de importaciones
-   🔍 Control de barcos y escotillas
-   🏭 Administración de bodegas
-   👤 Sistema de usuarios y autenticación
-   📊 Generación de reportes Excel
-   🔌 API RESTful documentada con Swagger
-   📜 Historial de cambios con visualización JSON
-   ⚖️ Sistema de conversión de unidades (kg, libras, quintales)
-   📈 Visualización gráfica del estado de las escotillas

## 🔥 Funcionalidades Principales

### ⚖️ Registro de Pesajes
- 📝 Control detallado de pesajes individuales y agregados
- 🔄 Conversión automática entre unidades de medida (kg, libras, quintales)
- 🧮 Cálculo de diferencias y porcentajes de carga

### 🧩 Visualización de Escotillas
- 🗺️ Representación gráfica de escotillas y bodegas
- 🚦 Indicadores visuales de estado (completado, en progreso, pendiente)
- 📊 Resumen de carga con totales por escotilla

### 📈 Reportes Exportables
- 📑 Generación de reportes Excel detallados
- 📋 Informes individuales y agregados
- 🔢 Visualización de datos por diferentes unidades de medida

## 📁 Estructura del Proyecto

-   `/Controllers` - Controladores MVC y API
-   `/Models` - Modelos de datos
-   `/Views` - Vistas MVC
-   `/Services` - Servicios de negocio
-   `/Middleware` - Middleware personalizado
-   `/Data` - Contexto de base de datos y migraciones
-   `/wwwroot` - Archivos estáticos (CSS, JS, imágenes)
-   `/Helpers` - Clases de utilidad
-   `/API` - Modelos específicos para la API

## 🚀 Instalación

1. Configure la base de datos MySQL:

```bash
mysql -u root -p
create database intergloimport;
create user 'prueba'@'localhost' identified by '12345678';
grant all privileges on intergloimport.* to 'prueba'@'localhost';
```

2. Instale las dependencias:

```bash
dotnet restore
```

3. Ejecute la aplicación:

```bash
dotnet run
```

## 👨‍💻 Configuración del Entorno de Desarrollo

1. Para trabajar con Visual Studio:
   - 📂 Abra la solución (.sln)
   - ⚙️ Configure el perfil de inicio en las propiedades del proyecto
   - ▶️ Inicie la aplicación con F5

2. Para trabajar con VS Code:
   - 🧩 Instale la extensión C# Dev Kit
   - 🔧 Configure el archivo launch.json para depuración
   - 🔄 Ejecute con F5 o use `dotnet watch run` para hot reload

## 🔌 Endpoints API

La documentación completa de la API está disponible en `/swagger` cuando la aplicación está en ejecución.

Endpoints importantes:
- 📋 `GET /api/RegistroPesajes`: Obtiene listado de registros de pesaje
- ➕ `POST /api/RegistroPesajes`: Crea nuevo registro de pesaje
- 📊 `GET /api/EscotillasResumen`: Obtiene resumen del estado de las escotillas

## 🔑 Autenticación

El sistema utiliza autenticación basada en cookies con las siguientes rutas:

-   🔐 Login: `/Auth/IniciarSesion`
-   🚪 Logout: `/Auth/CerrarSesion`

La API utiliza autenticación por token JWT.

## 🛠️ Solución de Problemas Comunes

- 🐞 **Problema**: Error de conexión a la base de datos
  ✅ **Solución**: Verifique la cadena de conexión en appsettings.json y que el servidor MySQL esté en ejecución

- 🐞 **Problema**: Las conversiones de unidades no funcionan correctamente
  ✅ **Solución**: Asegúrese de que las librerías JavaScript estén correctamente cargadas y que no haya errores en la consola

- 🐞 **Problema**: Errores al exportar a Excel
  ✅ **Solución**: Verifique que la librería SheetJS (xlsx) esté correctamente cargada

## 📄 Licencia

Este proyecto está bajo la Licencia MIT.
