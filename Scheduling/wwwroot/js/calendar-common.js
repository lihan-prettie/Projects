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
    const holidays = [...data1, ...data2];

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

        // 🔁 事件來源改為函式：依角色取 API & 生成 title
        events: async (info, successCallback, failureCallback) => {
            try {
                let url = "";
                if (role === 1) { // boss
                    url = `/Boss/GetAllSchedules?year=${info.start.getFullYear()}`;
                } else {
                    url = `/api/ScheduleApi/GetSchedules`;
                }

                const res = await fetch(url);
                const data = await res.json();

                const events = data.map(e => {
                    // 🎨 決定顏色邏輯
                    let bgColor = "#BEBEBE"; // 預設灰色（無人預約）
                    if (e.userId) {
                        if (e.userId === currentUserId) bgColor = "#FFA500"; // 橘色：自己預約
                        else bgColor = "#FFD700"; // 黃色：他人預約
                    }

                    // 🎯 組成標題文字
                    const title =
                        role === 1
                            ? `${e.workName ?? e.userName ?? "工作"} (${e.userId ?? "-"})`
                            : `${e.workName ?? "工作"}`;

                    return {
                        id: e.scheduleId,
                        title: title,
                        start: e.startTime,
                        end: e.endTime,
                        backgroundColor: bgColor,
                        extendedProps: {
                            scheduleId: e.scheduleId,
                            userId: e.userId,
                            workName: e.workName,
                            userName: e.userName,
                            startTime: e.startTime,
                            endTime: e.endTime
                        }
                    };
                });


                successCallback(events);
            } catch (err) {
                console.error(err);
                failureCallback(err);
            }
        },


        // 📌 點擊事件 — 依角色分流
        eventClick: (info) => {
            if (role === 1) {
                // 🧑‍💼 boss 不可編輯，只提示哪位員工的工作
                const { workName, userName, userId } = info.event.extendedProps;
                Swal.fire({
                    title: `${workName || "未命名工作"}`,
                    text: `員工ID: ${userId || "-"} ${userName ? `(${userName})` : ""}`,
                    icon: "info",
                    confirmButtonText: "確認"
                });
                return;
            }
            // 🚫 Manager 以外角色禁止預約出差
            if (info.event.extendedProps.workName?.includes("出差")) {
                if (role !== 2) {
                    Swal.fire("無法預約", "只有主管可預約出差班別", "warning");
                    return;
                }
            }
            // 👩‍💻 其他角色可以編輯
            const scheduleId = info.event.id || info.event.extendedProps.scheduleId;
            if (!scheduleId) {
                Swal.fire("錯誤", "無法讀取班表代號", "error");
                return;
            }

            $("#modalContainer").load(`/Schedule/EditWorkPartial?scheduleId=${scheduleId}`, function () {
                const modal = new bootstrap.Modal(document.getElementById("addWorkModal"));
                modal.show();
                setTimeout(() => {
                    if (typeof updateMapPreview === "function") updateMapPreview();
                }, 300);
            });
        },


        // 📅 標註節日背景（保留）
        dayCellDidMount: (info) => {
            const date = new Date(info.date.getTime() - info.date.getTimezoneOffset() * 60000);
            const dateStr = date.toISOString().slice(0, 10).replace(/-/g, "");
            const holiday = holidays.find(h => h["西元日期"] === dateStr);
            if (holiday && holiday["是否放假"] === "2") {
                info.el.style.backgroundColor =
                    holiday["星期"] === "六" || holiday["星期"] === "日"
                        ? "#fffacd"
                        : "#ffe4b5";

                if (holiday["備註"]) {
                    const remarkEl = document.createElement("div");
                    remarkEl.textContent = holiday["備註"];
                    remarkEl.classList.add("holiday-label");
                    const frame = info.el.querySelector(".fc-daygrid-day-frame");
                    frame.appendChild(remarkEl);
                }
            }
        },

        // 🎨 自訂事件外觀 — Boss 不顯示時間；Manager/Employee 顯示時間（保留）
        eventContent: function (arg) {
            if (arg.view.type !== "listMonth") {
                const { title, extendedProps } = arg.event;
                const time = `${extendedProps.startTime?.substring(11, 16)} ~ ${extendedProps.endTime?.substring(11, 16)}`;

                // 🧑‍💼 Boss 不顯示時間
                if (role === 1) {
                    return {
                        html: `
                    <div class="fc-custom-event p-1 rounded text-white" 
                         style="font-size:0.9rem; background-color:${arg.event.backgroundColor};">
                        <strong>${title}</strong>
                    </div>`
                    };
                }

                // 👩‍💻 Manager / Employee 顯示時間
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


        // ✅ 滑鼠懸停提示 — 保留
        eventDidMount: (info) => {
            const { startTime, endTime, workName, userName, userId } = info.event.extendedProps;
            if (role === 1) {
                info.el.title = `${workName || info.event.title}（UserId: ${userId ?? "-"}）`;
            } else {
                info.el.title = `${info.event.title} (${startTime ?? ""} ~ ${endTime ?? ""})`;
            }
        },
        datesSet: async (info) => {
            const year = info.start.getFullYear();
            const month = info.start.getMonth() + 1;
            if (typeof loadStatistics === "function") {
                await loadStatistics(year, month);
            }
        },

    });

    calendar.render();
}

// Boss 用的配色：同一人同色（可保留或自訂）
function getColorByUser(userId) {
    const palette = ["#007083", "#f5b301", "#7b68ee", "#ff6347", "#3cb371", "#20b2aa"];
    const idx = (userId ?? 0) % palette.length;
    return palette[idx];
}


// 🎨 Boss 顏色分配
function getColorByUser(userId) {
    const palette = ["#007083", "#f5b301", "#7b68ee", "#ff6347", "#3cb371", "#20b2aa"];
    return palette[userId % palette.length];
}


// 🎨 Boss 事件顏色分配
function getColorByUser(userId) {
    const palette = ["#007083", "#f5b301", "#7b68ee", "#ff6347", "#3cb371", "#20b2aa"];
    return palette[userId % palette.length];
}
