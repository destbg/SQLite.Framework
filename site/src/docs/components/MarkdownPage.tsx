import { useEffect, useRef, useState } from "react";
import type { ReactNode } from "react";
import ReactMarkdown from "react-markdown";
import rehypeHighlight from "rehype-highlight";
import remarkGfm from "remark-gfm";
import { useNavigate } from "react-router-dom";
import { slugify } from "../utils";
import { loadContent } from "../markdownFiles";
import { findPageByLink, type Page } from "../pages";
import PageNavigation from "./PageNavigation";

const FADE_DURATION = 200;
const HEIGHT_LOCK_DURATION = 700;

function childrenToText(node: ReactNode): string {
    if (typeof node === "string") return node;
    if (typeof node === "number") return String(node);
    if (Array.isArray(node)) return node.map(childrenToText).join("");
    if (node && typeof node === "object" && "props" in node)
        return childrenToText(
            (node as { props: { children: ReactNode } }).props.children,
        );
    return "";
}

interface LayerProps {
    page: Page;
    onLinkClick: (href: string) => void;
}

function PageLayer({ page, onLinkClick }: LayerProps) {
    return (
        <ReactMarkdown
            remarkPlugins={[remarkGfm]}
            rehypePlugins={[rehypeHighlight]}
            components={{
                a({ href, children }) {
                    if (href && !href.startsWith("http") && !href.startsWith("#")) {
                        const target = findPageByLink(href);
                        const resolved = target ? target.slug : href;
                        return (
                            <a
                                href={`/Docs/${resolved}`}
                                onClick={(e) => {
                                    e.preventDefault();
                                    onLinkClick(resolved);
                                }}
                            >
                                {children}
                            </a>
                        );
                    }
                    return (
                        <a href={href} target="_blank" rel="noopener noreferrer">
                            {children}
                        </a>
                    );
                },
                h2({ children }) {
                    const id = slugify(childrenToText(children));
                    return <h2 id={id}>{children}</h2>;
                },
                h3({ children }) {
                    const id = slugify(childrenToText(children));
                    return <h3 id={id}>{children}</h3>;
                },
                table({ children }) {
                    return (
                        <div className="table-wrap">
                            <table>{children}</table>
                        </div>
                    );
                },
            }}
        >
            {loadContent(page.title)}
        </ReactMarkdown>
    );
}

interface Props {
    page: Page;
}

export default function MarkdownPage({ page }: Props) {
    const navigate = useNavigate();
    const articleRef = useRef<HTMLElement>(null);
    const prevPageRef = useRef(page);
    const [displayPage, setDisplayPage] = useState(page);
    const [outgoingPage, setOutgoingPage] = useState<Page | null>(null);
    const [lockedHeight, setLockedHeight] = useState<number | null>(null);

    useEffect(() => {
        if (page.slug === prevPageRef.current.slug) return;

        const outgoing = prevPageRef.current;
        prevPageRef.current = page;

        const currentHeight = articleRef.current?.offsetHeight ?? 0;
        setLockedHeight(currentHeight);
        setOutgoingPage(outgoing);
        setDisplayPage(page);

        const raf = requestAnimationFrame(() => {
            window.scrollTo({ top: 0, behavior: "smooth" });
        });

        const fadeTimer = window.setTimeout(() => {
            setOutgoingPage(null);
        }, FADE_DURATION);

        const heightTimer = window.setTimeout(() => {
            setLockedHeight(null);
        }, HEIGHT_LOCK_DURATION);

        return () => {
            cancelAnimationFrame(raf);
            window.clearTimeout(fadeTimer);
            window.clearTimeout(heightTimer);
        };
    }, [page]);

    const handleLinkClick = (href: string) =>
        navigate(href === "Home" ? "/" : `/${href}`);

    return (
        <article
            ref={articleRef}
            className="markdown-content page-stage"
            style={lockedHeight != null ? { minHeight: lockedHeight } : undefined}
        >
            <div key={displayPage.slug} className="page-layer page-layer--in">
                <PageLayer page={displayPage} onLinkClick={handleLinkClick} />
                <PageNavigation slug={displayPage.slug} />
            </div>
            {outgoingPage && (
                <div
                    key={outgoingPage.slug}
                    className="page-layer page-layer--out"
                    aria-hidden="true"
                >
                    <PageLayer page={outgoingPage} onLinkClick={handleLinkClick} />
                    <PageNavigation slug={outgoingPage.slug} />
                </div>
            )}
        </article>
    );
}
