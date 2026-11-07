namespace Orders.API.DTOs;

public class ProductResponse
{
    public Guid Id { get; set; }

    public decimal Precio { get; set; }

    public int Stock { get; set; }
}