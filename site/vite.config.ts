import { defineConfig, type Plugin } from "vite";
import react from "@vitejs/plugin-react";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const rootDir = dirname(fileURLToPath(import.meta.url));

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
    plugins: [react(), subdirSpaFallback()],
    build: {
        rollupOptions: {
            input: {
                landing: resolve(rootDir, "index.html"),
                docs: resolve(rootDir, "Docs/index.html"),
                walkthrough: resolve(rootDir, "Walkthrough/index.html"),
                why: resolve(rootDir, "Why/index.html"),
            },
        },
    },
});
