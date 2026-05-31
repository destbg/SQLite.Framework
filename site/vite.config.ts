import { resolve } from 'node:path'
import { defineConfig, type Connect, type PluginOption } from 'vite'
import react from '@vitejs/plugin-react'

const SPA_DIRS = [
    { prefix: '/Walkthrough/', html: '/Walkthrough/index.html' },
    { prefix: '/Docs/', html: '/Docs/index.html' },
]

const subdirSpaFallback = (): PluginOption => {
    const handler: Connect.NextHandleFunction = (req, _res, next) => {
        const url = req.url ?? '/'
        if (!(req.headers.accept ?? '').includes('text/html')) return next()
        const path = url.split('?')[0]
        for (const { prefix, html } of SPA_DIRS) {
            if (path.startsWith(prefix) && path !== prefix && path !== html) {
                const rest = path.slice(prefix.length)
                if (!/\.[a-zA-Z0-9]+$/.test(rest)) {
                    req.url = html
                }
                break
            }
        }
        next()
    }
    return {
        name: 'subdir-spa-fallback',
        configureServer(server) {
            server.middlewares.use(handler)
        },
        configurePreviewServer(server) {
            server.middlewares.use(handler)
        },
    }
}

export default defineConfig({
    plugins: [react(), subdirSpaFallback()],
    base: '/',
    build: {
        rollupOptions: {
            input: {
                landing: resolve(__dirname, 'index.html'),
                docs: resolve(__dirname, 'Docs/index.html'),
                walkthrough: resolve(__dirname, 'Walkthrough/index.html'),
                why: resolve(__dirname, 'Why/index.html'),
            },
        },
    },
})
