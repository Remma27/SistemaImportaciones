using System.Linq.Expressions;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Text.Json;

namespace API.Services
{
    public interface ISyncService
    {
        Task<MemoryStream> ExportarDatosAsync(DateTime? desde = null);
        Task<ResultadoSync> ImportarDatosAsync(Stream datosStream);
        Task<bool> EnviarPorCorreoAsync(string destinatario, MemoryStream datos);
        Task<DateTime> ObtenerUltimaSincronizacionAsync();
        Task RegistrarSincronizacionAsync(DateTime fecha);
    }

    public class SyncService : ISyncService
    {
        private readonly ApiContext _context;
        private readonly ILogger<SyncService> _logger;
        private readonly IConfiguration _config;
        private readonly HistorialService _historialService;
        
        // Lista de tablas sin campo de fecha (ajustar según tu estructura real)
        private readonly HashSet<Type> _entidadesSinFecha = new HashSet<Type>
        {
            // Ejemplos (reemplazar con tus entidades reales sin fecha):
            // typeof(Usuario),
            // typeof(Permiso),
            // typeof(Rol)
        };

        public SyncService(
            ApiContext context, 
            ILogger<SyncService> logger, 
            IConfiguration config,
            HistorialService historialService)
        {
            _context = context;
            _logger = logger;
            _config = config;
            _historialService = historialService;
        }

        public async Task<MemoryStream> ExportarDatosAsync(DateTime? desde = null)
        {
            try
            {
                _logger.LogInformation("Iniciando exportación completa de todas las tablas sin filtros");
                
                var paqueteDatos = new PaqueteSyncData
                {
                    FechaCreacion = DateTime.UtcNow,
                    IdSincronizacion = Guid.NewGuid().ToString(),
                    VersionBase = DateTime.MinValue
                };

                // 1. Barcos
                try {
                    _logger.LogInformation("Exportando todos los Barcos");
                    paqueteDatos.Barcos = await _context.Barcos.ToListAsync();
                    _logger.LogInformation("Exportados {Count} barcos", paqueteDatos.Barcos.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Barcos");
                    paqueteDatos.Barcos = new List<Barco>();
                }
                
                // 2. Empresas
                try {
                    _logger.LogInformation("Exportando todas las Empresas");
                    paqueteDatos.Empresas = await _context.Empresas.ToListAsync();
                    _logger.LogInformation("Exportadas {Count} empresas", paqueteDatos.Empresas.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Empresas");
                    paqueteDatos.Empresas = new List<Empresa>();
                }
                
                // 3. Unidades
                try {
                    _logger.LogInformation("Exportando todas las Unidades");
                    paqueteDatos.Unidades = await _context.Unidades.ToListAsync();
                    _logger.LogInformation("Exportadas {Count} unidades", paqueteDatos.Unidades.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Unidades");
                    paqueteDatos.Unidades = new List<Unidad>();
                }

                // 4. Importaciones
                try {
                    _logger.LogInformation("Exportando todas las Importaciones");
                    paqueteDatos.Importaciones = await _context.Importaciones
                        .Include(i => i.Barco)
                        .ToListAsync();
                    _logger.LogInformation("Exportadas {Count} importaciones", paqueteDatos.Importaciones.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Importaciones");
                    paqueteDatos.Importaciones = new List<Importacion>();
                }
                
                // 5. Movimientos
                try {
                    _logger.LogInformation("Exportando todos los Movimientos");
                    paqueteDatos.Movimientos = await _context.Movimientos
                        .Include(m => m.Importacion)
                        .Include(m => m.Empresa)
                        .ToListAsync();
                    _logger.LogInformation("Exportados {Count} movimientos", paqueteDatos.Movimientos.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Movimientos");
                    paqueteDatos.Movimientos = new List<Movimiento>();
                }
                
                // 6. Empresa_Bodegas
                try {
                    _logger.LogInformation("Exportando todas las Bodegas de Empresa");
                    paqueteDatos.EmpresaBodegas = await _context.Empresa_Bodegas.ToListAsync();
                    _logger.LogInformation("Exportadas {Count} bodegas de empresa", paqueteDatos.EmpresaBodegas.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Bodegas de Empresa");
                    paqueteDatos.EmpresaBodegas = new List<Empresa_Bodegas>();
                }

                // 7. Usuarios
                try {
                    _logger.LogInformation("Exportando todos los Usuarios");
                    paqueteDatos.Usuarios = await _context.Usuarios
                        .Include(u => u.Rol)
                        .ToListAsync();
                    _logger.LogInformation("Exportados {Count} usuarios", paqueteDatos.Usuarios.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Usuarios");
                    paqueteDatos.Usuarios = new List<Usuario>();
                }
                
                // 8. Roles - Diagnóstico detallado
                try {
                    _logger.LogInformation("Exportando todos los Roles");
                    
                    // 1. Intentar obtener el conteo
                    int rolesCount = await _context.Roles.CountAsync();
                    _logger.LogDebug("Conteo de roles en la base de datos: {Count}", rolesCount);
                    
                    // 2. Intentar cargar los roles sin tracking
                    var roles = await _context.Roles.AsNoTracking().ToListAsync();
                    _logger.LogDebug("Roles cargados de la base de datos: {Count}", roles.Count);
                    
                    // 3. Intentar serializar solo los roles para ver si hay problemas
                    try {
                        var tempOptions = new JsonSerializerOptions { WriteIndented = true };
                        var rolesJson = JsonSerializer.Serialize(roles, tempOptions);
                        _logger.LogDebug("Serialización de prueba exitosa: {Length} bytes", rolesJson.Length);
                    } catch (Exception serEx) {
                        _logger.LogError(serEx, "Error al serializar Roles individualmente");
                    }
                    
                    // 4. Asignar al paquete
                    paqueteDatos.Roles = roles;
                    _logger.LogInformation("Exportados {Count} roles", roles.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Roles");
                    paqueteDatos.Roles = new List<Rol>();
                }
                
                // 9. Permisos
                try {
                    _logger.LogInformation("Exportando todos los Permisos");
                    paqueteDatos.Permisos = await _context.Permisos.ToListAsync();
                    _logger.LogInformation("Exportados {Count} permisos", paqueteDatos.Permisos.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Permisos");
                    paqueteDatos.Permisos = new List<Permiso>();
                }
                
                // 10. RolPermisos
                try {
                    _logger.LogInformation("Exportando todos los RolPermisos");
                    paqueteDatos.RolPermisos = await _context.RolPermisos
                        .Include(rp => rp.Rol)
                        .Include(rp => rp.Permiso)
                        .ToListAsync();
                    _logger.LogInformation("Exportados {Count} relaciones rol-permiso", paqueteDatos.RolPermisos.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar RolPermisos");
                    paqueteDatos.RolPermisos = new List<RolPermiso>();
                }
                
                // 11. HistorialCambios (opcional)
                try {
                    _logger.LogInformation("Exportando todo el Historial de Cambios");
                    paqueteDatos.HistorialCambios = await _context.HistorialCambios.ToListAsync();
                    _logger.LogInformation("Exportados {Count} registros de historial", paqueteDatos.HistorialCambios.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar HistorialCambios");
                    paqueteDatos.HistorialCambios = new List<HistorialCambios>();
                }
                
                // Serializar a JSON
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles, // Cambiar a IgnoreCycles
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    MaxDepth = 32
                };
                
                var jsonData = JsonSerializer.Serialize(paqueteDatos, options);
                
                // Comprimir los datos
                var compressedStream = new MemoryStream();
                using (var gzipStream = new System.IO.Compression.GZipStream(
                    compressedStream, System.IO.Compression.CompressionMode.Compress, true))
                using (var writer = new StreamWriter(gzipStream))
                {
                    await writer.WriteAsync(jsonData);
                }
                
                compressedStream.Position = 0;
                
                // Registrar la exportación en el historial
                _historialService.GuardarHistorial("Sincronización", 
                    $"Exportación completa. Total entidades exportadas: " +
                    $"{paqueteDatos.Barcos.Count + paqueteDatos.Empresas.Count + paqueteDatos.Unidades.Count + 
                      paqueteDatos.Importaciones.Count + paqueteDatos.Movimientos.Count + paqueteDatos.EmpresaBodegas.Count + 
                      paqueteDatos.Usuarios.Count + paqueteDatos.Roles.Count + paqueteDatos.Permisos.Count + paqueteDatos.RolPermisos.Count}",
                    "Exportación");
                
                return compressedStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error grave al exportar datos para sincronización");
                throw new Exception("Error al exportar datos para sincronización", ex);
            }
        }

        public async Task<ResultadoSync> ImportarDatosAsync(Stream datosStream)
        {
            var resultado = new ResultadoSync { Mensaje = "" };
            
            try
            {
                if (datosStream == null)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Stream de datos vacío";
                    return resultado;
                }

                // Descomprimir el stream si es necesario
                datosStream.Position = 0;
                bool isGzipped = false;
                
                if (datosStream.Length > 2)
                {
                    byte[] signature = new byte[2];
                    int bytesRead = await datosStream.ReadAsync(signature, 0, 2);
                    isGzipped = bytesRead == 2 && signature[0] == 0x1F && signature[1] == 0x8B;
                    datosStream.Position = 0;
                }
                
                string jsonContent;
                
                if (isGzipped)
                {
                    _logger.LogInformation("Detectado archivo comprimido con GZIP, descomprimiendo...");
                    using (var gzipStream = new System.IO.Compression.GZipStream(
                        datosStream, System.IO.Compression.CompressionMode.Decompress))
                    using (var reader = new StreamReader(gzipStream))
                    {
                        jsonContent = await reader.ReadToEndAsync();
                    }
                }
                else
                {
                    _logger.LogInformation("Procesando archivo JSON sin comprimir");
                    using (var reader = new StreamReader(datosStream))
                    {
                        jsonContent = await reader.ReadToEndAsync();
                    }
                }

                if (string.IsNullOrEmpty(jsonContent))
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "Contenido JSON vacío";
                    return resultado;
                }

                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                };

                var paqueteDatos = JsonSerializer.Deserialize<PaqueteSyncData>(jsonContent, options);
                
                if (paqueteDatos == null)
                {
                    resultado.Exitoso = false;
                    resultado.Mensaje = "No se pudo deserializar los datos";
                    return resultado;
                }

                // Usar la estrategia de ejecución del contexto para manejar transacciones automáticamente
                var strategy = _context.Database.CreateExecutionStrategy();
                
                await strategy.ExecuteAsync(async () => {
                    // Todo el código que necesita transacción va dentro de esta lambda
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            // 1. Entidades base (sin dependencias)
                            await ImportarEntidades(_context.Barcos, paqueteDatos.Barcos, b => b.id, resultado);
                            await ImportarEntidades(_context.Empresas, paqueteDatos.Empresas, e => e.id_empresa, resultado);
                            await ImportarEntidades(_context.Unidades, paqueteDatos.Unidades, u => u.id, resultado);
                            await ImportarEntidades(_context.Roles, paqueteDatos.Roles, r => r.id, resultado);
                            await ImportarEntidades(_context.Permisos, paqueteDatos.Permisos, p => p.id, resultado);
                            
                            await _context.SaveChangesAsync();
                            _context.ChangeTracker.Clear();
                            
                            // 2. Entidades que dependen de las entidades base
                            await ImportarEntidades(_context.Importaciones, paqueteDatos.Importaciones, i => i.id, resultado);
                            await ImportarEntidades(_context.Empresa_Bodegas, paqueteDatos.EmpresaBodegas, eb => eb.id, resultado);
                            await ImportarEntidades(_context.Usuarios, paqueteDatos.Usuarios, u => u.id, resultado);
                            await ImportarEntidades(_context.RolPermisos, paqueteDatos.RolPermisos, rp => rp.id, resultado);
                            
                            await _context.SaveChangesAsync();
                            _context.ChangeTracker.Clear();
                            
                            // 3. Entidades que dependen de las entidades de nivel 2
                            await ImportarEntidades(_context.Movimientos, paqueteDatos.Movimientos, m => m.id, resultado);
                            
                            // 4. Finalmente historial y otras entidades de sistema
                            await ImportarEntidades(_context.HistorialCambios, paqueteDatos.HistorialCambios, h => h.Id, resultado);
                            
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            
                            resultado.Exitoso = true;
                            resultado.Mensaje = $"Sincronización completada con éxito. Agregados: {resultado.Agregados}, Actualizados: {resultado.Actualizados}, Omitidos: {resultado.Omitidos}";
                            resultado.FechaSincronizacion = DateTime.UtcNow;

                            // Create a detailed data object for better logging
                            var syncData = new 
                            {
                                Fecha = DateTime.UtcNow,
                                Agregados = resultado.Agregados,
                                Actualizados = resultado.Actualizados, 
                                Omitidos = resultado.Omitidos,
                                Errores = resultado.ErroresCount
                            };

                            _historialService.GuardarHistorial(
                                "Sincronización", 
                                syncData, 
                                "Importación",
                                resultado.Mensaje);
                        }
                        catch (Exception)
                        {
                            await transaction.RollbackAsync();
                            throw; // Re-throw para que la estrategia de ejecución lo maneje
                        }
                    }
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Error de actualización de base de datos durante la importación");
                
                string errorDetail = "";
                if (dbEx.InnerException != null)
                    errorDetail = dbEx.InnerException.Message;
                
                resultado.Exitoso = false;
                resultado.Mensaje = $"Error de base de datos: {dbEx.Message}. Detalles: {errorDetail}";
                resultado.ErroresCount++;
                resultado.Errores.Add(resultado.Mensaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar datos");
                
                resultado.Exitoso = false;
                resultado.Mensaje = $"Error: {ex.Message}";
                if (ex.InnerException != null)
                    resultado.Mensaje += $" | Detalle: {ex.InnerException.Message}";
                
                resultado.ErroresCount++;
                resultado.Errores.Add(resultado.Mensaje);
            }
            
            return resultado;
        }

        // Método auxiliar para importar entidades de forma simplificada
        private async Task ImportarEntidades<T>(
            DbSet<T> dbSet, 
            List<T> entidades, 
            Func<T, object> keySelector,
            ResultadoSync resultado) 
            where T : class
        {
            if (entidades?.Any() != true)
                return;
            
            _logger.LogInformation($"Importando {entidades.Count} entidades de tipo {typeof(T).Name}");
            
            foreach (var entidad in entidades)
            {
                try
                {
                    var id = keySelector(entidad);
                    var idStr = id?.ToString();
                    
                    if (string.IsNullOrEmpty(idStr))
                    {
                        resultado.Omitidos++;
                        continue;
                    }
                    
                    // Buscar entidad existente
                    var existente = string.IsNullOrEmpty(idStr) ? null : await FindEntityByIdAsync<T>(idStr);
                    
                    if (existente == null)
                    {
                        // Limpiar propiedades de navegación para evitar conflictos
                        LimpiarNavegacionesYPrepararFK(entidad);
                        
                        // Agregar nueva entidad
                        dbSet.Add(entidad);
                        resultado.Agregados++;
                        _logger.LogInformation($"Agregada entidad {typeof(T).Name} con ID {idStr}");
                    }
                    else
                    {
                        // Comparar entidades para evitar actualizaciones innecesarias
                        if (EntidadesIguales(existente, entidad))
                        {
                            // Sin cambios, no actualizar
                            resultado.Omitidos++;
                            _logger.LogInformation($"Omitida entidad {typeof(T).Name} con ID {idStr} (sin cambios)");
                        }
                        else
                        {
                            // Actualizar entidad existente
                            _context.Entry(existente).State = EntityState.Detached;
                            
                            // Limpiar propiedades de navegación para evitar conflictos
                            LimpiarNavegacionesYPrepararFK(entidad);
                            
                            dbSet.Update(entidad);
                            resultado.Actualizados++;
                            _logger.LogInformation($"Actualizada entidad {typeof(T).Name} con ID {idStr}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al procesar entidad {typeof(T).Name}");
                    resultado.ErroresCount++;
                    resultado.Errores.Add($"Error en {typeof(T).Name}: {ex.Message}");
                    resultado.Omitidos++;
                }
            }
        }

        // Método para comparar entidades y determinar si son iguales
        private bool EntidadesIguales<T>(T entidad1, T entidad2) where T : class
        {
            if (entidad1 == null || entidad2 == null)
                return false;
            
            // Obtener propiedades para comparar (excluyendo navegaciones y propiedades de seguimiento)
            var properties = typeof(T).GetProperties()
                .Where(p => 
                    p.CanRead && 
                    p.CanWrite &&
                    !p.Name.EndsWith("Id") && // Excluir FKs
                    !typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType) && // Excluir colecciones
                    p.PropertyType.Namespace != "System.Collections.Generic" && // Excluir colecciones genéricas
                    p.GetMethod?.IsVirtual != true); // Excluir propiedades virtuales (navegaciones)
                    
            // Comparar cada propiedad
            foreach (var prop in properties)
            {
                var value1 = prop.GetValue(entidad1);
                var value2 = prop.GetValue(entidad2);
                
                // Manejar comparación de nulos
                if (value1 == null && value2 == null)
                    continue;
                    
                if (value1 == null || value2 == null)
                    return false;
                    
                // Comparación según el tipo
                if (!value1.Equals(value2))
                    return false;
            }
            
            return true;
        }

        // Método para limpiar navegaciones pero preservando FK
        private void LimpiarNavegacionesYPrepararFK<T>(T entidad) where T : class
        {
            var entityType = _context.Model.FindEntityType(typeof(T));
            if (entityType == null) return;
            
            // 1. Guardar todos los valores de FKs primero
            var fkProperties = entityType.GetForeignKeys()
                .SelectMany(fk => fk.Properties)
                .ToList();
            
            var fkValues = new Dictionary<string, object>();
            foreach (var fkProp in fkProperties)
            {
                var propInfo = typeof(T).GetProperty(fkProp.Name);
                if (propInfo != null)
                {
                    var value = propInfo.GetValue(entidad);
                    if (value != null)
                        fkValues[fkProp.Name] = value;
                }
            }
            
            // 2. Limpiar todas las navegaciones
            foreach (var navigation in entityType.GetNavigations())
            {
                var propInfo = typeof(T).GetProperty(navigation.Name);
                if (propInfo != null)
                    propInfo.SetValue(entidad, null);
            }
            
            // 3. Restaurar valores de FKs
            foreach (var fkEntry in fkValues)
            {
                var propInfo = typeof(T).GetProperty(fkEntry.Key);
                if (propInfo != null)
                    propInfo.SetValue(entidad, fkEntry.Value);
            }
            
            // 4. Manejar casos especiales conocidos
            if (typeof(T) == typeof(Importacion))
            {
                // Asegurarse de que BarcoId está configurado
                var importacion = entidad as Importacion;
                if (importacion?.Barco != null && importacion.idbarco == 0)
                {
                    importacion.idbarco = importacion.Barco.id;
                    importacion.Barco = null;
                }
            }
            else if (typeof(T) == typeof(Movimiento))
            {
                // Asegurar que ImportacionId y EmpresaId están configurados
                var movimiento = entidad as Movimiento;
                if (movimiento?.Importacion != null && movimiento.idimportacion == 0)
                {
                    movimiento.idimportacion = movimiento.Importacion.id;
                    movimiento.Importacion = null;
                }
                if (movimiento?.Empresa != null && movimiento.idempresa == 0)
                {
                    movimiento.idempresa = movimiento.Empresa.id_empresa;
                    movimiento.Empresa = null;
                }
            }
        }

        public async Task<bool> EnviarPorCorreoAsync(string destinatario, MemoryStream datos)
        {
            try
            {
                _logger.LogInformation("Enviando datos de sincronización por correo a {Destinatario}", destinatario);
                
                if (datos == null || datos.Length == 0)
                {
                    _logger.LogInformation("Generando datos de exportación para el correo");
                    datos = await ExportarDatosAsync();
                }

                // Reposicionar el stream al inicio para leerlo
                datos.Position = 0;
                
                // Crear un archivo temporal
                var archivoTemporal = Path.Combine(Path.GetTempPath(), $"Sincronizacion_{DateTime.Now:yyyyMMddHHmmss}.json");
                using (var fileStream = new FileStream(archivoTemporal, FileMode.Create))
                {
                    await datos.CopyToAsync(fileStream);
                }

                // Configurar cliente SMTP desde configuración
                var smtpHost = _config["SMTP:Host"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_config["SMTP:Port"] ?? "587");
                var smtpUser = _config["SMTP:Username"] ?? throw new Exception("Falta configuración de usuario SMTP");
                var smtpPass = _config["SMTP:Password"] ?? throw new Exception("Falta configuración de contraseña SMTP");
                var smtpSsl = bool.Parse(_config["SMTP:EnableSsl"] ?? "true");
                var smtpFrom = _config["SMTP:FromEmail"] ?? smtpUser;

                var smtpClient = new SmtpClient(smtpHost)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = smtpSsl
                };

                // Crear mensaje
                var mensaje = new MailMessage
                {
                    From = new MailAddress(smtpFrom, "Sistema de Gestión de Importaciones"),
                    Subject = "Datos de Sincronización - Sistema de Importaciones",
                    Body = "Se adjunta el archivo de sincronización para importar en el sistema. " +
                           "Para sincronizar, abra el sistema y vaya a la opción 'Importar Datos' en el menú de Sincronización.",
                    IsBodyHtml = false
                };

                mensaje.To.Add(destinatario);
                mensaje.Attachments.Add(new Attachment(archivoTemporal));

                // Enviar el correo
                await smtpClient.SendMailAsync(mensaje);
                
                // Limpiar
                File.Delete(archivoTemporal);
                
                _logger.LogInformation("Correo de sincronización enviado con éxito a {Destinatario}", destinatario);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detallado al enviar correo: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner Exception: {Message}", ex.InnerException.Message);
                }
                return false;
            }
        }

        public async Task<DateTime> ObtenerUltimaSincronizacionAsync()
        {
            // Aquí podrías tener una tabla o registro para almacenar la última sincronización
            // Por simplicidad, retornamos hace 7 días como valor predeterminado
            try
            {
                var ultimoRegistro = await _context.HistorialCambios
                    .OrderByDescending(h => h.FechaHora)
                    .FirstOrDefaultAsync();

                return ultimoRegistro?.FechaHora ?? DateTime.UtcNow.AddDays(-7);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al obtener última sincronización, usando valor predeterminado");
                return DateTime.UtcNow.AddDays(-7);
            }
        }

        public Task RegistrarSincronizacionAsync(DateTime fecha)
        {
            var data = new { 
                Fecha = fecha,
                Timestamp = DateTime.UtcNow
            };
            
            _historialService.GuardarHistorial(
                "Sincronización", 
                data,
                "Sincronización",
                "Última sincronización completa");
                
            return Task.CompletedTask;
        }

        private async Task ProcesarEntidadConFecha<T>(
            DbSet<T> dbSet, 
            T entidad, 
            Func<T, object> keySelector,
            Func<T, DateTime> fechaSelector,
            ResultadoSync resultado) 
            where T : class
        {
            try
            {
                var id = keySelector(entidad);
                if (id == null)
                {
                    _logger.LogWarning("ID nulo para entidad tipo {TipoEntidad}", typeof(T).Name);
                    resultado.Omitidos++;
                    return;
                }

                var idStr = id?.ToString();
                if (string.IsNullOrEmpty(idStr))
                {
                    _logger.LogWarning("ID string nulo o vacío para entidad tipo {TipoEntidad}", typeof(T).Name);
                    resultado.Omitidos++;
                    return;
                }

                // Usar el método FindEntityByIdAsync en lugar de hacer búsquedas con expresiones no traducibles
                var entidadExistente = await FindEntityByIdAsync<T>(idStr);
                        
                if (entidadExistente == null)
                {
                    // Desconectar navegaciones para evitar problemas de tracking
                    DesconectarNavegaciones(entidad);
                    
                    // Nueva entidad - agregar
                    dbSet.Add(entidad);
                    resultado.Agregados++;
                    _logger.LogInformation("Agregada nueva {EntidadTipo} con ID {Id}", 
                        typeof(T).Name, id);
                }
                else 
                {
                    // Comparar fechas
                    var fechaExistente = fechaSelector(entidadExistente);
                    var fechaNueva = fechaSelector(entidad);
                    
                    if (fechaNueva > fechaExistente)
                    {
                        // Desconectar navegaciones para evitar problemas de tracking
                        DesconectarNavegaciones(entidad);
                        
                        // Desasociar la entidad existente
                        _context.Entry(entidadExistente).State = EntityState.Detached;
                        
                        // Actualizar con la nueva versión
                        dbSet.Update(entidad);
                        resultado.Actualizados++;
                        _logger.LogInformation("Actualizada {EntidadTipo} con ID {Id}", 
                            typeof(T).Name, id);
                    }
                    else
                    {
                        resultado.Omitidos++;
                        _logger.LogInformation("Omitida {EntidadTipo} con ID {Id}", 
                            typeof(T).Name, id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando entidad {Tipo} con ID {Id}: {Message}", 
                    typeof(T).Name, keySelector(entidad)?.ToString() ?? "desconocido", ex.Message);
                resultado.ErroresCount++;
                resultado.Errores.Add($"Error procesando {typeof(T).Name}: {ex.Message}");
            }
        }

        private async Task ProcesarEntidadSinFecha<T>(
            DbSet<T> dbSet, 
            T entidad, 
            Func<T, object> keySelector,
            ResultadoSync resultado) 
            where T : class
        {
            try
            {
                var id = keySelector(entidad);
                if (id == null)
                {
                    _logger.LogWarning("ID nulo para entidad sin fecha tipo {TipoEntidad}", typeof(T).Name);
                    resultado.Omitidos++;
                    return;
                }

                var idStr = id.ToString();
                
                // Buscar entidad existente por ID
                var entidadExistente = string.IsNullOrEmpty(idStr) ? null : await FindEntityByIdAsync<T>(idStr);
                        
                if (entidadExistente == null)
                {
                    // Desconectar navegaciones para evitar problemas de tracking
                    DesconectarNavegaciones(entidad);
                    
                    // Nueva entidad - agregar
                    dbSet.Add(entidad);
                    resultado.Agregados++;
                    _logger.LogInformation("Agregada nueva {EntidadTipo} sin fecha con ID {Id}", 
                        typeof(T).Name, id);
                }
                else 
                {
                    // Desconectar navegaciones para evitar problemas de tracking
                    DesconectarNavegaciones(entidad);
                    
                    // Desasociar la entidad existente
                    _context.Entry(entidadExistente).State = EntityState.Detached;
                    
                    // Actualizar con la nueva versión
                    dbSet.Update(entidad);
                    resultado.Actualizados++;
                    _logger.LogInformation("Actualizada {EntidadTipo} sin fecha con ID {Id}", 
                        typeof(T).Name, id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando entidad sin fecha {Tipo} con ID {Id}: {Message}", 
                    typeof(T).Name, keySelector(entidad)?.ToString() ?? "desconocido", ex.Message);
                resultado.ErroresCount++;
                resultado.Errores.Add($"Error procesando {typeof(T).Name}: {ex.Message}");
            }
        }

        // Método auxiliar para desconectar propiedades de navegación
        private void DesconectarNavegaciones<T>(T entidad) where T : class
        {
            // Preserve IDs for foreign keys while clearing navigation properties
            var navigationProperties = _context.Model
                .FindEntityType(typeof(T))
                ?.GetNavigations();
                
            if (navigationProperties != null)
            {
                foreach (var navigationProperty in navigationProperties)
                {
                    // Skip navigation properties for essential foreign keys in Importaciones and Movimientos
                    if ((typeof(T) == typeof(Importacion) && navigationProperty.Name == "Barco") ||
                        (typeof(T) == typeof(Movimiento) && (navigationProperty.Name == "Importacion" || 
                                                             navigationProperty.Name == "Empresa")))
                    {
                        // For these special cases, preserve the ID but null the navigation
                        var propertyInfo = typeof(T).GetProperty(navigationProperty.Name);
                        if (propertyInfo != null)
                        {
                            var value = propertyInfo.GetValue(entidad);
                            if (value != null)
                            {
                                // Keep the foreign key property (e.g., BarcoId) but clear the navigation property
                                var fkProp = typeof(T).GetProperty(navigationProperty.Name + "Id");
                                if (fkProp != null)
                                {
                                    var idValue = fkProp.GetValue(entidad);
                                    propertyInfo.SetValue(entidad, null);  // Clear navigation
                                    fkProp.SetValue(entidad, idValue);     // Restore FK value
                                }
                            }
                        }
                        continue;
                    }
                    
                    // For other navigation properties, handle normally
                    var propInfo = typeof(T).GetProperty(navigationProperty.Name);
                    if (propInfo != null)
                    {
                        var value = propInfo.GetValue(entidad);
                        
                        // If it's a collection, set to null
                        if (value is IEnumerable<object>)
                        {
                            propInfo.SetValue(entidad, null);
                        }
                        // If it's a reference, set to null
                        else if (value != null)
                        {
                            propInfo.SetValue(entidad, null);
                        }
                    }
                }
            }
        }

        // Approach 1: Use client evaluation
        private async Task<TEntity?> FindEntityByStringKeyAsync<TEntity>(string idStr, Expression<Func<TEntity, object>> keySelector) where TEntity : class
        {
            // Pull data to memory first, then apply the filter
            var items = await _context.Set<TEntity>().ToListAsync();
            return items.FirstOrDefault(e => keySelector.Compile()(e).ToString() == idStr);
        }

        // Approach 2: Use specific property expressions based on entity type
        private async Task<TEntity?> FindEntityByIdAsync<TEntity>(string idStr) where TEntity : class
        {
            // Use type-specific handling based on the entity type
            if (typeof(TEntity) == typeof(Barco))
            {
                return await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<int>(e, "id").ToString() == idStr) as TEntity;
            }
            else if (typeof(TEntity) == typeof(Empresa))
            {
                return await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<int>(e, "id_empresa").ToString() == idStr) as TEntity;
            }
            else if (typeof(TEntity) == typeof(Unidad))
            {
                return await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<int>(e, "id").ToString() == idStr) as TEntity;
            }
            else if (typeof(TEntity) == typeof(Importacion))
            {
                return await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<int>(e, "id").ToString() == idStr) as TEntity;
            }
            else if (typeof(TEntity) == typeof(Movimiento))
            {
                return await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<int>(e, "id").ToString() == idStr) as TEntity;
            }
            else if (typeof(TEntity) == typeof(Empresa_Bodegas))
            {
                return await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<int>(e, "id").ToString() == idStr) as TEntity;
            }
            else if (typeof(TEntity) == typeof(Usuario))
            {
                return await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<int>(e, "id").ToString() == idStr) as TEntity;
            }
            else if (typeof(TEntity) == typeof(Rol))
            {
                return await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<int>(e, "id").ToString() == idStr) as TEntity;
            }
            else if (typeof(TEntity) == typeof(Permiso))
            {
                return await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<int>(e, "id").ToString() == idStr) as TEntity;
            }
            else if (typeof(TEntity) == typeof(RolPermiso))
            {
                return await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<int>(e, "id").ToString() == idStr) as TEntity;
            }
            else if (typeof(TEntity) == typeof(HistorialCambios))
            {
                return await _context.Set<TEntity>().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id").ToString() == idStr) as TEntity;
            }
            
            // Fallback for any other entity types: load entities to memory and filter
            _logger.LogWarning("No se encontró un método específico para buscar entidad de tipo {EntityType}, usando método fallback", typeof(TEntity).Name);
            var entities = await _context.Set<TEntity>().ToListAsync();
            return entities.FirstOrDefault(e => 
            {
                // Try to find an Id property using reflection
                var idProperty = typeof(TEntity).GetProperties()
                    .FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) || 
                                       p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase));
                
                if (idProperty != null)
                {
                    var value = idProperty.GetValue(e);
                    return value?.ToString() == idStr;
                }
                
                return false;
            });
        }
    }

    public class PaqueteSyncData
    {
        public string IdSincronizacion { get; set; } = Guid.NewGuid().ToString();
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime VersionBase { get; set; } = DateTime.MinValue;
        
        // Entidades principales
        public List<Barco> Barcos { get; set; } = new List<Barco>();
        public List<Empresa> Empresas { get; set; } = new List<Empresa>();
        public List<Unidad> Unidades { get; set; } = new List<Unidad>();
        public List<Importacion> Importaciones { get; set; } = new List<Importacion>();
        public List<Movimiento> Movimientos { get; set; } = new List<Movimiento>();
        public List<Empresa_Bodegas> EmpresaBodegas { get; set; } = new List<Empresa_Bodegas>();
        
        // Entidades de seguridad
        public List<Usuario> Usuarios { get; set; } = new List<Usuario>();
        public List<Rol> Roles { get; set; } = new List<Rol>();
        public List<Permiso> Permisos { get; set; } = new List<Permiso>();
        public List<RolPermiso> RolPermisos { get; set; } = new List<RolPermiso>();
        
        // Entidades de sistema
        public List<HistorialCambios> HistorialCambios { get; set; } = new List<HistorialCambios>();
        
    }

    public class ResultadoSync
    {
        public bool Exitoso { get; set; }
        public required string Mensaje { get; set; }
        public int Agregados { get; set; }
        public int Actualizados { get; set; }
        public int Omitidos { get; set; }
        public int ErroresCount { get; set; }
        public List<string> Errores { get; set; } = new List<string>();
        public DateTime? FechaSincronizacion { get; set; }
    }
}