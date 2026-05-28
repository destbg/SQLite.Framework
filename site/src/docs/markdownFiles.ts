// @ts-expect-error
const markdownFiles = import.meta.glob("../../../wiki/*.md", {
    query: "?raw",
    import: "default",
    eager: true,
}) as Record<string, string>;

export function loadContent(slug: string): string {
    return (
        markdownFiles[`../../../wiki/${slug}.md`] ??
        `# Not found\n\nThe page **${slug}** does not exist.`
    );
}
