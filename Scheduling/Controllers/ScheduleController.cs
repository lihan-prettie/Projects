using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scheduling.Models;
using Scheduling.Models.ViewModels;

namespace Scheduling.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly SchedulingContext _context;

        public ScheduleController(SchedulingContext context)
        {
            _context = context;
        }

        // ✅ GET: Schedule/EditWorkPartial
        [HttpGet]
        public IActionResult EditWorkPartial(int scheduleId)
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null) return RedirectToAction("Index", "Login");

            var schedule = _context.Schedules
                                   .Include(s => s.Work)
                                   .FirstOrDefault(s => s.ScheduleId == scheduleId);
            if (schedule == null) return NotFound();

            // ✅ 可編輯：空班或自己的班
            bool isEditable = (schedule.UserId == null) || (schedule.UserId == currentUserId);

            // ✅ 以「這筆班表的月份」計算目前登入者的排班數
            var monthStart = new DateOnly(schedule.ScheduleDate.Year, schedule.ScheduleDate.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            int shiftCount = _context.Schedules.Count(s =>
                s.UserId == currentUserId &&
                s.ScheduleDate >= monthStart &&
                s.ScheduleDate <= monthEnd);

            // 👉 帶到 View（注意：用「資料表原始值」）
            ViewData["ShiftCount"] = shiftCount;
            ViewData["OriginalUserId"] = schedule.UserId; // 可能是 null

            var vm = new EditScheduleViewModel
            {
                ScheduleId = schedule.ScheduleId,
                WorkName = schedule.Work.WorkName,
                ScheduleDate = schedule.ScheduleDate.ToString("yyyy-MM-dd"),
                StartTime = schedule.Work.DefaultStartTime?.ToString(@"hh\:mm"),
                EndTime = schedule.Work.DefaultEndTime?.ToString(@"hh\:mm"),
                WorkLocation = schedule.Work.WorkLocation,
                WorkNote = schedule.Work.WorkNote,
                Status = schedule.Work.IsActive ? "Active" : "Inactive",

                // 注意：這裡為了 UX 會把空班預帶成 currentUserId，但請**不要**用它判斷是否可釋出
                UserId = schedule.UserId ?? currentUserId,
                CreatedBy = currentUserId.Value,
                IsEditable = isEditable
            };

            return PartialView("~/Views/PartialView/_EditWorkPartial.cshtml", vm);
        }



        // ✅ POST: Schedule/UpdateSchedule
        [HttpPost]
        public IActionResult UpdateSchedule(EditScheduleViewModel model)
        {
            var schedule = _context.Schedules.FirstOrDefault(s => s.ScheduleId == model.ScheduleId);
            if (schedule == null)
                return Json(new { success = false, message = "找不到此班表" });

            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (currentUserId == null)
                return Json(new { success = false, message = "尚未登入" });

            // 🟠 他人班 → 禁止修改
            if (schedule.UserId != null && schedule.UserId != currentUserId)
                return Json(new { success = false, message = "此班已被他人選擇，無法修改" });

            // 🟡 自己班（目前使用者佔有）
            if (schedule.UserId == currentUserId)
            {
                // 若使用者送出 UserId=0 → 釋出班表
                if (model.UserId == 0)
                {
                    schedule.UserId = null; // ✅ 改為空（釋出成功）
                    schedule.Status = "Active";
                    schedule.UpdatedBy = currentUserId;
                    _context.SaveChanges();
                    return Json(new { success = true, message = "此班已釋出" });
                }

                // 若仍為自己 → 不變
                return Json(new { success = true, message = "未修改任何資料" });
            }

            // ⚪ 空班（無人）
            if (schedule.UserId == null)
            {
                // ✅ 用 schedule 日期所在的月份做計算
                var monthStart = new DateOnly(schedule.ScheduleDate.Year, schedule.ScheduleDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // 🧮 計算該月目前使用者的排班數
                int userShiftCount = _context.Schedules.Count(s =>
                    s.UserId == currentUserId &&
                    s.ScheduleDate >= monthStart &&
                    s.ScheduleDate <= monthEnd);

                if (userShiftCount >= 15)
                {
                    return Json(new { success = false, message = "您本月已達 15 班上限，無法再排班" });
                }

                // ❌ 檢查同一天是否已有班
                bool hasSameDayShift = _context.Schedules.Any(s =>
                    s.UserId == currentUserId &&
                    s.ScheduleDate == schedule.ScheduleDate &&
                    s.ScheduleId != schedule.ScheduleId);

                if (hasSameDayShift)
                {
                    return Json(new { success = false, message = "您當天已有其他班，無法重複排班" });
                }

                // ✅ 若目前使用者送進自己的 ID → 搶班成功
                if (model.UserId == currentUserId)
                {
                    schedule.UserId = currentUserId.Value;
                    schedule.Status = "Active";
                    schedule.UpdatedBy = currentUserId;
                    _context.SaveChanges();
                    return Json(new { success = true, message = "排班成功" });
                }

                // 🟡 若送來 0 → 保持空
                return Json(new { success = true, message = "未修改任何資料" });
            }


            return Json(new { success = false, message = "未識別的狀態" });
        }
    }
}
