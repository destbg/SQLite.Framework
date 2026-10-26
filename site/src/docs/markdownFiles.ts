import home from "../../../wiki/Home.md?raw";

const files = import.meta.glob<string>(["../../../wiki/*.md", "!../../../wiki/Home.md"], {
    query: "?raw",
    import: "default",
});

const cache = new Map<string, string>([["Home", home]]);

export function markdownFor(fileName: string): string | null {
    return cache.get(fileName) ?? null;
}

export async function loadMarkdown(fileName: string): Promise<string | null> {
    const cached = markdownFor(fileName);
    if (cached !== null) return cached;
    const load = files[`../../../wiki/${fileName}.md`];
    if (!load) return null;
    const markdown = await load();
    cache.set(fileName, markdown);
    return markdown;
}
