import { useEffect, useRef, useState } from 'react'

interface Props {
    slug: string
    markdown: string
}

const repoEditBase = 'https://github.com/destbg/SQLite.Framework/edit/main/wiki'
const repoRawBase = 'https://raw.githubusercontent.com/destbg/SQLite.Framework/main/wiki'

function buildPrompt(slug: string): string {
    const rawUrl = `${repoRawBase}/${encodeURIComponent(slug)}.md`
    return `Read the SQLite.Framework documentation page "${slug}" at ${rawUrl} and help me with questions about it.`
}

export default function OpenInButton({ slug, markdown }: Props) {
    const [open, setOpen] = useState(false)
    const [copied, setCopied] = useState(false)
    const containerRef = useRef<HTMLDivElement>(null)

    useEffect(() => {
        if (!open) return
        const onDocClick = (e: MouseEvent) => {
            if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
                setOpen(false)
            }
        }
        const onKey = (e: KeyboardEvent) => {
            if (e.key === 'Escape') setOpen(false)
        }
        window.addEventListener('mousedown', onDocClick)
        window.addEventListener('keydown', onKey)
        return () => {
            window.removeEventListener('mousedown', onDocClick)
            window.removeEventListener('keydown', onKey)
        }
    }, [open])

    const rawUrl = `${repoRawBase}/${encodeURIComponent(slug)}.md`
    const editUrl = `${repoEditBase}/${encodeURIComponent(slug)}.md`
    const prompt = buildPrompt(slug)
    const promptEncoded = encodeURIComponent(prompt)
    const chatGptUrl = `https://chatgpt.com/?q=${promptEncoded}`
    const claudeUrl = `https://claude.ai/new?q=${promptEncoded}`
    const t3Url = `https://t3.chat/new?q=${promptEncoded}`

    const copyMarkdown = async () => {
        try {
            await navigator.clipboard.writeText(markdown)
            setCopied(true)
            window.setTimeout(() => setCopied(false), 1500)
        } catch {
            // ignore clipboard errors (insecure context, blocked, etc.)
        }
    }

    return (
        <div className="open-in" ref={containerRef}>
            <button
                type="button"
                className="open-in-button"
                aria-haspopup="menu"
                aria-expanded={open}
                onClick={() => setOpen(v => !v)}
            >
                Open In
                <svg
                    viewBox="0 0 10 6"
                    width="10"
                    height="6"
                    aria-hidden="true"
                    className={`open-in-chevron${open ? ' open-in-chevron--open' : ''}`}
                >
                    <path d="M1 1l4 4 4-4" stroke="currentColor" strokeWidth="1.5" fill="none" strokeLinecap="round" strokeLinejoin="round" />
                </svg>
            </button>
            {open && (
                <div className="open-in-menu" role="menu">
                    <button type="button" className="open-in-item" role="menuitem" onClick={copyMarkdown}>
                        <OpenInIcon kind="copy" />
                        <span className="open-in-label">{copied ? 'Copied!' : 'Copy as Markdown'}</span>
                    </button>
                    <a className="open-in-item" role="menuitem" href={rawUrl} target="_blank" rel="noopener noreferrer">
                        <OpenInIcon kind="file" />
                        <span className="open-in-label">View as Markdown</span>
                    </a>
                    <div className="open-in-divider" />
                    <a className="open-in-item" role="menuitem" href={chatGptUrl} target="_blank" rel="noopener noreferrer">
                        <OpenInIcon kind="chatgpt" />
                        <span className="open-in-label">Open in ChatGPT</span>
                    </a>
                    <a className="open-in-item" role="menuitem" href={claudeUrl} target="_blank" rel="noopener noreferrer">
                        <OpenInIcon kind="claude" />
                        <span className="open-in-label">Open in Claude</span>
                    </a>
                    <a className="open-in-item" role="menuitem" href={t3Url} target="_blank" rel="noopener noreferrer">
                        <OpenInIcon kind="t3" />
                        <span className="open-in-label">Open in T3 Chat</span>
                    </a>
                    <div className="open-in-divider" />
                    <a className="open-in-item" role="menuitem" href={editUrl} target="_blank" rel="noopener noreferrer">
                        <OpenInIcon kind="github" />
                        <span className="open-in-label">Edit on GitHub</span>
                    </a>
                </div>
            )}
        </div>
    )
}

type IconKind = 'copy' | 'file' | 'chatgpt' | 'claude' | 't3' | 'github'

function OpenInIcon({ kind }: { kind: IconKind }) {
    switch (kind) {
        case 'copy':
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width="14" height="14" aria-hidden="true">
                    <path d="M5 2h7a1 1 0 0 1 1 1v9h-1V3H5V2z M3 4h7a1 1 0 0 1 1 1v9a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1zm0 1v9h7V5H3z" fill="currentColor" />
                </svg>
            )
        case 'file':
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width="14" height="14" aria-hidden="true">
                    <path d="M4 1.5A1.5 1.5 0 0 1 5.5 0h5L14 3.5v11a1.5 1.5 0 0 1-1.5 1.5h-7A1.5 1.5 0 0 1 4 14.5v-13zM5.5 1a.5.5 0 0 0-.5.5v13a.5.5 0 0 0 .5.5h7a.5.5 0 0 0 .5-.5V4h-3V1H5.5zM11 1.75V3h2.25L11 1.75z" fill="currentColor" />
                </svg>
            )
        case 'chatgpt':
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width="14" height="14" aria-hidden="true">
                    <path d="M8 1.5c-3.59 0-6.5 2.91-6.5 6.5 0 1.17.31 2.27.86 3.22L1.5 14.5l3.33-.88a6.5 6.5 0 1 0 3.17-12.12zm0 1.5a5 5 0 1 1-2.54 9.3l-.22-.13-1.97.52.53-1.93-.14-.23A5 5 0 0 1 8 3z" fill="currentColor" />
                </svg>
            )
        case 'claude':
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width="14" height="14" aria-hidden="true">
                    <path d="M8 1l1.7 4.8L14.5 8l-4.8 1.7L8 14.5l-1.7-4.8L1.5 8l4.8-1.7L8 1z" fill="currentColor" />
                </svg>
            )
        case 't3':
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width="14" height="14" aria-hidden="true">
                    <path d="M2 3h12v2H9v9H7V5H2V3z" fill="currentColor" />
                </svg>
            )
        case 'github':
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width="14" height="14" aria-hidden="true">
                    <path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2 .37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82a7.4 7.4 0 0 1 2-.27c.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8z" fill="currentColor" />
                </svg>
            )
    }
}
