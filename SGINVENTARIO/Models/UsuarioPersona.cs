using System.ComponentModel.DataAnnotations;

namespace SGINVENTARIO.Models;

public class UsuarioPersona
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Nombres { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Apellidos { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Documento { get; set; }

    public ICollection<Equipo> EquiposAsignados { get; set; } = new List<Equipo>();

    public string NombreCompleto => $"{Nombres} {Apellidos}".Trim();
}
