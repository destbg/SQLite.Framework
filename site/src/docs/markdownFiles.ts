const files = import.meta.glob("../../../wiki/*.md", {
    query: "?raw",
    import: "default",
    eager: true,
}) as Record<string, string>;

export function markdownFor(fileName: string): string | null {
    return files[`../../../wiki/${fileName}.md`] ?? null;
}
