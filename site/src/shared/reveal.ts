export function initReveal(): void {
    const items = Array.from(document.querySelectorAll<HTMLElement>("[data-reveal]"));
    if (items.length === 0) return;

    const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (reduced || typeof IntersectionObserver === "undefined") {
        for (const el of items) el.classList.add("is-revealed");
        return;
    }

    const pendingByRow = new Map<number, HTMLElement[]>();
    const observer = new IntersectionObserver(
        (entries) => {
            for (const entry of entries) {
                if (!entry.isIntersecting) continue;
                const el = entry.target as HTMLElement;
                observer.unobserve(el);
                const row = Math.round(entry.boundingClientRect.top / 40);
                const siblings = pendingByRow.get(row) ?? [];
                siblings.push(el);
                pendingByRow.set(row, siblings);
                const delay = (siblings.length - 1) * 70;
                window.setTimeout(() => el.classList.add("is-revealed"), delay);
                window.setTimeout(() => pendingByRow.delete(row), 400);
            }
        },
        { threshold: 0.15, rootMargin: "0px 0px -40px 0px" },
    );

    for (const el of items) observer.observe(el);
}
