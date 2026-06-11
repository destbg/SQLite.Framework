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

        const visible = new Set<string>();
        const observer = new IntersectionObserver(
            (entries) => {
                for (const entry of entries) {
                    if (entry.isIntersecting) visible.add(entry.target.id);
                    else visible.delete(entry.target.id);
                }
                for (const heading of headings) {
                    if (visible.has(heading.id)) {
                        setActiveId(heading.id);
                        return;
                    }
                }
            },
            { rootMargin: "-70px 0px -65% 0px" },
        );

        for (const heading of headings) {
            const el = document.getElementById(heading.id);
            if (el) observer.observe(el);
        }
        return () => observer.disconnect();
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
