import hljs from "highlight.js/lib/core";
import csharp from "highlight.js/lib/languages/csharp";
import "highlight.js/styles/github-dark.css";
import "./landing.css";

hljs.registerLanguage("csharp", csharp);

document.querySelectorAll<HTMLElement>("pre code").forEach((block) => {
    hljs.highlightElement(block);
});

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

if ("serviceWorker" in navigator && import.meta.env.PROD) {
    window.addEventListener("load", () => {
        navigator.serviceWorker
            .register("/sw.js", { scope: "/" })
            .catch((err) =>
                console.error("Service worker registration failed:", err),
            );
    });
}
