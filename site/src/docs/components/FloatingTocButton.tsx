import { useEffect, useRef, useState } from "react";
import type { Heading } from "../utils";

interface Props {
    headings: Heading[];
}

export default function FloatingTocButton({ headings }: Props) {
    const [open, setOpen] = useState(false);
    const containerRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        if (!open) return;
        const onDocClick = (e: MouseEvent) => {
            if (
                containerRef.current &&
                !containerRef.current.contains(e.target as Node)
            ) {
                setOpen(false);
            }
        };
        const onKey = (e: KeyboardEvent) => {
            if (e.key === "Escape") setOpen(false);
        };
        window.addEventListener("mousedown", onDocClick);
        window.addEventListener("keydown", onKey);
        return () => {
            window.removeEventListener("mousedown", onDocClick);
            window.removeEventListener("keydown", onKey);
        };
    }, [open]);

    if (headings.length === 0) return null;

    return (
        <div className="floating-toc" ref={containerRef}>
            <button
                type="button"
                className="open-in-button"
                aria-haspopup="menu"
                aria-expanded={open}
                onClick={() => setOpen((v) => !v)}
            >
                On this page
                <svg
                    viewBox="0 0 10 6"
                    width="10"
                    height="6"
                    aria-hidden="true"
                    className={`open-in-chevron${open ? " open-in-chevron--open" : ""}`}
                >
                    <path
                        d="M1 1l4 4 4-4"
                        stroke="currentColor"
                        strokeWidth="1.5"
                        fill="none"
                        strokeLinecap="round"
                        strokeLinejoin="round"
                    />
                </svg>
            </button>
            {open && (
                <div className="open-in-menu floating-toc-menu" role="menu">
                    {headings.map((h) => (
                        <button
                            key={h.id}
                            type="button"
                            role="menuitem"
                            className={`open-in-item${h.level === 3 ? " floating-toc-item--h3" : ""}`}
                            onClick={() => {
                                setOpen(false);
                                document
                                    .getElementById(h.id)
                                    ?.scrollIntoView({ behavior: "smooth" });
                            }}
                        >
                            <span className="open-in-label">{h.text}</span>
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
}
