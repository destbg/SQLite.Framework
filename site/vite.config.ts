import { defineConfig, type Plugin } from "vite";
import react from "@vitejs/plugin-react";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { readdirSync, readFileSync } from "node:fs";
import { extractHeadings } from "./src/docs/utils.ts";

const rootDir = dirname(fileURLToPath(import.meta.url));

function docSearchIndex(): Plugin {
    const moduleId = "virtual:docs-search-index";
    const resolvedId = `\0${moduleId}`;
    const wikiDir = resolve(rootDir, "../wiki");
    return {
        name: "docs-search-index",
        resolveId(id) {
            if (id === moduleId) {
                return resolvedId;
            }
        },
        load(id) {
            if (id !== resolvedId) return;
            const documents: Record<string, { body: string; headings: string[] }> = {};
            for (const entry of readdirSync(wikiDir, { withFileTypes: true })) {
                if (!entry.isFile() || !entry.name.endsWith(".md")) {
                    continue;
                }

                const file = resolve(wikiDir, entry.name);
                this.addWatchFile(file);
                const markdown = readFileSync(file, "utf8");
                documents[entry.name.slice(0, -3)] = {
                    body: markdown
                        .replace(/```[\s\S]*?```/g, " ")
                        .replace(/[#`*_>|]/g, "")
                        .replace(/\[([^\]]*)\]\([^)]*\)/g, "$1")
                        .replace(/\s+/g, " "),
                    headings: extractHeadings(markdown).map((heading) => heading.text),
                };
            }
            return `export default ${JSON.stringify(documents)};`;
        },
    };
}

function subdirSpaFallback(): Plugin {
    const rewrite = (url: string | undefined): string | null => {
        if (!url) return null;
        const path = url.split("?")[0];
        for (const dir of ["/Docs/", "/Walkthrough/"]) {
            if (path.startsWith(dir) && !path.slice(dir.length).includes(".")) {
                return `${dir}index.html`;
            }
        }
        return null;
    };

    return {
        name: "subdir-spa-fallback",
        configureServer(server) {
            server.middlewares.use((req, _res, next) => {
                const target = rewrite(req.url);
                if (target) req.url = target;
                next();
            });
        },
        configurePreviewServer(server) {
            server.middlewares.use((req, _res, next) => {
                const target = rewrite(req.url);
                if (target) req.url = target;
                next();
            });
        },
    };
}

export default defineConfig({
    base: "/",
    plugins: [react(), docSearchIndex(), subdirSpaFallback()],
    build: {
        rollupOptions: {
            input: {
                landing: resolve(rootDir, "index.html"),
                docs: resolve(rootDir, "Docs/index.html"),
                walkthrough: resolve(rootDir, "Walkthrough/index.html"),
            },
        },
    },
});
