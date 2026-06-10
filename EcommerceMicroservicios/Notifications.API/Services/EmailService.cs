using SendGrid;
using SendGrid.Helpers.Mail;

namespace Notifications.API.Services;

public class EmailService
{
    private readonly string _apiKey;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _apiKey = configuration["SendGrid:ApiKey"]
            ?? throw new InvalidOperationException("SendGrid API key no configurada.");
        _logger = logger;
    }

    // Envía un email real usando SendGrid.
    // Si falla, loguea el error pero no interrumpe el flujo principal
    // para que la notificación igual quede guardada en la BD.
    public async Task<bool> EnviarEmailAsync(string destinatario, string mensaje)
    {
        try
        {

            var client = new SendGridClient(_apiKey);

            var msg = MailHelper.CreateSingleEmail(
                from: new EmailAddress("tpmicroserviciosgrupo5@gmail.com", "ECommerce"),
                to: new EmailAddress(destinatario),
                subject: "🛒 Grupo 13 eCommerce — Nueva notificación",
                plainTextContent: mensaje,
                htmlContent: $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                    <div style='background-color: #1B3A5C; padding: 24px; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 24px;'>🛒 eCommerce Grupo 13</h1>
                        <p style='color: #B5D4F4; margin: 8px 0 0 0; font-size: 14px;'>Tu tienda online de confianza</p>
                    </div>
                    <div style='padding: 32px;'>
                        <h2 style='color: #1B3A5C;'>¡Bienvenido/a!</h2>
                        <p style='color: #333; line-height: 1.6;'>{mensaje}</p>
                        <p style='color: #666; line-height: 1.6;'>Ya podés explorar nuestro catálogo de productos y hacer tu primera compra.</p>
                        <div style='margin: 24px 0; text-align: center;'>
                            <span style='background-color: #1B3A5C; color: white; padding: 12px 24px; border-radius: 6px; font-size: 14px;'>¡Empezá a comprar!</span>
                        </div>
                    </div>
                    <div style='background-color: #f5f5f5; padding: 16px; text-align: center;'>
                        <p style='color: #999; font-size: 12px; margin: 0;'>TP Microservicios eCommerce — CAI 2026 — Grupo 13</p>
                    </div>
                </div>"
            );

            var response = await client.SendEmailAsync(msg);
            
            var responseBody = await response.Body.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email enviado correctamente a {Destinatario}", destinatario);
                return true;
            }

            _logger.LogWarning("SendGrid respondió con error {StatusCode} para {Destinatario}",
                response.StatusCode, destinatario);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar email a {Destinatario}", destinatario);
            return false;
        }
    }
}