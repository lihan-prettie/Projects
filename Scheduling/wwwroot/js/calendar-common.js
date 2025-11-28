async function initScheduleCalendar(containerId, role) {
    const calendarEl = document.getElementById(containerId);

    const today = new Date();
    const currentYear = today.getFullYear();
    const nextYear = currentYear + 1;
    const nextMonth = today.getMonth() + 1;
    const nextMonthYear = today.getFullYear() + Math.floor(nextMonth / 12);

    // 📅 載入假日資料（兩年份）
    const [data1, data2] = await Promise.all([
        fetch(`/api/Holiday/${currentYear}`).then(res => res.json()),
        fetch(`/api/Holiday/${nextYear}`).then(res => res.json())
    ]);
    const data = [...data1, ...data2];

    // 🧩 取得登入使用者 ID（從後端 ViewData 或 hidden input 帶入）
    const currentUserId = parseInt(document.getElementById("currentUserId")?.value || 0);

    calendar = new FullCalendar.Calendar(calendarEl, {
        themeSystem: "bootstrap5",
        contentHeight: 1000,
        locale: "zh-tw",
        initialView: "dayGridMonth",
        selectable: true,
        headerToolbar: {
            left: "prev today next",
            center: "title",
            right: "dayGridMonth,listMonth"
        },
        views: {
            listMonth: {
                noEventsText: "本月尚無班表",
                listDayFormat: { weekday: 'short', month: 'numeric', day: 'numeric' },
                listDaySideFormat: false
            }
        },
        buttonText: { today: "今天" },
        titleFormat: (date) => `${date.date.year}年${date.date.month + 1}月 班表`,
        events: "/api/ScheduleApi/GetSchedules",

        // 📌 點擊事件 — 呼叫 _EditWorkPartial
        eventClick: (info) => {
            const scheduleId = info.event.id || info.event.extendedProps.scheduleId;
            if (!scheduleId) {
                Swal.fire("錯誤", "無法讀取班表代號", "error");
                return;
            }

            $("#modalContainer").load(`/Schedule/EditWorkPartial?scheduleId=${scheduleId}`, function () {
                const modal = new bootstrap.Modal(document.getElementById("addWorkModal"));
                modal.show();
            });
        },

        // 📅 標註節日背景
        dayCellDidMount: (info) => {
            const date = new Date(info.date.getTime() - info.date.getTimezoneOffset() * 60000);
            const dateStr = date.toISOString().slice(0, 10).replace(/-/g, "");
            const holiday = data.find(h => h["西元日期"] === dateStr);
            if (holiday && holiday["是否放假"] === "2") {

                // 🎨 設定背景顏色（週末 or 節日）
                info.el.style.backgroundColor =
                    holiday["星期"] === "六" || holiday["星期"] === "日"
                        ? "#fffacd"   // 淺黃色週末
                        : "#ffe4b5";  // 節日橘黃

                if (holiday["備註"]) {
                    const remarkEl = document.createElement("div");
                    remarkEl.textContent = holiday["備註"];
                    remarkEl.classList.add("holiday-label"); // ✅ 使用統一樣式class
                    const frame = info.el.querySelector(".fc-daygrid-day-frame");
                    frame.appendChild(remarkEl);
                }

            }
        },


        // 🎨 自訂事件外觀（取消 icon，只保留顏色 + 內容）
        eventContent: function (arg) {
            if (arg.view.type !== "listMonth") {
                const { title, extendedProps } = arg.event;
                const time = `${extendedProps.startTime} ~ ${extendedProps.endTime}`;

                return {
                    html: `
                        <div class="fc-custom-event p-1 rounded text-white" 
                             style="font-size:0.9rem; background-color:${arg.event.backgroundColor};">
                            <strong>${title}</strong><br/>
                            <small>${time}</small>
                        </div>`
                };
            }
        },

        // ✅ 滑鼠懸停顯示時間提示
        eventDidMount: (info) => {
            info.el.title = `${info.event.title} (${info.event.extendedProps.startTime} ~ ${info.event.extendedProps.endTime})`;
        }
    });

    calendar.render();
}
