using System.ComponentModel.DataAnnotations;

namespace SGINVENTARIO.Models;

public class MovimientoEquipo
{
    public int Id { get; set; }

    public int EquipoId { get; set; }
    public Equipo? Equipo { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public TipoMovimiento Tipo { get; set; }

    [StringLength(300)]
    public string? Detalle { get; set; }

    [StringLength(150)]
    public string? UsuarioEjecutor { get; set; }
}
