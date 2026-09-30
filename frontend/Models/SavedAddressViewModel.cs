namespace LadiesDressStore.Web.Models;

public class SavedAddressViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "India";
    public bool IsDefault { get; set; }
    public string FullAddress { get; set; } = "";
}

public class SaveAddressViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string AddressLine1 { get; set; } = "";
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Country { get; set; } = "India";
    public bool IsDefault { get; set; }
}
