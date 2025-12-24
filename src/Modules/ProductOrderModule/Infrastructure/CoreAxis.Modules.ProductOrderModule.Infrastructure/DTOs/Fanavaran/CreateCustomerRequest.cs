namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.DTOs.Fanavaran;

public class CreateCustomerRequest
{
    public string? NationalCode { get; set; }
    public int? BirthYear { get; set; }
    public int? BirthMonth { get; set; }
    public int? BirthDay { get; set; }
    public string? BirthPlace { get; set; }
    public string? IdentityNoIssuPlace { get; set; }
    public int? CityId { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? Tel { get; set; }
    public string? Mobile { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? JobAddress { get; set; }
    public string? MaritalStatus { get; set; }
    public string? ReligionId { get; set; }
    public string? EducationLevelId { get; set; }
    public string? EducationField { get; set; }
    public string? EconomicCode { get; set; }
    public string? EnName { get; set; }
    public string? EnLastName { get; set; }
    public string? EnAddress { get; set; }
    public string? IsIranian { get; set; }
}

public class CreateCustomerResponse
{
    // The prompt says "Now we have the insurer ID in response".
    // Assuming the response is either the ID directly or a JSON with ID.
    // Usually these APIs return a JSON object. I'll assume standard envelope or direct ID.
    // For now, let's map what we usually see, or just parse the result as string/dynamic if unsure.
    public int Result { get; set; } // Often 0 for success
    public string Message { get; set; }
    public long CustomerId { get; set; } // The ID we need
}
