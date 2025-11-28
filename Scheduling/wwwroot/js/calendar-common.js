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

            // 👩‍💻 其他角色可以編輯
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
            await loadStatistics(year, month);
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

// 🧠 Boss 專用統計載入
// 🧠 Boss 專用統計載入 + 排行榜
async function loadBossStatistics(year, month) {
    const res = await fetch(`/Boss/GetMonthlyStats?year=${year}&month=${month}`);
    const data = await res.json();

    // 移除舊容器（避免重複載入）
    const existing = document.getElementById("boss-stats-container");
    if (existing) existing.remove();

    // === 統計表格區 ===
    const container = document.createElement("div");
    container.id = "boss-stats-container";
    container.classList.add("container", "mt-4");

    container.innerHTML = `
        <div class="row">
            <div class="col-md-6">
                <h4 class="mb-3">📊 員工出勤統計 (${year}年${month}月)</h4>
                <table class="table table-bordered table-hover align-middle shadow-sm">
                    <thead class="table-warning">
                        <tr>
                            <th>排名</th>
                            <th>員工</th>
                            <th>本月上班天數</th>
                            <th>本年上班天數</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${data.map((x, i) => `
                            <tr>
                                <td>${i + 1}</td>
                                <td>${x.userName}</td>
                                <td>${x.monthlyCount}</td>
                                <td>${x.yearlyCount}</td>
                            </tr>
                        `).join("")}
                    </tbody>
                </table>
            </div>
            <div class="col-md-6 d-flex flex-column align-items-center justify-content-center">
                <h5 class="text-center mb-3">🏆 本月出勤排行榜</h5>
                <canvas id="bossChart" style="max-height:350px; width:100%;"></canvas>
            </div>
        </div>
    `;

    document.querySelector("#calendar").after(container);

    // === 繪製 Chart.js 長條圖 ===
    const ctx = document.getElementById("bossChart").getContext("2d");

    // 取前10名（或全部）
    const labels = data.map(x => x.userName);
    const monthlyCounts = data.map(x => x.monthlyCount);

    const chartColors = [
        "#f5b301", "#ff9800", "#ffc107", "#ffb74d", "#ffcc80",
        "#20b2aa", "#7b68ee", "#007083", "#3cb371", "#ff6347"
    ];

    new Chart(ctx, {
        type: "bar",
        data: {
            labels: labels,
            datasets: [{
                label: "上班天數",
                data: monthlyCounts,
                backgroundColor: chartColors.slice(0, labels.length),
                borderRadius: 8
            }]
        },
        options: {
            responsive: true,
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: {
                        label: (ctx) => `${ctx.parsed.y} 天`
                    }
                }
            },
            scales: {
                x: {
                    ticks: {
                        font: { size: 14 },
                        color: "#6c757d"
                    },
                    grid: { display: false }
                },
                y: {
                    beginAtZero: true,
                    ticks: {
                        stepSize: 2,
                        font: { size: 12 },
                        color: "#6c757d"
                    },
                    title: {
                        display: true,
                        text: "天數",
                        color: "#6c757d"
                    }
                }
            }
        }
    });
}


// 🎨 Boss 事件顏色分配
function getColorByUser(userId) {
    const palette = ["#007083", "#f5b301", "#7b68ee", "#ff6347", "#3cb371", "#20b2aa"];
    return palette[userId % palette.length];
}
