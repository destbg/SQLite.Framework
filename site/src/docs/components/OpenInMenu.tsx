import { useEffect, useRef, useState } from "react";
import { copyText } from "../../highlight/copy";
import { OpenInIcon, type OpenInIconKind } from "./OpenInIcon";

interface OpenInMenuProps {
    fileName: string;
    markdown: string;
}

export function OpenInMenu({ fileName, markdown }: OpenInMenuProps) {
    const [open, setOpen] = useState(false);
    const [copied, setCopied] = useState(false);
    const rootRef = useRef<HTMLDivElement>(null);

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

    const rawUrl = `https://raw.githubusercontent.com/destbg/SQLite.Framework/main/wiki/${encodeURIComponent(fileName)}.md`;
    const editUrl = `https://github.com/destbg/SQLite.Framework/edit/main/wiki/${encodeURIComponent(fileName)}.md`;
    const prompt = encodeURIComponent(
        `Read the SQLite.Framework documentation page "${fileName}" at ${rawUrl} and help me with questions about it.`,
    );

    const onCopy = async () => {
        await copyText(markdown);
        setCopied(true);
        window.setTimeout(() => {
            setCopied(false);
            setOpen(false);
        }, 900);
    };

    const external: { label: string; href: string; icon: OpenInIconKind }[] = [
        { label: "View as Markdown", href: rawUrl, icon: "file" },
        { label: "Open in ChatGPT", href: `https://chatgpt.com/?q=${prompt}`, icon: "chatgpt" },
        { label: "Open in Claude", href: `https://claude.ai/new?q=${prompt}`, icon: "claude" },
        { label: "Open in T3 Chat", href: `https://t3.chat/new?q=${prompt}`, icon: "t3" },
        { label: "Edit on GitHub", href: editUrl, icon: "github" },
    ];

    return (
        <div className="docs-openin" ref={rootRef}>
            <button
                type="button"
                className="docs-openin-trigger"
                aria-expanded={open}
                onClick={() => setOpen((v) => !v)}
            >
                Open In
                <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m6 9 6 6 6-6" /></svg>
            </button>
            {open && (
                <div className="docs-openin-menu" role="menu">
                    <button type="button" role="menuitem" onClick={onCopy}>
                        <OpenInIcon kind="copy" />
                        {copied ? "Copied" : "Copy as Markdown"}
                    </button>
                    {external.map((item) => (
                        <a
                            key={item.label}
                            role="menuitem"
                            href={item.href}
                            target="_blank"
                            rel="noopener noreferrer"
                            onClick={() => setOpen(false)}
                        >
                            <OpenInIcon kind={item.icon} />
                            {item.label}
                        </a>
                    ))}
                </div>
            )}
        </div>
    );
}
