using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SGINVENTARIO.Data;
using SGINVENTARIO.Models;
using SGINVENTARIO.ViewModels;

namespace SGINVENTARIO.Services;

public class InventoryService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(ApplicationDbContext db, ILogger<InventoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DashboardSummary> GetDashboardAsync()
    {
        var total = await _db.Equipos.CountAsync();
        var asignados = await _db.Equipos.CountAsync(e => e.Estado == EstadoEquipo.Asignado);
        var mantenimiento = await _db.Equipos.CountAsync(e => e.Estado == EstadoEquipo.Mantenimiento);
        var baja = await _db.Equipos.CountAsync(e => e.Estado == EstadoEquipo.DeBaja);

        var estados = await _db.Equipos
            .GroupBy(e => e.Estado)
            .Select(g => new { Estado = g.Key.ToString(), Cantidad = g.Count() })
            .ToDictionaryAsync(g => g.Estado, g => g.Cantidad);

        var ultimosMovimientos = await _db.Movimientos
            .Include(m => m.Equipo)
            .OrderByDescending(m => m.Fecha)
            .Take(5)
            .Select(m => new MovimientoEquipoDto
            {
                Fecha = m.Fecha,
                Equipo = m.Equipo!.CodigoInventario,
                Tipo = m.Tipo.ToString(),
                Detalle = m.Detalle
            })
            .ToListAsync();

        var proximosMantenimientos = await _db.Mantenimientos
            .Include(m => m.Equipo)
            .Where(m => m.Estado == EstadoMantenimiento.Pendiente)
            .OrderBy(m => m.FechaProgramada)
            .Take(5)
            .Select(m => new MantenimientoDto
            {
                Equipo = m.Equipo!.CodigoInventario,
                FechaProgramada = m.FechaProgramada,
                Estado = m.Estado.ToString(),
                Tipo = m.Tipo.ToString()
            })
            .ToListAsync();

        return new DashboardSummary
        {
            TotalEquipos = total,
            EquiposAsignados = asignados,
            EquiposMantenimiento = mantenimiento,
            EquiposDeBaja = baja,
            TotalesPorEstado = estados,
            UltimosMovimientos = ultimosMovimientos,
            ProximosMantenimientos = proximosMantenimientos
        };
    }

    public async Task<PagedResult<Equipo>> GetEquiposAsync(EquipoFilter filter)
    {
        var query = _db.Equipos
            .Include(e => e.Area)
            .Include(e => e.Planta)
            .Include(e => e.UsuarioAsignado)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            query = query.Where(e =>
                e.CodigoInventario.ToLower().Contains(term) ||
                (e.Marca != null && e.Marca.ToLower().Contains(term)) ||
                (e.Modelo != null && e.Modelo.ToLower().Contains(term)) ||
                (e.Descripcion != null && e.Descripcion.ToLower().Contains(term)));
        }

        if (filter.AreaId.HasValue)
        {
            query = query.Where(e => e.AreaId == filter.AreaId.Value);
        }

        if (filter.PlantaId.HasValue)
        {
            query = query.Where(e => e.PlantaId == filter.PlantaId.Value);
        }

        if (filter.Estado.HasValue)
        {
            query = query.Where(e => e.Estado == filter.Estado.Value);
        }

        query = ApplySorting(query, filter.SortBy, filter.SortDescending);

        var total = await query.CountAsync();
        var pageIndex = Math.Max(filter.PageIndex, 1);
        var pageSize = Math.Clamp(filter.PageSize, 5, 100);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Equipo>
        {
            Items = items,
            TotalCount = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public Task<Equipo?> GetEquipoAsync(int id)
    {
        return _db.Equipos
            .Include(e => e.Area)
            .Include(e => e.Planta)
            .Include(e => e.UsuarioAsignado)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteEquipoAsync(int id)
    {
        var equipo = await _db.Equipos.FindAsync(id);
        if (equipo is null)
        {
            return (false, "Equipo no encontrado");
        }

        _db.Equipos.Remove(equipo);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage, Equipo? Entity)> SaveEquipoAsync(Equipo modelo, string usuarioEjecutor)
    {
        if (modelo.Estado == EstadoEquipo.Asignado && modelo.UsuarioAsignadoId is null)
        {
            return (false, "Debe seleccionar un usuario asignado cuando el estado es Asignado.", null);
        }

        if (modelo.Estado == EstadoEquipo.DeBaja && modelo.UsuarioAsignadoId is not null)
        {
            modelo.UsuarioAsignadoId = null;
        }

        if (await _db.Equipos.AnyAsync(e => e.CodigoInventario == modelo.CodigoInventario && e.Id != modelo.Id))
        {
            return (false, "El código de inventario ya existe.", null);
        }

        Equipo? entity;
        if (modelo.Id == 0)
        {
            entity = new Equipo();
            _db.Equipos.Add(entity);
            MapEquipo(entity, modelo);
            await _db.SaveChangesAsync();

            await RegistrarMovimiento(entity.Id, TipoMovimiento.Registro, "Equipo registrado", usuarioEjecutor);
        }
        else
        {
            entity = await _db.Equipos
                .Include(e => e.Movimientos)
                .FirstOrDefaultAsync(e => e.Id == modelo.Id);

            if (entity is null)
            {
                return (false, "No se encontró el equipo para actualizar.", null);
            }

            _db.Entry(entity).Property(e => e.RowVersion).OriginalValue = modelo.RowVersion ?? Array.Empty<byte>();

            var estadoAnterior = entity.Estado;
            var usuarioAnterior = entity.UsuarioAsignadoId;
            var ubicacionAnterior = entity.Ubicacion;
            var plantaAnterior = entity.PlantaId;
            MapEquipo(entity, modelo);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return (false, "El registro fue modificado por otro usuario. Refresque la página e intente nuevamente.", null);
            }

            if (estadoAnterior != entity.Estado)
            {
                await RegistrarMovimiento(entity.Id, TipoMovimiento.CambioEstado,
                    $"Estado cambiado de {estadoAnterior} a {entity.Estado}", usuarioEjecutor);
            }

            if (usuarioAnterior != entity.UsuarioAsignadoId && entity.Estado == EstadoEquipo.Asignado)
            {
                await RegistrarMovimiento(entity.Id, TipoMovimiento.Asignacion,
                    "Equipo asignado a nuevo usuario", usuarioEjecutor);
            }

            if (plantaAnterior != entity.PlantaId || ubicacionAnterior != entity.Ubicacion)
            {
                await RegistrarMovimiento(entity.Id, TipoMovimiento.Traslado,
                    "Actualización de ubicación o planta", usuarioEjecutor);
            }
        }

        return (true, null, entity);
    }

    private static void MapEquipo(Equipo entity, Equipo modelo)
    {
        entity.CodigoInventario = modelo.CodigoInventario;
        entity.Marca = modelo.Marca;
        entity.Modelo = modelo.Modelo;
        entity.Descripcion = modelo.Descripcion;
        entity.Estado = modelo.Estado;
        entity.Ubicacion = modelo.Ubicacion;
        entity.AreaId = modelo.AreaId;
        entity.PlantaId = modelo.PlantaId;
        entity.UsuarioAsignadoId = modelo.Estado == EstadoEquipo.DeBaja ? null : modelo.UsuarioAsignadoId;
        entity.FechaRegistro = modelo.FechaRegistro;
        entity.Activo = modelo.Activo;
        entity.GarantiaFin = modelo.GarantiaFin;
    }

    public async Task<ImportResult> ImportEquiposCsvAsync(Stream stream, string usuarioEjecutor)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var header = await reader.ReadLineAsync();
        var result = new ImportResult();

        if (header is null)
        {
            result.Errores.Add("El archivo está vacío.");
            return result;
        }

        var columnas = header.Split(',', StringSplitOptions.TrimEntries);
        var esperado = new[]
        {
            "CodigoInventario","Marca","Modelo","Descripcion","Estado","Ubicacion","Area","Planta","UsuarioAsignadoEmail","GarantiaFin"
        };

        if (!esperado.SequenceEqual(columnas))
        {
            result.Errores.Add("El encabezado del archivo CSV no coincide con el formato esperado.");
            return result;
        }

        var lineNumber = 1;
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            result.Procesados++;
            var values = ParseCsvLine(line);
            if (values.Length != columnas.Length)
            {
                result.Errores.Add($"Línea {lineNumber}: número incorrecto de columnas.");
                continue;
            }

            try
            {
                var codigo = values[0].Trim();
                if (string.IsNullOrEmpty(codigo))
                {
                    throw new InvalidOperationException("El código de inventario es obligatorio.");
                }

                var equipo = await _db.Equipos
                    .Include(e => e.UsuarioAsignado)
                    .FirstOrDefaultAsync(e => e.CodigoInventario == codigo);

                var estado = Enum.Parse<EstadoEquipo>(values[4], true);

                if (equipo is null)
                {
                    equipo = new Equipo
                    {
                        CodigoInventario = codigo,
                        FechaRegistro = DateTime.UtcNow.Date
                    };
                    _db.Equipos.Add(equipo);
                    result.Creados++;
                }
                else
                {
                    result.Actualizados++;
                }

                equipo.Marca = values[1];
                equipo.Modelo = values[2];
                equipo.Descripcion = values[3];
                equipo.Estado = estado;
                equipo.Ubicacion = values[5];
                equipo.AreaId = await BuscarOCrearArea(values[6]);
                equipo.PlantaId = await BuscarOCrearPlanta(values[7]);
                equipo.UsuarioAsignadoId = await BuscarUsuarioPorEmail(values[8]);
                equipo.GarantiaFin = DateOnlyToDate(values[9]);

                await _db.SaveChangesAsync();
                await RegistrarMovimiento(equipo.Id, TipoMovimiento.Registro, "Importación CSV", usuarioEjecutor);
            }
            catch (Exception ex)
            {
                result.Errores.Add($"Línea {lineNumber}: {ex.Message}");
                _logger.LogWarning(ex, "Error importando línea {Line}", lineNumber);
            }
        }

        return result;
    }

    private async Task<int?> BuscarUsuarioPorEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var usuario = await _db.UsuariosPersona.FirstOrDefaultAsync(u => u.Email == email);
        if (usuario is null)
        {
            usuario = new UsuarioPersona
            {
                Email = email,
                Nombres = email,
                Apellidos = string.Empty
            };
            _db.UsuariosPersona.Add(usuario);
            await _db.SaveChangesAsync();
        }

        return usuario.Id;
    }

    private async Task<int> BuscarOCrearArea(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("El nombre de Área es obligatorio.");
        }

        var area = await _db.Areas.FirstOrDefaultAsync(a => a.Nombre == nombre);
        if (area is null)
        {
            area = new Area { Nombre = nombre };
            _db.Areas.Add(area);
            await _db.SaveChangesAsync();
        }

        return area.Id;
    }

    private async Task<int> BuscarOCrearPlanta(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new InvalidOperationException("El nombre de Planta es obligatorio.");
        }

        var planta = await _db.Plantas.FirstOrDefaultAsync(a => a.Nombre == nombre);
        if (planta is null)
        {
            planta = new Planta { Nombre = nombre };
            _db.Plantas.Add(planta);
            await _db.SaveChangesAsync();
        }

        return planta.Id;
    }

    private static DateTime? DateOnlyToDate(string input)
    {
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
        {
            return date.Date;
        }

        return null;
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            switch (ch)
            {
                case '"':
                    inQuotes = !inQuotes;
                    break;
                case ',' when !inQuotes:
                    result.Add(current.ToString());
                    current.Clear();
                    break;
                default:
                    current.Append(ch);
                    break;
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }

    private async Task RegistrarMovimiento(int equipoId, TipoMovimiento tipo, string detalle, string usuario)
    {
        var movimiento = new MovimientoEquipo
        {
            EquipoId = equipoId,
            Fecha = DateTime.UtcNow,
            Tipo = tipo,
            Detalle = detalle,
            UsuarioEjecutor = usuario
        };

        _db.Movimientos.Add(movimiento);
        await _db.SaveChangesAsync();
    }

    private static IQueryable<Equipo> ApplySorting(IQueryable<Equipo> query, string sortBy, bool descending)
    {
        return (sortBy, descending) switch
        {
            ("Marca", false) => query.OrderBy(e => e.Marca),
            ("Marca", true) => query.OrderByDescending(e => e.Marca),
            ("Modelo", false) => query.OrderBy(e => e.Modelo),
            ("Modelo", true) => query.OrderByDescending(e => e.Modelo),
            ("Estado", false) => query.OrderBy(e => e.Estado),
            ("Estado", true) => query.OrderByDescending(e => e.Estado),
            ("Area", false) => query.OrderBy(e => e.Area!.Nombre),
            ("Area", true) => query.OrderByDescending(e => e.Area!.Nombre),
            ("Planta", false) => query.OrderBy(e => e.Planta!.Nombre),
            ("Planta", true) => query.OrderByDescending(e => e.Planta!.Nombre),
            (_, true) => query.OrderByDescending(e => e.CodigoInventario),
            _ => query.OrderBy(e => e.CodigoInventario)
        };
    }

    public async Task<byte[]> ExportEquiposCsvAsync(EquipoFilter filter)
    {
        var data = await GetEquiposAsync(filter);
        var sb = new StringBuilder();
        sb.AppendLine("CodigoInventario,Marca,Modelo,Descripcion,Estado,Ubicacion,Area,Planta,UsuarioAsignado,GarantiaFin");
        foreach (var equipo in data.Items)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                EscapeCsv(equipo.CodigoInventario),
                EscapeCsv(equipo.Marca),
                EscapeCsv(equipo.Modelo),
                EscapeCsv(equipo.Descripcion),
                EscapeCsv(equipo.Estado.ToString()),
                EscapeCsv(equipo.Ubicacion),
                EscapeCsv(equipo.Area?.Nombre),
                EscapeCsv(equipo.Planta?.Nombre),
                EscapeCsv(equipo.UsuarioAsignado?.Email),
                EscapeCsv(equipo.GarantiaFin?.ToString("yyyy-MM-dd"))
            }));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportEquiposPdfAsync(EquipoFilter filter)
    {
        var data = await GetEquiposAsync(filter);
        var fecha = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Header().Text("SGINVENTARIO - Equipos")
                    .FontSize(18).Bold();
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text($"Generado: {fecha}");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Código").Bold();
                            header.Cell().Text("Marca").Bold();
                            header.Cell().Text("Modelo").Bold();
                            header.Cell().Text("Estado").Bold();
                            header.Cell().Text("Área / Planta").Bold();
                        });

                        foreach (var item in data.Items)
                        {
                            table.Cell().Text(item.CodigoInventario);
                            table.Cell().Text(item.Marca ?? string.Empty);
                            table.Cell().Text(item.Modelo ?? string.Empty);
                            table.Cell().Text(item.Estado.ToString());
                            table.Cell().Text($"{item.Area?.Nombre} / {item.Planta?.Nombre}");
                        }
                    });
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
