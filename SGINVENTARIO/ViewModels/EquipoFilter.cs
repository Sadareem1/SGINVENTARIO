using SGINVENTARIO.Models;

namespace SGINVENTARIO.ViewModels;

public class EquipoFilter
{
    public string? Search { get; set; }
    public int? AreaId { get; set; }
    public int? PlantaId { get; set; }
    public EstadoEquipo? Estado { get; set; }
    public string SortBy { get; set; } = "CodigoInventario";
    public bool SortDescending { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
