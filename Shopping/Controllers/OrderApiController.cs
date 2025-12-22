using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopping.Models;
using Shopping.Models.DTOs;

namespace Shopping.Controllers
{
    [Authorize]
    [Route("api/orders")]
    [ApiController]
    public class OrderApiController : ControllerBase
    {
        private readonly ShoppingContext _context;
        public OrderApiController(ShoppingContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var memberId = int.Parse(User.FindFirst("MemberId")!.Value);
            var order = await _context.Orders.Where(w => w.MemberId == memberId).OrderByDescending(o => o.CreatedAt).Select(s => new OrderListResponse
            {
                OrderId = s.OrderId,
                CreatedAt = s.CreatedAt,
                TotalAmount = s.TotalAmount,
                PaymentStatus = s.PaymentStatus
            }).ToListAsync();

            return Ok(order);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            var memberId = int.Parse(User.FindFirst("MemberId")!.Value);
            var order = await _context.Orders.Include(i => i.OrderItems).ThenInclude(c => c.Product).Where(c => c.OrderId == orderId && c.MemberId==memberId).Select(s => new OrderDetailResponse
            {
                OrderId = s.OrderId,
                CreatedAt = s.CreatedAt,
                TotalAmount = s.TotalAmount,
                PaymentStatus = s.PaymentStatus,
                Items = s.OrderItems.Select(i => new OrderItemDto
                {
                    ProductName = i.Product.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            }).FirstOrDefaultAsync();

            if(order == null)
            {
                return NotFound(new { success = false, message = "找不到訂單" });
            }
            return Ok(order);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder()
        {
            var memberId = int.Parse(User.FindFirst("MemberId")!.Value);

            var cart = await _context.Carts.Include(a=>a.CartItems).ThenInclude(q=>q.Product).FirstOrDefaultAsync(c => c.MemberId == memberId);
            if (cart == null || !cart.CartItems.Any())
            {
                return BadRequest(new {success = false,message= "購物車是空的，無法結帳" });
            }

            var order = new Order
            {
                MemberId = memberId,
                CreatedAt = DateTime.UtcNow,
                PaymentStatus = "Pending",
                TotalAmount = cart.CartItems.Sum(c =>c.Quantity * (c.Product?.Price??0)),
                OrderItems = cart.CartItems.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    UnitPrice = c.Product.Price,
                }).ToList()
            };

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
            return Ok(new { success = true, message = "訂單建立成功", orderId = order.OrderId });
        }
    }
}