/// <reference types="vite/client" />

declare module "virtual:docs-search-index" {
    const documents: Record<string, { body: string; headings: string[] }>;
    export default documents;
}
