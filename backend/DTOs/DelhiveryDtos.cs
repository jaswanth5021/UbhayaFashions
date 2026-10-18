namespace backend.DTOs;

public sealed class CreateDelhiveryShipmentRequest
{
    public string RecipientName { get; set; } = "";
    public string RecipientPhone { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "India";
    public int WeightGrams { get; set; } = 500;
}