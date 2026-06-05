import "../shared/view-transitions.css";
import "../landing/landing.css";
import "./why.css";

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
