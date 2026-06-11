import "../shared/tokens.css";
import "../shared/chrome.css";
import "../shared/view-transitions.css";
import "../highlight/syntax.css";
import "./landing.css";
import { highlightInto } from "../highlight/highlighter";
import { attachCopyButtons } from "../highlight/copy";
import { bindThemeToggles } from "../shared/theme";
import { initReveal } from "../shared/reveal";
import { initQueryTabs } from "./queryTabs";
import { initSavePace } from "./savePace";

for (const code of document.querySelectorAll<HTMLElement>("code[data-lang]")) {
    highlightInto(code, code.textContent ?? "", code.dataset.lang);
}

attachCopyButtons(document);
bindThemeToggles(".theme-toggle");
initReveal();
initQueryTabs();
initSavePace();

const tilt = document.querySelector<HTMLElement>("[data-tilt]");
if (
    tilt &&
    window.matchMedia("(pointer: fine)").matches &&
    !window.matchMedia("(prefers-reduced-motion: reduce)").matches
) {
    const card = tilt.firstElementChild as HTMLElement | null;
    if (card) {
        tilt.addEventListener("pointermove", (e) => {
            const rect = tilt.getBoundingClientRect();
            const rx = ((e.clientY - rect.top) / rect.height - 0.5) * -5;
            const ry = ((e.clientX - rect.left) / rect.width - 0.5) * 5;
            card.style.transform = `perspective(900px) rotateX(${rx.toFixed(2)}deg) rotateY(${ry.toFixed(2)}deg)`;
        });
        tilt.addEventListener("pointerleave", () => {
            card.style.transform = "";
        });
    }
}
