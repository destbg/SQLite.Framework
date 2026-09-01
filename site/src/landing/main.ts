import "../shared/fonts";
import "../shared/tokens.css";
import "../shared/chrome.css";
import "../shared/view-transitions.css";
import "../highlight/syntax.css";
import "./landing.css";
import { highlightInto } from "../highlight/highlighter";
import { attachCopyButtons } from "../highlight/copy";
import { bindThemeToggles } from "../shared/theme";
import { initReveal } from "../shared/reveal";
import { initMobileNav } from "../shared/nav";
import { initQueryTabs } from "./queryTabs";
import { initSavePace } from "./savePace";

for (const code of document.querySelectorAll<HTMLElement>("code[data-lang]")) {
    highlightInto(code, code.textContent ?? "", code.dataset.lang);
}

attachCopyButtons(document);
bindThemeToggles(".theme-toggle");
initReveal();
initMobileNav();
initQueryTabs();
initSavePace();
