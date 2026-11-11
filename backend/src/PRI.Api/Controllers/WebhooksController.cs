using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRI.Application.Revenue.Interfaces;
using PRI.Shared.Constants;

namespace PRI.Api.Controllers;

[Route(ApiRoutes.Base + "/webhooks")]
public sealed class WebhooksController(IPayPalWebhookService webhookService) : ApiControllerBase
{
    /// <summary>PayPal webhook'u. JWT istemez; kimlik, PayPal imzasının doğrulanmasıyla sağlanır.</summary>
    [HttpPost("paypal")]
    [AllowAnonymous]
    [RequestSizeLimit(1_000_000)]
    public async Task<IActionResult> PayPal(CancellationToken cancellationToken)
    {
        // İmza ham gövde üzerinden doğrulanır; bu yüzden gövde model binding olmadan okunur.
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var headers = new PayPalWebhookHeaders(
            Header("PAYPAL-AUTH-ALGO"),
            Header("PAYPAL-CERT-URL"),
            Header("PAYPAL-TRANSMISSION-ID"),
            Header("PAYPAL-TRANSMISSION-SIG"),
            Header("PAYPAL-TRANSMISSION-TIME"));

        await webhookService.HandleAsync(headers, rawBody, cancellationToken);

        return Ok();
    }

    private string Header(string name) => Request.Headers[name].ToString();
}
