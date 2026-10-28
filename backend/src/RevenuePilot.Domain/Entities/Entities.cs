namespace RevenuePilot.Domain.Entities;

public enum CustomerStatus { Active, Archived }
public enum OrderStatus { Paid, Pending, Failed, Cancelled, Refunded }

public class Business
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? BusinessModel { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string TimeZoneId { get; set; } = "Africa/Johannesburg";
    public bool IsDemo { get; set; }
    public DateTime CreatedAt { get; set; }   // UTC
}

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public string? ExternalCustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Segment { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public DateTime CreatedAt { get; set; }   // UTC
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid CustomerId { get; set; }
    public string ExternalOrderId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }   // UTC
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public DateTime CreatedAt { get; set; }   // UTC
}