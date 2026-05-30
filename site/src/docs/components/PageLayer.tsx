import type { ReactNode } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { slugify } from "../utils";
import { highlight } from "../../highlight/highlighter";
import CopyButton from "../../highlight/CopyButton";
import { loadContent } from "../markdownFiles";
import { findPageByLink, type Page } from "../pages";

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

interface Props {
    page: Page;
    onLinkClick: (href: string) => void;
}

export default function PageLayer({ page, onLinkClick }: Props) {
    return (
        <ReactMarkdown
            remarkPlugins={[remarkGfm]}
            components={{
                pre(props) {
                    const node = (props as {
                        node?: {
                            children?: Array<{
                                tagName?: string;
                                properties?: { className?: unknown };
                                children?: Array<{ value?: string }>;
                            }>;
                        };
                    }).node;
                    const codeNode = node?.children?.find((c) => c.tagName === "code");
                    const classes = Array.isArray(codeNode?.properties?.className)
                        ? (codeNode.properties.className as string[])
                        : [];
                    let lang = "";
                    for (const cls of classes) {
                        const m = /^language-(\w+)$/.exec(cls);
                        if (m) {
                            lang = m[1];
                            break;
                        }
                    }
                    const text = (codeNode?.children ?? [])
                        .map((c) => (typeof c.value === "string" ? c.value : ""))
                        .join("");
                    return (
                        <pre>
                            <CopyButton text={text} />
                            <code
                                className={lang ? `language-${lang}` : undefined}
                                dangerouslySetInnerHTML={{ __html: highlight(text, lang) }}
                            />
                        </pre>
                    );
                },
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
                    return (
                        <h2 id={id}>
                            <a href={`#${id}`} className="heading-anchor" aria-label="Link to this section">#</a>
                            {children}
                        </h2>
                    );
                },
                h3({ children }) {
                    const id = slugify(childrenToText(children));
                    return (
                        <h3 id={id}>
                            <a href={`#${id}`} className="heading-anchor" aria-label="Link to this section">#</a>
                            {children}
                        </h3>
                    );
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
