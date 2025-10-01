namespace SGINVENTARIO.ViewModels;

public class DashboardSummary
{
    public int TotalEquipos { get; set; }
    public int EquiposAsignados { get; set; }
    public int EquiposMantenimiento { get; set; }
    public int EquiposDeBaja { get; set; }
    public IReadOnlyDictionary<string, int> TotalesPorEstado { get; set; } = new Dictionary<string, int>();
    public IReadOnlyList<MovimientoEquipoDto> UltimosMovimientos { get; set; } = Array.Empty<MovimientoEquipoDto>();
    public IReadOnlyList<MantenimientoDto> ProximosMantenimientos { get; set; } = Array.Empty<MantenimientoDto>();
}

public class MovimientoEquipoDto
{
    public DateTime Fecha { get; set; }
    public string Equipo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Detalle { get; set; }
}

public class MantenimientoDto
{
    public string Equipo { get; set; } = string.Empty;
    public DateTime? FechaProgramada { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}
