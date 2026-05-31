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
            Console.WriteLine($">>> API Key: '{_apiKey?.Substring(0, 10)}...'");

            var client = new SendGridClient(_apiKey);

            var msg = MailHelper.CreateSingleEmail(
                from: new EmailAddress("tpmicroserviciosgrupo5@gmail.com", "ECommerce"),
                to: new EmailAddress(destinatario),
                subject: "Notificación de tu orden",
                plainTextContent: mensaje,
                htmlContent: $"<p>{mensaje}</p>"
            );

            var response = await client.SendEmailAsync(msg);
            
            var responseBody = await response.Body.ReadAsStringAsync();
            Console.WriteLine($">>> SendGrid status: {response.StatusCode} - {responseBody}");

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