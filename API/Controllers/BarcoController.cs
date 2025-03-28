﻿using Microsoft.AspNetCore.Mvc;
using API.Models;
using API.Data;
using Microsoft.AspNetCore.Authorization;
using API.Services;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class BarcoController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly HistorialService _historialService;
        private readonly ILogger<BarcoController> _logger;

        public BarcoController(ApiContext context, HistorialService historialService, ILogger<BarcoController> logger)
        {
            _context = context;
            _historialService = historialService;
            _logger = logger;
        }

        [HttpPost]
        [Consumes("application/json")]
        [Authorize(Roles = "Administrador,Operador")]
        public JsonResult Create([FromBody] Barco barco)
        {
            if (barco.id != 0)
            {
                return new JsonResult(BadRequest("El id debe ser 0"));
            }

            barco.nombrebarco = barco.nombrebarco?.ToUpper();
            
            _context.Barcos.Add(barco);
            _context.SaveChanges();
            
            _historialService.GuardarHistorial("CREAR", barco, "Barcos", $"Creación: {barco.nombrebarco}");
            
            return new JsonResult(Ok(barco));
        }

        [HttpPut]
        [Authorize(Roles = "Administrador,Operador")]
        public JsonResult Edit(Barco barco)
        {
            try
            {
                var barcoExistente = _context.Barcos.Find(barco.id);
                if (barcoExistente == null)
                {
                    return new JsonResult(NotFound());
                }

                _historialService.GuardarHistorial(
                    "ANTES_EDITAR", 
                    barcoExistente, 
                    "Barcos", 
                    $"Estado anterior del barco {barcoExistente.nombrebarco} (ID: {barcoExistente.id})"
                );
                
                barco.nombrebarco = barco.nombrebarco?.ToUpper();
                
                _context.Entry(barcoExistente).CurrentValues.SetValues(barco);
                _context.SaveChanges();
                
                _historialService.GuardarHistorial(
                    "DESPUES_EDITAR", 
                    barco, 
                    "Barcos", 
                    $"Estado nuevo del barco {barco.nombrebarco} (ID: {barco.id})"
                );
                
                return new JsonResult(Ok(barco));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar barco: {Id}", barco.id);
                return new JsonResult(StatusCode(500, new { message = "Error al editar barco", error = ex.Message }));
            }
        }

        // Get
        [HttpGet]
        public JsonResult Get(int id)
        {
            try
            {
                var result = _context.Barcos.Find(id);
                if (result == null)
                {
                    return new JsonResult(NotFound());
                }
                return new JsonResult(Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener barco: {Id}", id);
                return new JsonResult(StatusCode(500, new { message = "Error al obtener barco", error = ex.Message }));
            }
        }

        // Delete
        [HttpDelete]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var barco = await _context.Barcos.FindAsync(id);
                if (barco == null)
                {
                    return NotFound(new { message = "Barco no encontrado" });
                }

                var tieneImportaciones = await _context.Importaciones
                    .AnyAsync(i => i.idbarco == id);
                
                if (tieneImportaciones)
                {
                    return BadRequest(new {
                        message = "No se puede eliminar este barco porque tiene importaciones asociadas."
                    });
                }

                _historialService.GuardarHistorial("ELIMINAR", barco, "Barcos", $"Eliminación: {barco.nombrebarco}");
                
                _context.Barcos.Remove(barco);
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar barco: {Id}", id);
                return StatusCode(500, new { message = "Error al eliminar barco", error = ex.Message });
            }
        }

        // GetAll
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _context.Barcos.ToListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los barcos");
                return StatusCode(500, new { message = "Error al obtener barcos", error = ex.Message });
            }
        }
    }
}

