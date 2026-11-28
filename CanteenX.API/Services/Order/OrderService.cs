using CanteenX.API.Data;
using CanteenX.API.DTOs.Orders;
using CanteenX.API.Model;
using CanteenX.API.Services.Order;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace CanteenX.API.Services
{
    public class OrderService : IOrderService
    {
        private readonly CanteenxdbContext _context;

        public OrderService(CanteenxdbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. CREATE ORDER
        // ==========================================
        public async Task<OrderResponseDto> CreateOrderAsync(string userId,CheckoutRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Invalid user.");

            if (request == null || request.Items == null || !request.Items.Any())
                throw new ArgumentException("Your cart is empty.");

            if (request.Items.Count > 50)
                throw new ArgumentException("Maximum 50 cart entries are allowed.");

            // Combine duplicate menu items
            var requestedItems = request.Items
                .GroupBy(x => x.MenuItemId)
                .Select(g => new
                {
                    MenuItemId = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            if (requestedItems.Any(x => x.MenuItemId <= 0 ||
                                        x.Quantity <= 0 ||
                                        x.Quantity > 50))
            {
                throw new ArgumentException("Invalid cart item or quantity.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var menuItemIds = requestedItems
                    .Select(x => x.MenuItemId)
                    .ToList();

                // Fetch actual menu items and canteen details from database
                var menuItems = await _context.MenuItems
                    .Include(x => x.Canteen)
                    .Where(x => menuItemIds.Contains(x.MenuItemId))
                    .ToListAsync(cancellationToken);

                if (menuItems.Count != requestedItems.Count)
                    throw new InvalidOperationException(
                        "One or more menu items were not found.");

                var itemLookup = requestedItems.ToDictionary(
                    x => x.MenuItemId,
                    x => x.Quantity);

                // Validate availability and calculate prices from database
                var orderLines = new List<(MenuItems Item, int Quantity, decimal UnitPrice, decimal Subtotal)>();

                foreach (var item in menuItems)
                {
                    if (item.IsAvailable != true)
                        throw new InvalidOperationException(
                            $"{item.ItemName} is currently unavailable.");

                    if (item.Canteen == null || item.Canteen.IsActive != true)
                        throw new InvalidOperationException(
                            $"The canteen for {item.ItemName} is not active.");

                    if (!decimal.TryParse(
                        item.Price,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal unitPrice) || unitPrice < 0)
                    {
                        throw new InvalidOperationException(
                            $"Invalid price configured for {item.ItemName}.");
                    }

                    int quantity = itemLookup[item.MenuItemId];

                    decimal subtotal = decimal.Round(
                        unitPrice * quantity, 2, MidpointRounding.AwayFromZero);

                    orderLines.Add((item, quantity, unitPrice, subtotal));
                }

                // Calculate grand total
                decimal totalAmount = orderLines.Sum(x => x.Subtotal);

                // Create parent Order
                var order = new Model.Order
                {
                    UserId = userId,
                    TotalAmount = totalAmount.ToString(
                        "0.00", CultureInfo.InvariantCulture),
                    OrderStatus = "Pending",
                    PaymentStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);

                await _context.SaveChangesAsync(cancellationToken);

                // Group ordered items by CanteenId
                var canteenGroups = orderLines.GroupBy(
                    x => x.Item.CanteenId);

                foreach (var group in canteenGroups)
                {
                    decimal canteenSubtotal = group.Sum(x => x.Subtotal);

                    // Create one CanteenOrder per canteen
                    var canteenOrder = new CanteenOrder
                    {
                        OrderId = order.OrderId,
                        CanteenId = group.Key,
                        OrderStatus = "Pending",
                        Subtotal = canteenSubtotal.ToString(
                            "0.00", CultureInfo.InvariantCulture),
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.CanteenOrders.Add(canteenOrder);

                    await _context.SaveChangesAsync(cancellationToken);

                    // Create individual OrderItems
                    foreach (var line in group)
                    {
                        var orderItem = new OrderItems
                        {
                            CanteenOrderId = canteenOrder.CanteenOrderId,
                            MenuItemId = line.Item.MenuItemId,
                            ItemName = line.Item.ItemName,
                            UnitPrice = line.UnitPrice.ToString(
                                "0.00", CultureInfo.InvariantCulture),
                            Quantity = line.Quantity,
                            SubTotal = line.Subtotal.ToString("0.00",CultureInfo.InvariantCulture)
                        };

                        _context.OrderItems.Add(orderItem);
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                return new OrderResponseDto
                {
                    OrderId = order.OrderId,
                    TotalAmount = order.TotalAmount,
                    OrderStatus = order.OrderStatus,
                    PaymentStatus = order.PaymentStatus,
                    CreatedAt = order.CreatedAt,
                    Message = "Order created successfully. Proceed to payment."
                };
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        // ==========================================
        // 2. GET ORDER BY ID
        // ==========================================
        public async Task<OrderDetailsResponseDto?> GetOrderByIdAsync(int orderId,string userId,CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(x => x.CanteenOrders)
                    .ThenInclude(x => x.Canteen)
                .Include(x => x.CanteenOrders)
                    .ThenInclude(x => x.OrderItems)
                .FirstOrDefaultAsync(
                    x => x.OrderId == orderId && x.UserId == userId,
                    cancellationToken);

            if (order == null)
                return null;

            return new OrderDetailsResponseDto
            {
                OrderId = order.OrderId,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                CreatedAt = order.CreatedAt,

                CanteenOrders = order.CanteenOrders.Select(co =>
                    new CanteenOrderResponseDto
                    {
                        CanteenOrderId = co.CanteenOrderId,
                        CanteenId = co.CanteenId,
                        CanteenName = co.Canteen.CanteenName,
                        SubTotal = co.Subtotal,
                        OrderStatus = co.OrderStatus,

                        Items = co.OrderItems.Select(oi =>
                            new OrderItemResponseDto
                            {
                                OrderItemId = oi.OrderItemId,
                                MenuItemId = oi.MenuItemId,
                                ItemName = oi.ItemName,
                                UnitPrice = oi.UnitPrice ?? "0.00",
                                Quantity = oi.Quantity,
                                SubTotal = oi.SubTotal
                            }).ToList()
                    }).ToList()
            };
        }

        // ==========================================
        // 3. GET MY ORDERS
        // ==========================================
        public async Task<List<OrderDetailsResponseDto>> GetMyOrdersAsync(string userId,CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Invalid Users..");

      
            return await _context.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(order => new OrderDetailsResponseDto
            {

            OrderId = order.OrderId,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            PaymentStatus = order.PaymentStatus,
            CreatedAt = order.CreatedAt,
            CanteenOrders = order.CanteenOrders.Select(co => new CanteenOrderResponseDto
            {

                CanteenOrderId = co.CanteenOrderId,
                CanteenId = co.CanteenId,
                CanteenName = co.Canteen != null ? co.Canteen.CanteenName : "Canteen",
                SubTotal = co.Subtotal,
                OrderStatus = co.OrderStatus,
                Items = co.OrderItems.Select(oi => new OrderItemResponseDto
                {
                    OrderItemId = oi.OrderItemId,
                    MenuItemId = oi.MenuItemId,
                    ItemName = oi.ItemName,
                    UnitPrice = oi.UnitPrice ?? "0.00",
                    Quantity = oi.Quantity,
                    SubTotal = oi.SubTotal
                }).ToList()
            }).ToList()
        })
        .ToListAsync(cancellationToken);
        }

        // ==========================================
        // 4. CANCEL ORDER
        // ==========================================
        public async Task<bool> CancelOrderAsync(int orderId,string userId,CancellationToken cancellationToken = default)
        {
            var order = await _context.Orders
                .Include(x => x.CanteenOrders)
                .FirstOrDefaultAsync(
                    x => x.OrderId == orderId && x.UserId == userId,
                    cancellationToken);

            if (order == null)
                return false;

            // Only unpaid and pending orders can be cancelled here
            if (order.OrderStatus != "Pending" ||
                order.PaymentStatus != "Pending")
            {
                return false;
            }

            order.OrderStatus = "Cancelled";
            order.UpdatedAt = DateTime.UtcNow;

            foreach (var canteenOrder in order.CanteenOrders)
            {
                canteenOrder.OrderStatus = "Cancelled";
                canteenOrder.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
