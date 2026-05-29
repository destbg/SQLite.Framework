import { useEffect, useRef, useState } from "react";
import OpenInIcon from "./OpenInIcon";

interface Props {
    fileName: string;
    markdown: string;
}

const repoEditBase =
    "https://github.com/destbg/SQLite.Framework/edit/main/wiki";
const repoRawBase =
    "https://raw.githubusercontent.com/destbg/SQLite.Framework/main/wiki";

function buildPrompt(fileName: string): string {
    const rawUrl = `${repoRawBase}/${encodeURIComponent(fileName)}.md`;
    return `Read the SQLite.Framework documentation page "${fileName}" at ${rawUrl} and help me with questions about it.`;
}

export default function OpenInButton({ fileName, markdown }: Props) {
    const [open, setOpen] = useState(false);
    const [copied, setCopied] = useState(false);
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

    const rawUrl = `${repoRawBase}/${encodeURIComponent(fileName)}.md`;
    const editUrl = `${repoEditBase}/${encodeURIComponent(fileName)}.md`;
    const prompt = buildPrompt(fileName);
    const promptEncoded = encodeURIComponent(prompt);
    const chatGptUrl = `https://chatgpt.com/?q=${promptEncoded}`;
    const claudeUrl = `https://claude.ai/new?q=${promptEncoded}`;
    const t3Url = `https://t3.chat/new?q=${promptEncoded}`;

    const copyMarkdown = async () => {
        try {
            await navigator.clipboard.writeText(markdown);
            setCopied(true);
            window.setTimeout(() => setCopied(false), 1500);
        } catch {
            // ignore clipboard errors (insecure context, blocked, etc.)
        }
    };

    return (
        <div className="open-in" ref={containerRef}>
            <button
                type="button"
                className="open-in-button"
                aria-haspopup="menu"
                aria-expanded={open}
                onClick={() => setOpen((v) => !v)}
            >
                Open In
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
                <div className="open-in-menu" role="menu">
                    <button
                        type="button"
                        className="open-in-item"
                        role="menuitem"
                        onClick={copyMarkdown}
                    >
                        <OpenInIcon kind="copy" />
                        <span className="open-in-label">
                            {copied ? "Copied!" : "Copy as Markdown"}
                        </span>
                    </button>
                    <a
                        className="open-in-item"
                        role="menuitem"
                        href={rawUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                    >
                        <OpenInIcon kind="file" />
                        <span className="open-in-label">View as Markdown</span>
                    </a>
                    <div className="open-in-divider" />
                    <a
                        className="open-in-item"
                        role="menuitem"
                        href={chatGptUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                    >
                        <OpenInIcon kind="chatgpt" />
                        <span className="open-in-label">Open in ChatGPT</span>
                    </a>
                    <a
                        className="open-in-item"
                        role="menuitem"
                        href={claudeUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                    >
                        <OpenInIcon kind="claude" />
                        <span className="open-in-label">Open in Claude</span>
                    </a>
                    <a
                        className="open-in-item"
                        role="menuitem"
                        href={t3Url}
                        target="_blank"
                        rel="noopener noreferrer"
                    >
                        <OpenInIcon kind="t3" />
                        <span className="open-in-label">Open in T3 Chat</span>
                    </a>
                    <div className="open-in-divider" />
                    <a
                        className="open-in-item"
                        role="menuitem"
                        href={editUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                    >
                        <OpenInIcon kind="github" />
                        <span className="open-in-label">Edit on GitHub</span>
                    </a>
                </div>
            )}
        </div>
    );
}
