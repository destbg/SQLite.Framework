import { resolve } from 'node:path'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
    plugins: [react()],
    base: '/',
    build: {
        rollupOptions: {
            input: {
                landing: resolve(__dirname, 'index.html'),
                docs: resolve(__dirname, 'Docs/index.html'),
            },
        },
    },
})
