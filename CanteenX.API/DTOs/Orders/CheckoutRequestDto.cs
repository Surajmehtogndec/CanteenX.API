namespace CanteenX.API.DTOs.Orders
{
    public class CheckoutRequestDto
    {
        public List<CheckoutItemDto> Items { get; set; } = new();
    }
}
