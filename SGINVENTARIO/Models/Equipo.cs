using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SGINVENTARIO.Models;

public class Equipo
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string CodigoInventario { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Marca { get; set; }

    [StringLength(100)]
    public string? Modelo { get; set; }

    [StringLength(300)]
    public string? Descripcion { get; set; }

    public EstadoEquipo Estado { get; set; } = EstadoEquipo.Operativo;

    [StringLength(150)]
    public string? Ubicacion { get; set; }

    public int AreaId { get; set; }
    public Area? Area { get; set; }

    public int PlantaId { get; set; }
    public Planta? Planta { get; set; }

    public int? UsuarioAsignadoId { get; set; }
    public UsuarioPersona? UsuarioAsignado { get; set; }

    [DataType(DataType.Date)]
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow.Date;

    public bool Activo { get; set; } = true;

    [DataType(DataType.Date)]
    public DateTime? GarantiaFin { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<MovimientoEquipo> Movimientos { get; set; } = new List<MovimientoEquipo>();

    public ICollection<Mantenimiento> Mantenimientos { get; set; } = new List<Mantenimiento>();
}
