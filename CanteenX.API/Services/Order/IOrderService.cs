using CanteenX.API.DTOs.Orders;

namespace CanteenX.API.Services.Order
{
    public interface IOrderService
    {
        // Create a new order from student's cart
        Task<OrderResponseDto> CreateOrderAsync(string userId,CheckoutRequestDto request,CancellationToken cancellationToken = default );
        // Get a specific order with complete details
        Task<OrderDetailsResponseDto?> GetOrderByIdAsync(int orderId,string userId,CancellationToken cancellationToken = default);

        // Get all orders of logged-in student
        Task<List<OrderResponseDto>> GetMyOrdersAsync(string userId,CancellationToken cancellationToken = default);

        // Cancel an eligible order
        Task<bool> CancelOrderAsync(int orderId,string userId,CancellationToken cancellationToken = default);


    }
}
