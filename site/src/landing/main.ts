import "./landing.css";
import "../highlight/syntax.css";
import { initSavePace } from "./savePace";
import { highlight } from "../highlight/highlighter";

initSavePace();

document.querySelectorAll<HTMLElement>("pre code").forEach((block) => {
    const match = /language-(\w+)/.exec(block.className);
    block.innerHTML = highlight(block.textContent ?? "", match ? match[1] : "");
});

const reduceMotion = window.matchMedia(
    "(prefers-reduced-motion: reduce)",
).matches;

const revealItems = document.querySelectorAll<HTMLElement>("[data-reveal]");
if (revealItems.length > 0 && "IntersectionObserver" in window && !reduceMotion) {
    const revealObserver = new IntersectionObserver(
        (entries) => {
            for (const entry of entries) {
                if (entry.isIntersecting) {
                    entry.target.classList.add("is-visible");
                    revealObserver.unobserve(entry.target);
                }
            }
        },
        { threshold: 0.15, rootMargin: "0px 0px -8% 0px" },
    );

    let groupStart = 0;
    let lastTop = Number.NaN;
    revealItems.forEach((item, index) => {
        const top = Math.round(item.getBoundingClientRect().top);
        if (top !== lastTop) {
            groupStart = index;
            lastTop = top;
        }
        item.style.setProperty(
            "--reveal-delay",
            `${(index - groupStart) * 70}ms`,
        );
        revealObserver.observe(item);
    });
} else {
    revealItems.forEach((item) => item.classList.add("is-visible"));
}

const sqlPanel = document.getElementById("sql-panel");
if (sqlPanel) {
    if (reduceMotion || !("IntersectionObserver" in window)) {
        sqlPanel.classList.add("sql-panel-revealed");
    } else {
        const block = sqlPanel.querySelector<HTMLElement>(".code-block");
        if (block) {
            sqlPanel.style.setProperty(
                "--sql-height",
                `${block.offsetHeight}px`,
            );
        }
        const sqlObserver = new IntersectionObserver(
            (entries) => {
                for (const entry of entries) {
                    if (entry.isIntersecting) {
                        entry.target.classList.add("sql-panel-revealed");
                        sqlObserver.unobserve(entry.target);
                    }
                }
            },
            { threshold: 0.4 },
        );
        sqlObserver.observe(sqlPanel);
    }
}

const codeWindow = document.getElementById("hero-code-window");
if (codeWindow && !reduceMotion && window.matchMedia("(pointer: fine)").matches) {
    const maxTilt = 5;
    codeWindow.addEventListener("pointermove", (event) => {
        const rect = codeWindow.getBoundingClientRect();
        const px = (event.clientX - rect.left) / rect.width - 0.5;
        const py = (event.clientY - rect.top) / rect.height - 0.5;
        codeWindow.style.transform = `perspective(900px) rotateX(${(-py * maxTilt).toFixed(2)}deg) rotateY(${(px * maxTilt).toFixed(2)}deg)`;
    });
    codeWindow.addEventListener("pointerleave", () => {
        codeWindow.style.transform = "";
    });
}

const toggle = document.getElementById("theme-toggle");
if (toggle) {
    toggle.addEventListener("click", () => {
        const isLight =
            document.documentElement.getAttribute("data-theme") === "light";
        if (isLight) {
            document.documentElement.removeAttribute("data-theme");
            localStorage.removeItem("theme");
        } else {
            document.documentElement.setAttribute("data-theme", "light");
            localStorage.setItem("theme", "light");
        }
    });
}
