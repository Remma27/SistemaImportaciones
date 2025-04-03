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

                try {
                    _logger.LogInformation("Exportando todos los Barcos");
                    paqueteDatos.Barcos = await _context.Barcos.ToListAsync();
                    _logger.LogInformation("Exportados {Count} barcos", paqueteDatos.Barcos.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Barcos");
                    paqueteDatos.Barcos = new List<Barco>();
                }
                
                try {
                    _logger.LogInformation("Exportando todas las Empresas");
                    paqueteDatos.Empresas = await _context.Empresas.ToListAsync();
                    _logger.LogInformation("Exportadas {Count} empresas", paqueteDatos.Empresas.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Empresas");
                    paqueteDatos.Empresas = new List<Empresa>();
                }
                
                try {
                    _logger.LogInformation("Exportando todas las Unidades");
                    paqueteDatos.Unidades = await _context.Unidades.ToListAsync();
                    _logger.LogInformation("Exportadas {Count} unidades", paqueteDatos.Unidades.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Unidades");
                    paqueteDatos.Unidades = new List<Unidad>();
                }

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
                
                try {
                    _logger.LogInformation("Exportando todas las Bodegas de Empresa");
                    paqueteDatos.EmpresaBodegas = await _context.Empresa_Bodegas.ToListAsync();
                    _logger.LogInformation("Exportadas {Count} bodegas de empresa", paqueteDatos.EmpresaBodegas.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Bodegas de Empresa");
                    paqueteDatos.EmpresaBodegas = new List<Empresa_Bodegas>();
                }

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
                
                try {
                    _logger.LogInformation("Exportando todos los Roles");
                    
                    int rolesCount = await _context.Roles.CountAsync();
                    _logger.LogDebug("Conteo de roles en la base de datos: {Count}", rolesCount);
                    
                    var roles = await _context.Roles.AsNoTracking().ToListAsync();
                    _logger.LogDebug("Roles cargados de la base de datos: {Count}", roles.Count);
                    
                    try {
                        var tempOptions = new JsonSerializerOptions { WriteIndented = true };
                        var rolesJson = JsonSerializer.Serialize(roles, tempOptions);
                        _logger.LogDebug("Serialización de prueba exitosa: {Length} bytes", rolesJson.Length);
                    } catch (Exception serEx) {
                        _logger.LogError(serEx, "Error al serializar Roles individualmente");
                    }
                    
                    paqueteDatos.Roles = roles;
                    _logger.LogInformation("Exportados {Count} roles", roles.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Roles");
                    paqueteDatos.Roles = new List<Rol>();
                }
                
                try {
                    _logger.LogInformation("Exportando todos los Permisos");
                    paqueteDatos.Permisos = await _context.Permisos.ToListAsync();
                    _logger.LogInformation("Exportados {Count} permisos", paqueteDatos.Permisos.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar Permisos");
                    paqueteDatos.Permisos = new List<Permiso>();
                }
                
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
                
                try {
                    _logger.LogInformation("Exportando todo el Historial de Cambios");
                    paqueteDatos.HistorialCambios = await _context.HistorialCambios.ToListAsync();
                    _logger.LogInformation("Exportados {Count} registros de historial", paqueteDatos.HistorialCambios.Count);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Error al exportar HistorialCambios");
                    paqueteDatos.HistorialCambios = new List<HistorialCambios>();
                }
                
                var options = new JsonSerializerOptions
                {
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles, // Cambiar a IgnoreCycles
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    MaxDepth = 32
                };
                
                var jsonData = JsonSerializer.Serialize(paqueteDatos, options);
                
                var compressedStream = new MemoryStream();
                using (var gzipStream = new System.IO.Compression.GZipStream(
                    compressedStream, System.IO.Compression.CompressionMode.Compress, true))
                using (var writer = new StreamWriter(gzipStream))
                {
                    await writer.WriteAsync(jsonData);
                }
                
                compressedStream.Position = 0;

                var exportData = new 
                {
                    Fecha = DateTime.UtcNow,
                    EntidadesExportadas = new 
                    {
                        Barcos = paqueteDatos.Barcos.Count,
                        Empresas = paqueteDatos.Empresas.Count, 
                        Unidades = paqueteDatos.Unidades.Count,
                        Importaciones = paqueteDatos.Importaciones.Count,
                        Movimientos = paqueteDatos.Movimientos.Count,
                        EmpresaBodegas = paqueteDatos.EmpresaBodegas.Count,
                        Usuarios = paqueteDatos.Usuarios.Count, 
                        Roles = paqueteDatos.Roles.Count,
                        Permisos = paqueteDatos.Permisos.Count,
                        RolPermisos = paqueteDatos.RolPermisos.Count,
                        HistorialCambios = paqueteDatos.HistorialCambios.Count
                    },
                    TotalEntidades = paqueteDatos.Barcos.Count + paqueteDatos.Empresas.Count + paqueteDatos.Unidades.Count + 
                                     paqueteDatos.Importaciones.Count + paqueteDatos.Movimientos.Count + paqueteDatos.EmpresaBodegas.Count + 
                                     paqueteDatos.Usuarios.Count + paqueteDatos.Roles.Count + paqueteDatos.Permisos.Count + 
                                     paqueteDatos.RolPermisos.Count + paqueteDatos.HistorialCambios.Count
                };

                _historialService.GuardarHistorial(
                    "Sincronización", 
                    exportData, 
                    "Exportación",
                    $"Exportación completa. Total entidades exportadas: {exportData.TotalEntidades}");

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

                var strategy = _context.Database.CreateExecutionStrategy();
                
                await strategy.ExecuteAsync(async () => {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            await ImportarEntidades(_context.Barcos, paqueteDatos.Barcos, b => b.id, resultado);
                            await ImportarEntidades(_context.Empresas, paqueteDatos.Empresas, e => e.id_empresa, resultado);
                            await ImportarEntidades(_context.Unidades, paqueteDatos.Unidades, u => u.id, resultado);
                            await ImportarEntidades(_context.Roles, paqueteDatos.Roles, r => r.id, resultado);
                            await ImportarEntidades(_context.Permisos, paqueteDatos.Permisos, p => p.id, resultado);
                            
                            await _context.SaveChangesAsync();
                            _context.ChangeTracker.Clear();
                            
                            await ImportarEntidades(_context.Importaciones, paqueteDatos.Importaciones, i => i.id, resultado);
                            await ImportarEntidades(_context.Empresa_Bodegas, paqueteDatos.EmpresaBodegas, eb => eb.id, resultado);
                            await ImportarEntidades(_context.Usuarios, paqueteDatos.Usuarios, u => u.id, resultado);
                            await ImportarEntidades(_context.RolPermisos, paqueteDatos.RolPermisos, rp => rp.id, resultado);
                            
                            await _context.SaveChangesAsync();
                            _context.ChangeTracker.Clear();
                            
                            await ImportarEntidades(_context.Movimientos, paqueteDatos.Movimientos, m => m.id, resultado);
                            
                            await ImportarEntidades(_context.HistorialCambios, paqueteDatos.HistorialCambios, h => h.Id, resultado);
                            
                            await _context.SaveChangesAsync();
                            await transaction.CommitAsync();
                            
                            resultado.Exitoso = true;
                            resultado.Mensaje = $"Sincronización completada con éxito. Agregados: {resultado.Agregados}, Actualizados: {resultado.Actualizados}, Omitidos: {resultado.Omitidos}";
                            resultado.FechaSincronizacion = DateTime.UtcNow;

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
                            throw; 
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
                    
                    var existente = string.IsNullOrEmpty(idStr) ? null : await FindEntityByIdAsync<T>(idStr);
                    
                    if (existente == null)
                    {
                        LimpiarNavegacionesYPrepararFK(entidad);
                        
                        dbSet.Add(entidad);
                        resultado.Agregados++;
                        _logger.LogInformation($"Agregada entidad {typeof(T).Name} con ID {idStr}");
                    }
                    else
                    {
                        if (EntidadesIguales(existente, entidad))
                        {
                            resultado.Omitidos++;
                            _logger.LogInformation($"Omitida entidad {typeof(T).Name} con ID {idStr} (sin cambios)");
                        }
                        else
                        {
                            _context.Entry(existente).State = EntityState.Detached;
                            
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

        private bool EntidadesIguales<T>(T entidad1, T entidad2) where T : class
        {
            if (entidad1 == null || entidad2 == null)
                return false;
            
            var properties = typeof(T).GetProperties()
                .Where(p => 
                    p.CanRead && 
                    p.CanWrite &&
                    !p.Name.EndsWith("Id") && // Excluir FKs
                    !typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType) && 
                    p.PropertyType.Namespace != "System.Collections.Generic" &&
                    p.GetMethod?.IsVirtual != true);
                    
            foreach (var prop in properties)
            {
                var value1 = prop.GetValue(entidad1);
                var value2 = prop.GetValue(entidad2);
                
                if (value1 == null && value2 == null)
                    continue;
                    
                if (value1 == null || value2 == null)
                    return false;
                    
                if (!value1.Equals(value2))
                    return false;
            }
            
            return true;
        }

        private void LimpiarNavegacionesYPrepararFK<T>(T entidad) where T : class
        {
            var entityType = _context.Model.FindEntityType(typeof(T));
            if (entityType == null) return;
            
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
            
            foreach (var navigation in entityType.GetNavigations())
            {
                var propInfo = typeof(T).GetProperty(navigation.Name);
                if (propInfo != null)
                    propInfo.SetValue(entidad, null);
            }
            
            foreach (var fkEntry in fkValues)
            {
                var propInfo = typeof(T).GetProperty(fkEntry.Key);
                if (propInfo != null)
                    propInfo.SetValue(entidad, fkEntry.Value);
            }
            
            if (typeof(T) == typeof(Importacion))
            {
                var importacion = entidad as Importacion;
                if (importacion?.Barco != null && importacion.idbarco == 0)
                {
                    importacion.idbarco = importacion.Barco.id;
                    importacion.Barco = null;
                }
            }
            else if (typeof(T) == typeof(Movimiento))
            {
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

                datos.Position = 0;
                
                var archivoTemporal = Path.Combine(Path.GetTempPath(), $"Sincronizacion_{DateTime.Now:yyyyMMddHHmmss}.json");
                using (var fileStream = new FileStream(archivoTemporal, FileMode.Create))
                {
                    await datos.CopyToAsync(fileStream);
                }

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

                await smtpClient.SendMailAsync(mensaje);
                
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

        private async Task<TEntity?> FindEntityByIdAsync<TEntity>(string idStr) where TEntity : class
        {
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
            
            _logger.LogWarning("No se encontró un método específico para buscar entidad de tipo {EntityType}, usando método fallback", typeof(TEntity).Name);
            var entities = await _context.Set<TEntity>().ToListAsync();
            return entities.FirstOrDefault(e => 
            {
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