import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";
import { parseHeadings } from "../utils";
import { findPageBySlug } from "../pages";
import OpenInButton from "./OpenInButton";

// @ts-expect-error
const markdownFiles = import.meta.glob("../../*.md", {
    query: "?raw",
    import: "default",
    eager: true,
}) as Record<string, string>;

export default function TableOfContents() {
    const location = useLocation();
    const slug = decodeURIComponent(
        location.pathname === "/" ? "Home" : location.pathname.slice(1),
    );
    const page = findPageBySlug(slug);
    const content = page ? markdownFiles[`../../${page.title}.md`] ?? "" : "";
    const headings = parseHeadings(content);
    const [activeId, setActiveId] = useState("");

    useEffect(() => {
        setActiveId("");
        const updateActive = () => {
            const els = Array.from(
                document.querySelectorAll<HTMLElement>(
                    ".markdown-content h2, .markdown-content h3",
                ),
            );
            if (!els.length) return;
            const scrollY = window.scrollY + 88;
            let active = els[0].id;
            for (const el of els) {
                if (el.offsetTop <= scrollY) active = el.id;
            }
            setActiveId(active);
        };
        window.addEventListener("scroll", updateActive, { passive: true });
        updateActive();
        return () => window.removeEventListener("scroll", updateActive);
    }, [slug]);

    if (headings.length === 0) return null;

    return (
        <aside className="toc">
            <div className="toc-header">
                <p className="toc-title">On this page</p>
                <OpenInButton fileName={page?.title ?? slug} markdown={content} />
            </div>
            {headings.length > 0 && (
                <ul className="toc-list">
                    {headings.map((h) => (
                        <li key={h.id}>
                            <a
                                href={`#${h.id}`}
                                className={[
                                    "toc-link",
                                    h.level === 3 ? "toc-link--h3" : "",
                                    activeId === h.id ? "toc-link--active" : "",
                                ]
                                    .filter(Boolean)
                                    .join(" ")}
                                onClick={(e) => {
                                    e.preventDefault();
                                    document
                                        .getElementById(h.id)
                                        ?.scrollIntoView({ behavior: "smooth" });
                                }}
                            >
                                {h.text}
                            </a>
                        </li>
                    ))}
                </ul>
            )}
        </aside>
    );
}
