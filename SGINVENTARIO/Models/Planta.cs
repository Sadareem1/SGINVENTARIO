using System.ComponentModel.DataAnnotations;

namespace SGINVENTARIO.Models;

public class Planta
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Direccion { get; set; }

    public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
}
