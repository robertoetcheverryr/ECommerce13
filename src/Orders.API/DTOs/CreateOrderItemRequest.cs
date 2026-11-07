namespace Orders.API.DTOs;

public class CreateOrderItemRequest
{
    public Guid ProductoId { get; set; }

    public int Cantidad { get; set; }
}