export function applyTheme(theme: "dark" | "light"): void {
    if (theme === "light") {
        document.documentElement.setAttribute("data-theme", "light");
        localStorage.setItem("theme", "light");
    } else {
        document.documentElement.removeAttribute("data-theme");
        localStorage.removeItem("theme");
    }
}

export function currentTheme(): "dark" | "light" {
    return document.documentElement.getAttribute("data-theme") === "light" ? "light" : "dark";
}

export function toggleTheme(): void {
    applyTheme(currentTheme() === "light" ? "dark" : "light");
}

export function bindThemeToggles(selector: string): void {
    for (const el of document.querySelectorAll(selector)) {
        el.addEventListener("click", toggleTheme);
    }
}
