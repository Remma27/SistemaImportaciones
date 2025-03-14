# Sistema de Gestión de Importaciones

Sistema web para la gestión y control de importaciones marítimas desarrollado con ASP.NET Core 9.0.

## Tecnologías Utilizadas

-   ASP.NET Core 9.0
-   Entity Framework Core
-   MySQL
-   BCrypt.NET
-   EPPlus
-   Swagger

## Requisitos Previos

-   .NET 9.0 SDK
-   MySQL Server
-   Visual Studio 2022 o VS Code

## Configuración

1. Clone el repositorio
2. Actualice la cadena de conexión en `appsettings.json`
3. Ejecute las migraciones:

```bash
dotnet ef database update
```

## Características

-   Gestión de importaciones
-   Control de barcos
-   Administración de bodegas
-   Sistema de usuarios y autenticación
-   Generación de reportes Excel
-   API RESTful documentada con Swagger

## Estructura del Proyecto

-   `/Controllers` - Controladores MVC y API
-   `/Models` - Modelos de datos
-   `/Views` - Vistas MVC
-   `/Services` - Servicios de negocio
-   `/Middleware` - Middleware personalizado
-   `/Data` - Contexto de base de datos y migraciones

## Instalación

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

## Endpoints API

La documentación completa de la API está disponible en `/swagger` cuando la aplicación está en ejecución.

## Autenticación

El sistema utiliza autenticación basada en cookies con las siguientes rutas:

-   Login: `/Auth/IniciarSesion`
-   Logout: `/Auth/CerrarSesion`

## Licencia

Este proyecto está bajo la Licencia MIT.
