using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopping.Models;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Shopping.Controllers
{
    [ApiController]
    [Route("api/orders/{orderId}/payment")]
    public class OrderPaymentsController : ControllerBase
    {
        private readonly ShoppingContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<OrderPaymentsController> _logger;

        public OrderPaymentsController(ShoppingContext context, IConfiguration config, ILogger<OrderPaymentsController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        // ===============================
        // 1️⃣ 建立付款流程（Pending → 綠界）
        // POST /api/orders/{orderId}/payment
        // ===============================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePayment(int orderId)
        {
            var memberId = int.Parse(User.FindFirst("MemberId")!.Value);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.MemberId == memberId);

            if (order == null)
                return NotFound(new { message = "找不到訂單" });

            if (order.PaymentStatus != "Pending" && order.PaymentStatus != "待付款")
                return BadRequest(new { message = "訂單狀態不允許付款" });

            // 建立綠界參數
            var tradeNo = $"ORD{order.OrderId}{DateTime.Now:MMddHHmmss}";

            var paymentData = new Dictionary<string, string>
            {
                { "MerchantID", _config["ECPay:MerchantID"] },
                { "MerchantTradeNo", tradeNo },
                { "MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") },
                { "PaymentType", "aio" },
                { "TotalAmount", ((int)order.TotalAmount).ToString() },
                { "TradeDesc", "商城訂單付款" },
                { "ItemName", $"訂單編號 {order.OrderId}" },
                { "ReturnURL", $"{Request.Scheme}://{Request.Host}/api/payment/callback" },
                { "ClientBackURL", $"{Request.Scheme}://{Request.Host}/Order/Success?orderId={orderId}" },
                { "ChoosePayment", "ALL" },
                { "EncryptType", "1" }
            };

            paymentData["CheckMacValue"] = GenerateCheckMacValue(paymentData);

            return Ok(new
            {
                paymentUrl = _config["ECPay:PaymentUrl"],
                formData = paymentData
            });
        }

        // ===============================
        // 2️⃣ 綠界付款結果回呼（改成 Paid）
        // POST /api/payment/callback
        // ===============================
        [HttpPost("/api/payment/callback")]
        [AllowAnonymous] // 綠界不會帶 JWT
        public async Task<IActionResult> PaymentCallback()
        {
            try
            {
                var form = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());

                _logger.LogInformation("收到綠界回調，參數數量: {Count}", form.Count);
                _logger.LogInformation("回調參數: {Params}", string.Join(", ", form.Select(kv => $"{kv.Key}={kv.Value}")));

                if (!IsCheckMacValueValid(form))
                {
                    _logger.LogWarning("CheckMacValue 驗證失敗");
                    return Content("0|CheckMacValue Error");
                }

                // 從 MerchantTradeNo 提取 orderId（格式：ORD{orderId}{MMddHHmmss}）
                int? extractedOrderId = null;
                if (form.TryGetValue("MerchantTradeNo", out var merchantTradeNo))
                {
                    _logger.LogInformation("MerchantTradeNo: {TradeNo}", merchantTradeNo);

                    // MerchantTradeNo 格式：ORD{orderId}{MMddHHmmss}
                    // 例如：ORD1231225123456，orderId 是 123
                    // 時間戳固定10位（MMddHHmmss），從末尾倒數10位就是時間戳
                    if (merchantTradeNo.StartsWith("ORD") && merchantTradeNo.Length > 13) // 至少 "ORD" + 1位orderId + 10位時間戳
                    {
                        var remaining = merchantTradeNo.Substring(3); // 移除 "ORD" 前綴
                        // 時間戳是固定的10位，從末尾倒數10位
                        if (remaining.Length >= 10)
                        {
                            var orderIdStr = remaining.Substring(0, remaining.Length - 10); // 前面部分是 orderId
                            if (int.TryParse(orderIdStr, out var parsedOrderId))
                            {
                                extractedOrderId = parsedOrderId;
                                _logger.LogInformation("從 MerchantTradeNo 提取的 orderId: {OrderId}", extractedOrderId);
                            }
                        }
                    }
                }

                // 驗證 orderId 是否有效
                if (!extractedOrderId.HasValue || extractedOrderId.Value <= 0)
                {
                    _logger.LogWarning("無法從 MerchantTradeNo 提取有效的 orderId");
                    // 如果無法確定 orderId，記錄錯誤但仍返回成功給綠界
                    // 避免綠界重複發送回調
                    return Content("1|OK");
                }

                var targetOrderId = extractedOrderId.Value;

                // RtnCode = 1 代表付款成功
                if (form.TryGetValue("RtnCode", out var rtnCode))
                {
                    // 特別標記 RtnCode，方便查看
                    _logger.LogInformation("========== RtnCode: {RtnCode} ==========", rtnCode);
                    Console.WriteLine($"========== 綠界回調 RtnCode: {rtnCode} ==========");

                    if (rtnCode == "1")
                    {
                        var order = await _context.Orders.FindAsync(targetOrderId);
                        if (order != null)
                        {
                            _logger.LogInformation("找到訂單 {OrderId}，當前狀態: {Status}", targetOrderId, order.PaymentStatus);

                            if (order.PaymentStatus == "Pending" || order.PaymentStatus == "待付款")
                            {
                                order.PaymentStatus = "已付款";
                                await _context.SaveChangesAsync();
                                _logger.LogInformation("訂單 {OrderId} 狀態已更新為 已付款", targetOrderId);
                            }
                            else
                            {
                                _logger.LogWarning("訂單 {OrderId} 狀態不是 Pending 或 待付款，當前狀態: {Status}", targetOrderId, order.PaymentStatus);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("找不到訂單 {OrderId}", targetOrderId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("付款失敗，RtnCode: {RtnCode}", rtnCode);
                    }
                }
                else
                {
                    _logger.LogWarning("回調中沒有 RtnCode 參數");
                }

                // 一定要回這個給綠界
                return Content("1|OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理綠界回調時發生錯誤");
                // 即使發生錯誤，也要返回成功給綠界，避免重複回調
                return Content("1|OK");
            }
        }

        // ===============================
        // 3️⃣ 查詢付款狀態
        // GET /api/orders/{orderId}/payment
        // ===============================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaymentStatus(int orderId)
        {
            var memberId = int.Parse(User.FindFirst("MemberId")!.Value);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.MemberId == memberId);

            if (order == null)
                return NotFound();

            return Ok(new
            {
                orderId = order.OrderId,
                paymentStatus = order.PaymentStatus
            });
        }

        // ===============================
        // 4️⃣ 調試用：查看回調數據（僅開發環境）
        // GET /api/payment/debug
        // ===============================
        [HttpGet("/api/payment/debug")]
        [AllowAnonymous]
        public IActionResult DebugCallback()
        {
            // 這個方法用於測試，顯示回調的格式
            var sampleData = new
            {
                message = "這是調試頁面，用於查看回調數據格式",
                note = "實際的回調數據會顯示在控制台日誌中",
                callbackUrl = "/api/payment/callback",
                expectedFields = new
                {
                    MerchantTradeNo = "ORD{orderId}{MMddHHmmss}",
                    RtnCode = "1 (成功) 或其他錯誤代碼",
                    CheckMacValue = "驗證碼",
                    PaymentType = "付款類型",
                    TradeAmt = "交易金額"
                },
                howToView = new[]
                {
                    "1. 在 Visual Studio 中查看「輸出」視窗",
                    "2. 選擇「顯示輸出來源：偵錯」",
                    "3. 執行綠界支付測試",
                    "4. 查看日誌中的 'RtnCode' 信息"
                }
            };

            return Ok(sampleData);
        }

        // ===============================
        // ⚠️ 開發用：手動標記為已付款
        // POST /api/orders/{orderId}/payment/mock-paid
        // ===============================
        [HttpPost("mock-paid")]
        [Authorize]
        public async Task<IActionResult> MockPaid(int orderId)
        {
            var memberId = int.Parse(User.FindFirst("MemberId")!.Value);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.MemberId == memberId);

            if (order == null)
                return NotFound(new { message = "找不到訂單" });

            if (order.PaymentStatus == "已付款")
                return Ok(new { message = "訂單已是已付款狀態" });

            order.PaymentStatus = "已付款";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "（開發用）訂單已標記為已付款",
                orderId = order.OrderId
            });
        }


        // ===============================
        // 🔐 CheckMacValue 工具
        // ===============================
        private string GenerateCheckMacValue(Dictionary<string, string> data)
        {
            var hashKey = _config["ECPay:HashKey"];
            var hashIV = _config["ECPay:HashIV"];

            var raw = string.Join("&", data
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}"));

            var encode = $"HashKey={hashKey}&{raw}&HashIV={hashIV}";
            var urlEncode = HttpUtility.UrlEncode(encode).ToLower();

            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(urlEncode));
            return BitConverter.ToString(bytes).Replace("-", "").ToUpper();
        }

        private bool IsCheckMacValueValid(Dictionary<string, string> data)
        {
            if (!data.TryGetValue("CheckMacValue", out var mac))
                return false;

            data.Remove("CheckMacValue");
            var genMac = GenerateCheckMacValue(data);
            return mac == genMac;
        }
    }
}
