namespace backend.DTOs;

public record CreateAddressRequest(
    string Name,
    string Mobile,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string? Country,
    bool IsDefault);

public record UpdateAddressRequest(
    string Name,
    string Mobile,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string? Country,
    bool IsDefault);
