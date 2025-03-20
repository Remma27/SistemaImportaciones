-- Crear tabla de roles
CREATE TABLE IF NOT EXISTS roles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    descripcion VARCHAR(255) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Crear tabla de permisos
CREATE TABLE IF NOT EXISTS permisos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    clave VARCHAR(100) NULL,
    descripcion VARCHAR(255) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Crear tabla de relación entre roles y permisos
CREATE TABLE IF NOT EXISTS rol_permisos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    rol_id INT NOT NULL,
    permiso_id INT NOT NULL,
    FOREIGN KEY (rol_id) REFERENCES roles(id) ON DELETE CASCADE,
    FOREIGN KEY (permiso_id) REFERENCES permisos(id) ON DELETE CASCADE,
    UNIQUE KEY uk_rol_permiso (rol_id, permiso_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Modificar la tabla de usuarios para añadir la relación con roles
ALTER TABLE usuarios 
ADD COLUMN rol_id INT NULL,
ADD CONSTRAINT fk_usuarios_roles FOREIGN KEY (rol_id) REFERENCES roles(id) ON DELETE SET NULL;

-- Insertar roles predeterminados
INSERT INTO roles (id, nombre, descripcion)
VALUES 
    (1, 'Administrador', 'Acceso completo al sistema'),
    (2, 'Operador', 'Acceso a operaciones básicas'),
    (3, 'Usuario', 'Acceso limitado solo lectura');

-- Insertar permisos básicos
INSERT INTO permisos (nombre, clave, descripcion)
VALUES 
    ('Ver importaciones', 'importaciones.ver', 'Permite ver las importaciones'),
    ('Crear importaciones', 'importaciones.crear', 'Permite crear nuevas importaciones'),
    ('Editar importaciones', 'importaciones.editar', 'Permite modificar importaciones existentes'),
    ('Eliminar importaciones', 'importaciones.eliminar', 'Permite eliminar importaciones'),
    
    ('Ver empresas', 'empresas.ver', 'Permite ver las empresas'),
    ('Crear empresas', 'empresas.crear', 'Permite crear nuevas empresas'),
    ('Editar empresas', 'empresas.editar', 'Permite modificar empresas existentes'),
    ('Eliminar empresas', 'empresas.eliminar', 'Permite eliminar empresas'),
    
    ('Ver barcos', 'barcos.ver', 'Permite ver los barcos'),
    ('Crear barcos', 'barcos.crear', 'Permite crear nuevos barcos'),
    ('Editar barcos', 'barcos.editar', 'Permite modificar barcos existentes'),
    ('Eliminar barcos', 'barcos.eliminar', 'Permite eliminar barcos'),
    
    ('Ver bodegas', 'bodegas.ver', 'Permite ver las bodegas'),
    ('Crear bodegas', 'bodegas.crear', 'Permite crear nuevas bodegas'),
    ('Editar bodegas', 'bodegas.editar', 'Permite modificar bodegas existentes'),
    ('Eliminar bodegas', 'bodegas.eliminar', 'Permite eliminar bodegas'),
    
    ('Ver movimientos', 'movimientos.ver', 'Permite ver los movimientos'),
    ('Crear movimientos', 'movimientos.crear', 'Permite crear nuevos movimientos'),
    ('Editar movimientos', 'movimientos.editar', 'Permite modificar movimientos existentes'),
    ('Eliminar movimientos', 'movimientos.eliminar', 'Permite eliminar movimientos'),
    
    ('Gestionar usuarios', 'usuarios.gestionar', 'Permite gestionar usuarios'),
    ('Ver historial', 'historial.ver', 'Permite ver el historial del sistema'),
    ('Ver informes', 'informes.ver', 'Permite ver informes');

-- Asignar todos los permisos al rol Administrador
INSERT INTO rol_permisos (rol_id, permiso_id)
SELECT 1, id FROM permisos;

-- Asignar permisos al rol Operador (acceso a todo excepto gestión de usuarios y historial)
INSERT INTO rol_permisos (rol_id, permiso_id)
SELECT 2, id FROM permisos 
WHERE clave NOT LIKE 'usuarios.%' AND clave NOT LIKE 'historial.%';

-- Asignar permisos de solo lectura al rol Usuario
INSERT INTO rol_permisos (rol_id, permiso_id)
SELECT 3, id FROM permisos 
WHERE clave LIKE '%.ver' OR clave = 'informes.ver';

-- Actualizar usuarios existentes para asignar el rol de Administrador al primer usuario
-- y Operador al resto (si hay usuarios sin rol asignado)
UPDATE usuarios SET rol_id = 1 WHERE id = 1 AND rol_id IS NULL;
UPDATE usuarios SET rol_id = 2 WHERE id > 1 AND rol_id IS NULL;
