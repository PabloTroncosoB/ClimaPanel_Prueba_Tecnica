namespace ClimaPanel.Web.Common;

/// <summary>
/// Excepción pública disponible para representar fallas controladas del
/// proveedor meteorológico. Puede utilizarla o extender el manejo existente.
/// </summary>
public sealed class WeatherProviderException : Exception
{
    public WeatherProviderException(string message) : base(message)
    {
    }
}
