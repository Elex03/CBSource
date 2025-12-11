namespace WebApplication2.Models
{ 
public class PaymentRequest
{
    // 💳 Información de la tarjeta
    public string CardNumber { get; set; }          // "4111111111111111"
    public string ExpirationMonth { get; set; }     // "12"
    public string ExpirationYear { get; set; }      // "2030"
    public string SecurityCode { get; set; }        // "123"

    // 💰 Información del pedido
    public string Amount { get; set; }              // "10.00"
    public string Currency { get; set; }            // "USD"

    // 🧾 Información de facturación
    public string FirstName { get; set; }           // "Test"
    public string LastName { get; set; }            // "User"
    public string Address { get; set; }             // "1 Market St"
    public string City { get; set; }                // "San Francisco"
    public string State { get; set; }               // "CA"
    public string Zip { get; set; }                 // "94105"
    public string Country { get; set; }             // "US"
    public string Email { get; set; }               // "test@example.com"
}

public class SessionResponse
    {
        public string SessionId { get; set; }
    }
}
