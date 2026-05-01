// @ts-expect-error
const markdownFiles = import.meta.glob("../*.md", {
  query: "?raw",
  import: "default",
  eager: true,
}) as Record<string, string>;

export function loadContent(slug: string): string {
  return (
    markdownFiles[`../${slug}.md`] ??
    `# Not found\n\nThe page **${slug}** does not exist.`
  );
}
