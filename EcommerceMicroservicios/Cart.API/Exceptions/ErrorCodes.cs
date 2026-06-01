namespace Cart.API.Exceptions;
// ══════════════════════════════════════════════════════════════════════
// ERROR CODES
// Centraliza los códigos de error del catálogo del TP.
// Usamos constantes para evitar strings hardcodeados en el código
// y facilitar el mantenimiento — si cambia un código, se cambia acá.
// ══════════════════════════════════════════════════════════════════════
public static class ErrorCodes
{
    public const string CarritoNoEncontrado    = "CRT-001";
    public const string ProductoNoEncontrado   = "CRT-002";
    public const string StockInsuficiente      = "CRT-003";
    public const string CantidadInvalida       = "CRT-004";
    public const string ErrorInterno           = "CRT-005";
}