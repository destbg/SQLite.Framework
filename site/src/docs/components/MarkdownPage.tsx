import { useEffect, useRef, type ReactNode } from "react";
import Markdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { Link } from "react-router-dom";
import { CodeBlock } from "../../highlight/CodeBlock";
import { findPage } from "../pages";
import { extractText, slugifyHeading } from "../utils";

interface MarkdownPageProps {
    markdown: string;
}

function Heading({ level, children }: { level: 2 | 3; children?: ReactNode }) {
    const text = extractText(children);
    const id = slugifyHeading(text);
    const Tag = `h${level}` as const;
    return (
        <Tag id={id}>
            {children}
            <a className="docs-anchor" href={`#${id}`} aria-label={`Link to ${text}`}>
                #
            </a>
        </Tag>
    );
}

function DocLink({ href, children }: { href?: string; children?: ReactNode }) {
    if (!href) return <a>{children}</a>;
    if (href.startsWith("#")) return <a href={href}>{children}</a>;
    if (/^https?:\/\//.test(href)) {
        return (
            <a href={href} target="_blank" rel="noopener noreferrer">
                {children}
            </a>
        );
    }
    if (href.startsWith("/Docs/")) {
        return <Link to={href.slice("/Docs".length)}>{children}</Link>;
    }
    if (href.startsWith("/")) return <a href={href}>{children}</a>;

    const target = href.replace(/^\.\//, "").replace(/\.md$/, "").split("#");
    const page = findPage(target[0]);
    if (page) {
        const hash = target[1] ? `#${target[1]}` : "";
        return <Link to={`/${page.slug}${hash}`}>{children}</Link>;
    }
    return <a href={href}>{children}</a>;
}

export function MarkdownPage({ markdown }: MarkdownPageProps) {
    const rootRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const root = rootRef.current;
        if (!root) return;
        for (const table of root.querySelectorAll("table")) {
            const headers = Array.from(table.querySelectorAll("thead th")).map(
                (th) => th.textContent ?? "",
            );
            for (const row of table.querySelectorAll("tbody tr")) {
                row.querySelectorAll("td").forEach((td, i) => {
                    if (headers[i]) td.setAttribute("data-label", headers[i]);
                });
            }
        }
    }, [markdown]);

    return (
        <div className="docs-markdown" ref={rootRef}>
            <Markdown
                remarkPlugins={[remarkGfm]}
                components={{
                    pre: ({ children }) => <>{children}</>,
                    code: ({ className, children }) => {
                        const match = /language-([\w-]+)/.exec(className ?? "");
                        const text = String(children ?? "");
                        if (match || text.includes("\n")) {
                            return (
                                <CodeBlock
                                    code={text.replace(/\n$/, "")}
                                    language={match?.[1]}
                                />
                            );
                        }
                        return <code className="docs-inline-code">{text}</code>;
                    },
                    h2: ({ children }) => <Heading level={2}>{children}</Heading>,
                    h3: ({ children }) => <Heading level={3}>{children}</Heading>,
                    a: ({ href, children }) => <DocLink href={href}>{children}</DocLink>,
                    table: ({ children }) => (
                        <div className="docs-table-scroll">
                            <table>{children}</table>
                        </div>
                    ),
                }}
            >
                {markdown}
            </Markdown>
        </div>
    );
}
