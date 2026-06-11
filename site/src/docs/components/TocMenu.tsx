import { useEffect, useMemo, useRef, useState } from "react";
import { extractHeadings } from "../utils";

interface TocMenuProps {
    markdown: string;
}

export function TocMenu({ markdown }: TocMenuProps) {
    const headings = useMemo(() => extractHeadings(markdown), [markdown]);
    const [open, setOpen] = useState(false);
    const rootRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        setOpen(false);
    }, [markdown]);

    useEffect(() => {
        if (!open) return;
        const onPointerDown = (e: PointerEvent) => {
            if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
        };
        const onKeyDown = (e: KeyboardEvent) => {
            if (e.key === "Escape") setOpen(false);
        };
        window.addEventListener("pointerdown", onPointerDown);
        window.addEventListener("keydown", onKeyDown);
        return () => {
            window.removeEventListener("pointerdown", onPointerDown);
            window.removeEventListener("keydown", onKeyDown);
        };
    }, [open]);

    if (headings.length === 0) return null;

    const go = (id: string) => {
        const el = document.getElementById(id);
        if (el) {
            el.scrollIntoView({ behavior: "smooth" });
            history.replaceState(null, "", `#${id}`);
        }
        setOpen(false);
    };

    return (
        <div className="docs-openin docs-tocmenu" ref={rootRef}>
            <button
                type="button"
                className="docs-openin-trigger"
                aria-expanded={open}
                onClick={() => setOpen((v) => !v)}
            >
                <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M4 6h16M4 12h10M4 18h13" /></svg>
                On this page
                <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m6 9 6 6 6-6" /></svg>
            </button>
            {open && (
                <div className="docs-openin-menu docs-tocmenu-menu" role="menu">
                    {headings.map((heading) => (
                        <button
                            key={heading.id}
                            type="button"
                            role="menuitem"
                            className={heading.depth === 3 ? "is-sub" : undefined}
                            onClick={() => go(heading.id)}
                        >
                            {heading.text}
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
}
