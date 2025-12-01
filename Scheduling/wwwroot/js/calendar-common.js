async function initScheduleCalendar(containerId, role) {
    console.log('=== initScheduleCalendar 開始 ===');
    console.log('containerId:', containerId);
    console.log('role:', role);

    const calendarEl = document.getElementById(containerId);
    console.log('calendarEl:', calendarEl);

    // ✅ 檢查容器是否存在
    if (!calendarEl) {
        console.error(`找不到 ID 為 ${containerId} 的元素`);
        console.log('document.body:', document.body);
        return null;
    }

    console.log('容器找到，開始載入假日資料...');

    const today = new Date();
    const currentYear = today.getFullYear();
    const nextYear = currentYear + 1;

    try {
        // 📅 載入假日資料（兩年份）
        const [data1, data2] = await Promise.all([
            fetch(`/api/Holiday/${currentYear}`).then(res => {
                console.log(`Holiday API ${currentYear} 回應:`, res.status);
                return res.json();
            }),
            fetch(`/api/Holiday/${nextYear}`).then(res => {
                console.log(`Holiday API ${nextYear} 回應:`, res.status);
                return res.json();
            })
        ]);
        const holidays = [...data1, ...data2];
        console.log('假日資料載入完成，數量:', holidays.length);

        // 🧩 取得登入使用者 ID
        const currentUserId = parseInt(document.getElementById("currentUserId")?.value || 0);
        console.log('currentUserId:', currentUserId);

        // ✅ 檢查 FullCalendar 是否已載入
        if (typeof FullCalendar === 'undefined') {
            console.error('❌ FullCalendar 未定義！');
            return null;
        }
        console.log('✅ FullCalendar 已載入');

        const calendar = new FullCalendar.Calendar(calendarEl, {
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

            events: async (info, successCallback, failureCallback) => {
                console.log('📅 events 回調被觸發');
                console.log('日期範圍:', info.start, 'to', info.end);

                try {
                    let url = "";
                    if (role === 1) {
                        const startStr = info.start.toISOString();
                        const endStr = info.end.toISOString();
                        url = `/Boss/GetAllSchedules?start=${startStr}&end=${endStr}`;
                        console.log('Boss API URL:', url);
                    } else {
                        url = `/api/ScheduleApi/GetSchedules`;
                        console.log('Employee API URL:', url);
                    }

                    const res = await fetch(url);
                    console.log('API 回應狀態:', res.status);

                    if (!res.ok) {
                        throw new Error(`HTTP ${res.status}: ${res.statusText}`);
                    }

                    const data = await res.json();
                    console.log('取得的事件數量:', data.length);
                    console.log('事件資料:', data);

                    const events = data.map(e => {
                        let bgColor = "#BEBEBE";
                        if (e.userId) {
                            if (e.userId === currentUserId) bgColor = "#FFA500";
                            else bgColor = "#FFD700";
                        }

                        const title = role === 1
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

                    console.log('處理後的事件:', events);
                    successCallback(events);
                } catch (err) {
                    console.error('❌ events 回調錯誤:', err);
                    failureCallback(err);
                }
            },

            eventClick: (info) => {
                if (role === 1) {
                    const { workName, userName, userId } = info.event.extendedProps;
                    Swal.fire({
                        title: `${workName || "未命名工作"}`,
                        text: `員工ID: ${userId || "-"} ${userName ? `(${userName})` : ""}`,
                        icon: "info",
                        confirmButtonText: "確認"
                    });
                    return;
                }
                if (info.event.extendedProps.workName?.includes("出差")) {
                    if (role !== 2) {
                        Swal.fire("無法預約", "只有主管可預約出差班別", "warning");
                        return;
                    }
                }
                const scheduleId = info.event.id || info.event.extendedProps.scheduleId;
                if (!scheduleId) {
                    Swal.fire("錯誤", "無法讀取班表代號", "error");
                    return;
                }
                const eventDate = new Date(info.event.start);
                // 🧭 今天
                const today = new Date();
                let nextMonth = today.getMonth() + 1;
                let nextYear = today.getFullYear();
                if (nextMonth === 12) {
                    nextMonth = 0;
                    nextYear++;
                }

                // ✅ 判斷事件是否屬於下一個月
                const isNextMonth =
                    eventDate.getFullYear() === nextYear &&
                    eventDate.getMonth() === nextMonth;

                // 📤 帶上 readonly 參數 (true = 唯讀)
                const readonly = isNextMonth ? "false" : "true";

                $("#modalContainer").load(`/Schedule/EditWorkPartial?scheduleId=${scheduleId}&readonly=${readonly}`, function () {
                    const modal = new bootstrap.Modal(document.getElementById("addWorkModal"));
                    modal.show();

                    setTimeout(() => {
                        if (typeof updateMapPreview === "function") updateMapPreview();
                    }, 300);
                });
                
            },

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
                        if (frame) frame.appendChild(remarkEl);
                    }
                }
            },

            eventContent: function (arg) {
                if (arg.view.type !== "listMonth") {
                    const { title, extendedProps } = arg.event;
                    const time = `${extendedProps.startTime?.substring(11, 16)} ~ ${extendedProps.endTime?.substring(11, 16)}`;

                    if (role === 1) {
                        return {
                            html: `
                        <div class="fc-custom-event p-1 rounded text-white" 
                             style="font-size:0.9rem; background-color:${arg.event.backgroundColor};">
                            <strong>${title}</strong>
                        </div>`
                        };
                    }

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

            eventDidMount: (info) => {
                const { startTime, endTime, workName, userName, userId } = info.event.extendedProps;
                if (role === 1) {
                    info.el.title = `${workName || info.event.title}（UserId: ${userId ?? "-"}）`;
                } else {
                    info.el.title = `${info.event.title} (${startTime ?? ""} ~ ${endTime ?? ""})`;
                }
            }
        });

        console.log('Calendar 物件已建立');
        console.log('開始 render...');

        calendar.render();

        console.log('✅ Calendar render 完成');
        console.log('返回 calendar 物件:', calendar);

        return calendar;

    } catch (error) {
        console.error('❌ initScheduleCalendar 發生錯誤:', error);
        console.error('錯誤堆疊:', error.stack);
        return null;
    }
}

// ✅ 只保留一個 getColorByUser 函數
function getColorByUser(userId) {
    const palette = ["#007083", "#f5b301", "#7b68ee", "#ff6347", "#3cb371", "#20b2aa"];
    const idx = (userId ?? 0) % palette.length;
    return palette[idx];
}