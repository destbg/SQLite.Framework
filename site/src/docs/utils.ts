import type { ReactNode } from "react";

export function slugifyHeading(text: string): string {
    return text
        .toLowerCase()
        .trim()
        .replace(/[^a-z0-9\s-]/g, "")
        .replace(/\s+/g, "-");
}

export function extractText(node: ReactNode): string {
    if (node == null || typeof node === "boolean") return "";
    if (typeof node === "string" || typeof node === "number") return String(node);
    if (Array.isArray(node)) return node.map(extractText).join("");
    if (typeof node === "object" && "props" in node) {
        return extractText((node.props as { children?: ReactNode }).children);
    }
    return "";
}

export interface Heading {
    depth: 2 | 3;
    text: string;
    id: string;
}

export function extractHeadings(markdown: string): Heading[] {
    const headings: Heading[] = [];
    let inFence = false;
    for (const line of markdown.split("\n")) {
        if (/^\s*(```|~~~)/.test(line)) {
            inFence = !inFence;
            continue;
        }
        if (inFence) continue;
        const match = /^(#{2,3})\s+(.+?)\s*#*\s*$/.exec(line);
        if (match) {
            const text = match[2].replace(/[`*_]/g, "");
            headings.push({
                depth: match[1].length as 2 | 3,
                text,
                id: slugifyHeading(text),
            });
        }
    }
    return headings;
}
