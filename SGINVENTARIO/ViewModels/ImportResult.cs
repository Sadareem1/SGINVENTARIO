namespace SGINVENTARIO.ViewModels;

public class ImportResult
{
    public int Procesados { get; set; }
    public int Creados { get; set; }
    public int Actualizados { get; set; }
    public List<string> Errores { get; } = new();
    public bool TieneErrores => Errores.Count > 0;
}
