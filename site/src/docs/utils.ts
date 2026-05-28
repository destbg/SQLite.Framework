export function slugify(text: string): string {
    return text
        .toLowerCase()
        .replace(/[^a-z0-9\s-]/g, "")
        .replace(/\s+/g, "-")
        .trim();
}

export function urlSlug(text: string): string {
    return text.trim().replace(/\s+/g, "-");
}

export interface Heading {
    id: string;
    text: string;
    level: 2 | 3;
}

export function parseHeadings(markdown: string): Heading[] {
    const headings: Heading[] = [];
    for (const line of markdown.split("\n")) {
        const m = line.match(/^(#{2,3}) (.+)/);
        if (m) {
            const level = m[1].length as 2 | 3;
            const text = m[2].trim();
            headings.push({ id: slugify(text), text, level });
        }
    }
    return headings;
}
