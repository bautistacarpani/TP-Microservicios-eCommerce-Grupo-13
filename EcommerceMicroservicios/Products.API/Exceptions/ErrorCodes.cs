namespace Products.API.Exceptions;
// ══════════════════════════════════════════════════════════════════════
// ERROR CODES
// Centraliza los códigos de error del catálogo del TP.
// Usamos constantes para evitar strings hardcodeados en el código
// y facilitar el mantenimiento — si cambia un código, se cambia acá.
// ══════════════════════════════════════════════════════════════════════
public static class ErrorCodes
{
    public const string ProductoNoEncontrado      = "PRD-001";
    public const string DatosInvalidos            = "PRD-002";
    public const string NombreDuplicado           = "PRD-003";
    public const string ProductoConOrdenesActivas = "PRD-004";
    public const string ErrorInterno              = "PRD-005";
}