// FinTrack — small progressive enhancements. Pages work without this file.

const ftAnimate = document.documentElement.classList.contains("ft-anim");

// ---------------------------------------------------------------------------
// Staggered entrance: cards, rows and list items appear one after another.
// ---------------------------------------------------------------------------
if (ftAnimate) {
    const content = document.querySelector(".ft-content, .ft-public-main");

    if (content) {
        const blocks = content.querySelectorAll(
            ".ft-alert, .ft-page-head, .ft-stat-grid > *, .ft-row > *, " +
            ".ft-content > .ft-card, .ft-account-grid > *, .ft-form-card, .ft-auth, .ft-state"
        );

        blocks.forEach((el, i) => {
            el.classList.add("ft-reveal");
            el.style.setProperty("--ft-delay", `${Math.min(i, 10) * 60}ms`);
        });

        // Table rows and list items slide in a little after their card
        content.querySelectorAll(".ft-table tbody tr, .ft-list li, .ft-donut-legend li, .ft-auth-points li")
            .forEach((el, i) => {
                if (i > 14) return;
                el.classList.add("ft-reveal-x");
                el.style.setProperty("--ft-delay", `${250 + i * 40}ms`);
            });

        // Chart bars grow month by month
        content.querySelectorAll(".ft-bar-group").forEach((group, i) => {
            group.querySelectorAll(".ft-bar").forEach((bar, j) => {
                bar.style.setProperty("--ft-delay", `${i * 70 + j * 40}ms`);
            });
        });

        // Draw attention to the main action of an empty page
        content.querySelectorAll(".ft-empty .ft-btn-primary").forEach((btn) => {
            btn.classList.add("ft-primary-pulse");
        });
    }
}

// ---------------------------------------------------------------------------
// Count-up numbers: <span data-count="1234.5" data-prefix="+" data-decimals="2">
// The server-rendered text stays as the no-JS fallback.
// ---------------------------------------------------------------------------
(() => {
    const items = document.querySelectorAll("[data-count]");
    if (!items.length) return;

    const formatters = {};
    const fmt = (value, decimals) => {
        formatters[decimals] ??= new Intl.NumberFormat("en-US", {
            minimumFractionDigits: decimals,
            maximumFractionDigits: decimals
        });
        return formatters[decimals].format(value);
    };

    const paint = (el, value) => {
        const decimals = Number(el.dataset.decimals ?? 2);
        const prefix = el.dataset.prefix ?? "";

        if (el.dataset.format === "hero") {
            const negative = value < 0;
            const abs = Math.abs(value);
            const whole = Math.trunc(abs);
            const cents = (abs - whole).toFixed(2).slice(1);
            el.replaceChildren(
                document.createTextNode((negative ? "-" : "") + fmt(whole, 0)),
                Object.assign(document.createElement("span"), { className: "ft-cents", textContent: cents })
            );
            return;
        }

        el.textContent = prefix + fmt(value, decimals);
    };

    if (!ftAnimate) return;

    const duration = 1100;
    const ease = (x) => 1 - Math.pow(1 - x, 4);

    items.forEach((el, index) => {
        const target = Number(el.dataset.count);
        if (!Number.isFinite(target) || target === 0) return;

        const delay = 150 + Math.min(index, 8) * 60;
        paint(el, 0);

        setTimeout(() => {
            const start = performance.now();
            const tick = (now) => {
                const p = Math.min(1, (now - start) / duration);
                paint(el, target * ease(p));
                if (p < 1) requestAnimationFrame(tick);
                else paint(el, target);
            };
            requestAnimationFrame(tick);
        }, delay);
    });
})();

// ---------------------------------------------------------------------------
// Success alerts collapse away after a few seconds.
// ---------------------------------------------------------------------------
document.querySelectorAll(".ft-alert-success").forEach((alert) => {
    setTimeout(() => {
        alert.style.maxHeight = alert.scrollHeight + "px";
        requestAnimationFrame(() => alert.classList.add("is-leaving"));
        setTimeout(() => alert.remove(), 700);
    }, 5000);
});

// ---------------------------------------------------------------------------
// Client-side filter for tables marked with [data-filter-scope]:
//   buttons  [data-filter="all|<type>"]  match rows by [data-type]
//   input    [data-filter-search]         matches rows by [data-search]
// ---------------------------------------------------------------------------
document.querySelectorAll("[data-filter-scope]").forEach((scope) => {
    const buttons = scope.querySelectorAll("[data-filter]");
    const search = scope.querySelector("[data-filter-search]");
    const rows = scope.querySelectorAll("tbody tr");
    const empty = scope.querySelector("[data-filter-empty]");
    let activeType = "all";

    const apply = () => {
        const term = (search?.value || "").trim().toLowerCase();
        let visible = 0;

        rows.forEach((row) => {
            const typeOk = activeType === "all" || row.dataset.type === activeType;
            const textOk = !term || (row.dataset.search || "").includes(term);
            const show = typeOk && textOk;

            if (show && row.hidden) {
                row.classList.remove("ft-reveal-x", "ft-row-in");
                void row.offsetWidth; // restart the fade
                row.classList.add("ft-row-in");
            }

            row.hidden = !show;
            if (show) visible++;
        });

        if (empty) empty.hidden = visible !== 0;
    };

    buttons.forEach((button) => {
        button.addEventListener("click", () => {
            activeType = button.dataset.filter;
            buttons.forEach((b) => {
                const on = b === button;
                b.classList.toggle("is-active", on);
                b.setAttribute("aria-pressed", on ? "true" : "false");
            });
            apply();
        });
    });

    search?.addEventListener("input", apply);
});

// ---------------------------------------------------------------------------
// Currency label next to an amount field follows the selected account.
//   select [data-currency-source]  — its value is either a currency code, or
//          an account id looked up in <script id="accountCurrencies"> JSON
//   span   [data-currency-label]   — shows the resolved currency code
// ---------------------------------------------------------------------------
document.querySelectorAll("[data-currency-source]").forEach((source) => {
    const label = source.form?.querySelector("[data-currency-label]");
    if (!label) return;

    let map = null;
    const json = document.getElementById("accountCurrencies");
    if (json) {
        try { map = JSON.parse(json.textContent); } catch { map = null; }
    }

    const update = () => {
        label.textContent = map ? (map[source.value] ?? "") : source.value;
    };

    source.addEventListener("change", update);
    update();
});
