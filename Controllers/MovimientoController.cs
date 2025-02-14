using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Gestion_de_Importaciones.Data;
using Sistema_de_Gestion_de_Importaciones.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace Sistema_de_Gestion_de_Importaciones.Controllers;

public class MovimientoController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MovimientoController> _logger;

    public MovimientoController(ApplicationDbContext context, ILogger<MovimientoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: Movimiento/InformeGeneral
    [Authorize]
    [Route("Operaciones/InformeGeneral")]
    [HttpGet]
    public async Task<IActionResult> InformeGeneral(int? importacionId)
    {
        try
        {
            var importaciones = await _context.Importaciones
                .Include(i => i.Barco)
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            var selectListItems = importaciones.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = $"#{i.Id} - {(i.Barco?.NombreBarco ?? "Sin Barco")} - ({i.FechaHoraSystema:dd/MM/yyyy})",
                Selected = i.Id == importacionId
            }).ToList();

            selectListItems.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Seleccione una importación...",
                Selected = !importacionId.HasValue
            });

            ViewBag.Importaciones = selectListItems;

            if (!importacionId.HasValue)
            {
                return View("~/Views/Operaciones/InformeGeneral.cshtml", new List<InformeGeneralViewModel>());
            }

            // Obtener la data del informe usando el método existente
            var informeData = await GetInformeData(importacionId);

            return View("~/Views/Operaciones/InformeGeneral.cshtml", informeData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in InformeGeneral");
            throw;
        }
    }

    private async Task<List<InformeGeneralViewModel>> GetInformeData(int? importacionId)
    {
        // Obtener datos y analizar la estructura
        var analysis = await _context.Movimientos
            .Include(m => m.Empresa)
            .Where(m => !importacionId.HasValue || m.IdImportacion == importacionId)
            .Select(m => new
            {
                m.IdEmpresa,
                EmpresaNombre = m.Empresa != null ? m.Empresa.NombreEmpresa : "Sin Empresa",
                m.IdImportacion,
                m.CantidadRequerida,
                m.CantidadEntregada,
            })
            .ToListAsync();

        // Primero agrupar por empresa
        var groupedByEmpresa = analysis
            .GroupBy(m => new { m.IdEmpresa, m.EmpresaNombre })
            .Select(g => new
            {
                g.Key.IdEmpresa,
                g.Key.EmpresaNombre,
                ImportacionesCount = g.Select(x => x.IdImportacion).Distinct().Count(),
                MovimientosCount = g.Count(),
                TotalRequerido = g.Sum(x => x.CantidadRequerida ?? 0),
                TotalEntregado = g.Sum(x => x.CantidadEntregada ?? 0)
            })
            .ToList();

        // Crear el informe final
        var informeData = groupedByEmpresa
            .Select(g => new InformeGeneralViewModel
            {
                Empresa = g.EmpresaNombre ?? "Sin Empresa",
                RequeridoKg = g.TotalRequerido,
                DescargaKg = g.TotalEntregado,
                RequeridoTon = g.TotalRequerido / 1000,
                FaltanteKg = g.TotalRequerido - g.TotalEntregado,
                TonFaltantes = (g.TotalRequerido - g.TotalEntregado) / 1000,
                CamionesFaltantes = (int)Math.Ceiling((g.TotalRequerido - g.TotalEntregado) / 30000),
                ConteoPlacas = _context.Movimientos
                    .Where(m => m.IdEmpresa == g.IdEmpresa && m.Placa != null)
                    .Select(m => m.Placa)
                    .Distinct()
                    .Count(),
                PorcentajeDescarga = g.TotalRequerido > 0
                    ? (g.TotalEntregado / g.TotalRequerido) * 100
                    : 0
            })
            .OrderBy(x => x.Empresa)
            .ToList();

        return informeData;
    }
}