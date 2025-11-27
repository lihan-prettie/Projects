async function initScheduleCalendar(containerId, role) {
    const calendarEl = document.getElementById(containerId);

    const today = new Date();
    const currentYear = today.getFullYear();
    const nextYear = currentYear + 1;
    const nextMonth = today.getMonth() + 1;
    const nextMonthYear = today.getFullYear() + Math.floor(nextMonth / 12);

    // 載入假日
    const [data1, data2] = await Promise.all([
        fetch(`/api/Holiday/${currentYear}`).then(res => res.json()),
        fetch(`/api/Holiday/${nextYear}`).then(res => res.json())
    ]);
    const data = [...data1, ...data2];

    const calendar = new FullCalendar.Calendar(calendarEl, {
        themeSystem: "bootstrap5",
        locale: "zh-tw",
        initialView: "dayGridMonth",
        selectable: true,
        headerToolbar: {
            left: "prev today next",
            center: "title",
            right: "dayGridMonth,listMonth"
        },
        buttonText: { today: "今天" },
        titleFormat: (date) => `${date.date.year}年${date.date.month + 1}月 班表`,

        eventClick: function (info) {
            const event = info.event;
            const workId = event.extendedProps.workId;
            const date = event.startStr;

            // 🧩 用 AJAX 載入局部視圖
            $("#modalContainer").load(`/Schedule/EditWorkPartial?workId=${workId}&date=${date}`, function () {
                const modal = new bootstrap.Modal(document.getElementById("addWorkModal"));
                modal.show();
            });
        },

        // 標註節日
        dayCellDidMount: (info) => {
            const date = new Date(info.date.getTime() - info.date.getTimezoneOffset() * 60000);
            const dateStr = date.toISOString().slice(0, 10).replace(/-/g, "");
            const holiday = data.find(h => h["西元日期"] === dateStr);
            if (holiday && holiday["是否放假"] === "2") {
                info.el.style.backgroundColor = holiday["星期"] === "六" || holiday["星期"] === "日" ? "#fffacd" : "#ffe4b5";
                if (holiday["備註"]) {
                    const remarkEl = document.createElement("div");
                    remarkEl.textContent = holiday["備註"];
                    remarkEl.style.fontSize = "1rem";
                    remarkEl.style.color = "#b8860b";
                    remarkEl.style.fontWeight = "600";
                    remarkEl.style.position = "absolute";
                    const frame = info.el.querySelector(".fc-daygrid-day-frame");
                    frame.style.position = "relative";
                    frame.appendChild(remarkEl);
                }
            }
        }
    });

    calendar.render();
}
