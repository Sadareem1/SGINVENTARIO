using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SGINVENTARIO.Models;

public class Mantenimiento
{
    public int Id { get; set; }

    public int EquipoId { get; set; }
    public Equipo? Equipo { get; set; }

    public TipoMantenimiento Tipo { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FechaProgramada { get; set; }

    [DataType(DataType.Date)]
    public DateTime? FechaEjecucion { get; set; }

    [StringLength(150)]
    public string? Proveedor { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? Costo { get; set; }

    [StringLength(400)]
    public string? Notas { get; set; }

    public EstadoMantenimiento Estado { get; set; } = EstadoMantenimiento.Pendiente;
}
