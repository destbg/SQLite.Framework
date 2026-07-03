import { useEffect, useMemo, useState } from "react";
import { extractHeadings } from "../utils";

interface TableOfContentsProps {
    markdown: string;
    pageKey: string;
}

export function TableOfContents({ markdown, pageKey }: TableOfContentsProps) {
    const headings = useMemo(() => extractHeadings(markdown), [markdown]);
    const [activeId, setActiveId] = useState<string | null>(null);

    useEffect(() => {
        setActiveId(headings[0]?.id ?? null);
        if (headings.length === 0) return;

        let frame = 0;
        const update = () => {
            frame = 0;
            const line = window.innerHeight * 0.35;
            const atBottom =
                window.scrollY + window.innerHeight >=
                document.documentElement.scrollHeight - 2;
            let current = headings[0].id;
            for (const heading of headings) {
                const el = document.getElementById(heading.id);
                if (el && (atBottom || el.getBoundingClientRect().top <= line)) {
                    current = heading.id;
                }
            }
            setActiveId(current);
        };
        const schedule = () => {
            if (frame === 0) frame = requestAnimationFrame(update);
        };

        update();
        window.addEventListener("scroll", schedule, { passive: true });
        window.addEventListener("resize", schedule);
        return () => {
            if (frame !== 0) cancelAnimationFrame(frame);
            window.removeEventListener("scroll", schedule);
            window.removeEventListener("resize", schedule);
        };
    }, [headings, pageKey]);

    if (headings.length === 0) return null;

    const onClick = (e: React.MouseEvent, id: string) => {
        e.preventDefault();
        const el = document.getElementById(id);
        if (!el) return;
        el.scrollIntoView({ behavior: "smooth" });
        history.replaceState(null, "", `#${id}`);
        setActiveId(id);
    };

    return (
        <nav className="docs-toc" aria-label="On this page">
            <p className="docs-toc-title">On this page</p>
            <ul>
                {headings.map((heading) => (
                    <li key={heading.id} className={heading.depth === 3 ? "is-sub" : undefined}>
                        <a
                            href={`#${heading.id}`}
                            className={activeId === heading.id ? "is-active" : undefined}
                            onClick={(e) => onClick(e, heading.id)}
                        >
                            {heading.text}
                        </a>
                    </li>
                ))}
            </ul>
        </nav>
    );
}
