using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopping.Models;
using Shopping.Models.DTOs;

namespace Shopping.Controllers
{
    [Authorize]
    [Route("api/carts")]
    [ApiController]
    public class CartApiController : ControllerBase
    {
        private readonly ShoppingContext _context;
        public CartApiController(ShoppingContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            //檢查會員
            int memberId;
            try
            {
                memberId = GetMemberId();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { success = false, message = "尚未登入或沒有會員資格" });
            }


            //檢查是否有購物車和購物車內有無商品
            var cart = await _context.Carts.Include(p => p.CartItems).ThenInclude(m => m.Product).FirstOrDefaultAsync(a => a.MemberId == memberId);
            if (cart == null || !cart.CartItems.Any())
            {
                return Ok( new { items = new List<object>(),total = 0 }); //建立一個型別是object(所有型別的父型別)的list
            }

            //把購物車商品轉成前端資料格式
            var items = cart.CartItems.Select(c => new CartItemResponse
            {
                ProductName = c.Product.ProductName,
                ProductId = c.ProductId,
                Price = c.Product.Price,
                Quantity = c.Quantity,
                Subtotal = c.Product.Price * c.Quantity
            });

            var total = items.Sum(i => i.Subtotal);
            return Ok( new{ items,total} );
        }


        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody]AddToCartRequest dto)
        {
            //檢查會員
            int memberId;
            try
            {
                memberId = GetMemberId();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { success = false, message = "尚未登入或沒有會員資格" });
            }


            //檢查該會員是否有購物車，用JWT的memebrId取得資料表的Cart，沒有就自己建立一個
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.MemberId == memberId);
            if (cart == null)
            {
                cart = new Cart
                {
                    MemberId = memberId,
                    CreatedAt = DateTime.UtcNow,
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            //檢查目標商品是否存在
            var existItem = await _context.Products.FirstOrDefaultAsync(e => e.ProductId == dto.ProductId);
            if (existItem == null)
            {
                return NotFound(new { success = false, message = "商品不存在" });
            }

            //查詢購物車是否有這件商品，有的話加數量沒有就新增
            var cartItem = await _context.CartItems.FirstOrDefaultAsync(c => c.ProductId == dto.ProductId && c.CartId == cart.CartId);
            if (dto.Quantity<=0)
            {
                return BadRequest(new { success=false,message="商品數量不可為零或以下"});
            }
            if (cartItem != null)
            {
                cartItem.Quantity += dto.Quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                };

                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { success=true,message="加入購物車成功"});
        }


        [HttpPut("items/{productId}")]
        public async Task<IActionResult> UpdateQuantity(int productId,[FromBody] UpdateCartItemRequest dto)
        {
            //檢查會員
            int memberId;
            try
            {
                memberId = GetMemberId();
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { success = false, message = "尚未登入或沒有會員資格" });
            }

            //檢查商品數量邏輯
            if (dto.Quantity <= 0) return BadRequest(new { success = false, message = "商品數量必須大為零" });

            //取得商品資料
            var cartItem = await _context.CartItems.Include(p => p.Cart).FirstOrDefaultAsync(o => o.ProductId == productId && o.Cart.MemberId == memberId);
            if (cartItem == null) {
                return NotFound(new { success = false,message = "購物車無此商品"});
            }

            cartItem.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "商品數量修改成功" });
        }

        [HttpDelete("items/{productId}")]
        public async Task<IActionResult> DeleteItem(int productId)
        {
            //檢查會員
            int memberId;
            try
            {
                memberId = GetMemberId();
            }
            catch(UnauthorizedAccessException)
            {
                return Unauthorized(new {success=false,message="尚未登入或沒有會員資格"});
            }

            //取得商品資料
            var cartItem = await _context.CartItems.Include(i => i.Cart).FirstOrDefaultAsync(o => o.ProductId == productId && o.Cart.MemberId == memberId);
            if (cartItem == null)
            {
                return NotFound(new { success = false, message = "購物車無此商品" });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "刪除商品成功" });
        }

        private int GetMemberId()
        {
            var claim = User.FindFirst("MemberId");
            if(claim == null)
            {
                throw new UnauthorizedAccessException("MemberId不存在");
            }
            return int.Parse(claim.Value);
        }
    }
}
