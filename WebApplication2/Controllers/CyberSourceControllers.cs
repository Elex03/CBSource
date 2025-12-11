using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WebApplication2.Models;

namespace WebApplication2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CyberSourceController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CyberSourceController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        // ============================================================
        //     PROCESS PAYMENT REAL
        // ============================================================


        [HttpPost("process-payment")]
        public async Task<IActionResult> ProcessPayment(
    [FromBody] PaymentRequest request)
        {
            string merchantId = "sistematica_1765301688";
            string apiKeyId = "ce448504-8bd9-4c53-83a2-28376be38bad";
            string secretKey = "Ab/rdHBQCaxud0W6o3in3xEE3blCZNf9i+xVxlgsqjM=";

            string host = "apitest.cybersource.com";
            string resourcePath = "/pts/v2/payments";
            string url = $"https://{host}{resourcePath}";
            string method = "post";

            var payload = new
            {
                clientReferenceInformation = new { code = Guid.NewGuid().ToString() },
                processingInformation = new { capture = true },
                paymentInformation = new
                {
                    paymentType = new
                    {
                        method = new { name = "CARD" }
                    },
                    card = new
                    {
                        number = request.CardNumber,
                        expirationMonth = request.ExpirationMonth,
                        expirationYear = request.ExpirationYear,
                        securityCode = request.SecurityCode
                    }
                },
                orderInformation = new
                {
                    amountDetails = new
                    {
                        totalAmount = request.Amount,
                        currency = request.Currency
                    },
                    billTo = new
                    {
                        firstName = request.FirstName,
                        lastName = request.LastName,
                        address1 = request.Address,
                        locality = request.City,
                        administrativeArea = request.State,
                        postalCode = request.Zip,
                        country = request.Country,
                        email = request.Email
                    }
                }
            };


            string jsonBody = JsonSerializer.Serialize(payload);

            // DIGEST
            string digest = "SHA-256=" + Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(jsonBody))
            );

            string vcDate = DateTime.UtcNow.ToString("r");

            // CADENA PARA FIRMAR
            string signatureString =
                $"host: {host}\n" +
                $"v-c-date: {vcDate}\n" +
                $"request-target: {method} {resourcePath}\n" +
                $"digest: {digest}\n" +
                $"v-c-merchant-id: {merchantId}";

            // FIRMA EN HMAC SHA256
            byte[] decodedSecretKey = Convert.FromBase64String(secretKey);
            string signature = Convert.ToBase64String(
                new HMACSHA256(decodedSecretKey).ComputeHash(
                    Encoding.UTF8.GetBytes(signatureString)
                )
            );

            // HEADER SIGNATURE
            string signatureHeader =
                $"keyid=\"{apiKeyId}\"," +
                $" algorithm=\"HmacSHA256\"," +
                $" headers=\"host v-c-date request-target digest v-c-merchant-id\"," +
                $" signature=\"{signature}\"";

            // CLIENTE HTTP
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("v-c-date", vcDate);
            client.DefaultRequestHeaders.Add("v-c-merchant-id", merchantId);
            client.DefaultRequestHeaders.Add("digest", digest);
            client.DefaultRequestHeaders.Add("signature", signatureHeader);
            client.DefaultRequestHeaders.Add("host", host);

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            var resultContent = await response.Content.ReadAsStringAsync();

            // LOGS
            Console.WriteLine("=== CYBERSOURCE RESPONSE ===");
            Console.WriteLine(resultContent);

            if (response.IsSuccessStatusCode)
            {
                var csResponse = JsonSerializer.Deserialize<JsonElement>(resultContent);

                if (csResponse.TryGetProperty("status", out var statusProp))
                {
                    string status = statusProp.GetString();

                    if (status == "AUTHORIZED_PENDING_REVIEW")
                    {
                        string referenceId = csResponse.GetProperty("id").GetString();
                        string amount = null;
                        string currency = null;

                        if (csResponse.TryGetProperty("orderInformation", out var orderInfo) &&
                            orderInfo.TryGetProperty("amountDetails", out var amountDetails))
                        {
                            amount = amountDetails.GetProperty("authorizedAmount").GetString();
                            currency = amountDetails.GetProperty("currency").GetString();
                        }

                        return Ok(new
                        {
                            status = "review",
                            message = "La transacción fue autorizada pero marcada para revisión de seguridad.",
                            referenceId,
                            amount,
                            currency
                        });
                    }
                    else if (status == "AUTHORIZED")
                    {
                        string referenceId = csResponse.GetProperty("id").GetString();
                        string amount = null;
                        string currency = null;

                        if (csResponse.TryGetProperty("orderInformation", out var orderInfo) &&
                            orderInfo.TryGetProperty("amountDetails", out var amountDetails))
                        {
                            amount = amountDetails.GetProperty("authorizedAmount").GetString();
                            currency = amountDetails.GetProperty("currency").GetString();
                        }

                        return Ok(new
                        {
                            status = "approved",
                            message = "La transacción fue autorizada exitosamente.",
                            referenceId,
                            amount,
                            currency
                        });
                    }
                    else
                    {
                        // Caso de tarjeta declinada o error
                        string declineMessage = csResponse.TryGetProperty("message", out var msgProp)
                                                ? msgProp.GetString()
                                                : "La transacción fue declinada.";

                        return BadRequest(new
                        {
                            status = "declined",
                            message = declineMessage
                        });
                    }
                }
                else
                {
                    return BadRequest(new
                    {
                        status = "error",
                        message = "Respuesta inesperada del servidor de pagos."
                    });
                }
            }
            else
            {
                var csResponse = JsonSerializer.Deserialize<JsonElement>(resultContent);
                string declineMessage = csResponse.TryGetProperty("message", out var msgProp)
                                        ? msgProp.GetString()
                                        : "La transacción fue declinada.";

                return StatusCode((int)response.StatusCode, new
                {
                    status = "declined",
                    message = declineMessage,
                    httpStatus = response.StatusCode
                });
            }



        }
    }
}
