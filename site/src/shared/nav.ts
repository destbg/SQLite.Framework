export function initMobileNav(): void {
    const toggle = document.querySelector<HTMLButtonElement>(".nav-toggle");
    const nav = document.querySelector<HTMLElement>(".site-nav");
    if (!toggle || !nav) return;

    const close = () => {
        nav.classList.remove("is-open");
        toggle.setAttribute("aria-expanded", "false");
    };

    toggle.addEventListener("click", () => {
        const open = nav.classList.toggle("is-open");
        toggle.setAttribute("aria-expanded", String(open));
    });

    nav.addEventListener("click", (e) => {
        if ((e.target as HTMLElement).tagName === "A") close();
    });

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape") close();
    });

    window.matchMedia("(min-width: 701px)").addEventListener("change", (e) => {
        if (e.matches) close();
    });
}
