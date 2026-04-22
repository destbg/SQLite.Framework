import { useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import ReactMarkdown from 'react-markdown'
import rehypeHighlight from 'rehype-highlight'
import remarkGfm from 'remark-gfm'
import { useNavigate } from 'react-router-dom'
import { slugify } from '../utils'
import PageNavigation from './PageNavigation'

// @ts-expect-error
const markdownFiles = import.meta.glob('../../*.md', {
    query: '?raw',
    import: 'default',
    eager: true,
}) as Record<string, string>

const FADE_DURATION = 200
const HEIGHT_LOCK_DURATION = 700

function childrenToText(node: ReactNode): string {
    if (typeof node === 'string') return node
    if (typeof node === 'number') return String(node)
    if (Array.isArray(node)) return node.map(childrenToText).join('')
    if (node && typeof node === 'object' && 'props' in node)
        return childrenToText((node as { props: { children: ReactNode } }).props.children)
    return ''
}

function loadContent(slug: string): string {
    return markdownFiles[`../../${slug}.md`] ?? `# Not found\n\nThe page **${slug}** does not exist.`
}

interface LayerProps {
    slug: string
    onLinkClick: (href: string) => void
}

function PageLayer({ slug, onLinkClick }: LayerProps) {
    return (
        <ReactMarkdown
            remarkPlugins={[remarkGfm]}
            rehypePlugins={[rehypeHighlight]}
            components={{
                a({ href, children }) {
                    if (href && !href.startsWith('http') && !href.startsWith('#')) {
                        return (
                            <a
                                href={`#/${href}`}
                                onClick={e => {
                                    e.preventDefault()
                                    onLinkClick(href)
                                }}
                            >
                                {children}
                            </a>
                        )
                    }
                    return (
                        <a href={href} target="_blank" rel="noopener noreferrer">
                            {children}
                        </a>
                    )
                },
                h2({ children }) {
                    const id = slugify(childrenToText(children))
                    return <h2 id={id}>{children}</h2>
                },
                h3({ children }) {
                    const id = slugify(childrenToText(children))
                    return <h3 id={id}>{children}</h3>
                },
            }}
        >
            {loadContent(slug)}
        </ReactMarkdown>
    )
}

interface Props {
    slug: string
}

export default function MarkdownPage({ slug }: Props) {
    const navigate = useNavigate()
    const articleRef = useRef<HTMLElement>(null)
    const prevSlugRef = useRef(slug)
    const [displaySlug, setDisplaySlug] = useState(slug)
    const [outgoingSlug, setOutgoingSlug] = useState<string | null>(null)
    const [lockedHeight, setLockedHeight] = useState<number | null>(null)

    useEffect(() => {
        if (slug === prevSlugRef.current) return

        const outgoing = prevSlugRef.current
        prevSlugRef.current = slug

        const currentHeight = articleRef.current?.offsetHeight ?? 0
        setLockedHeight(currentHeight)
        setOutgoingSlug(outgoing)
        setDisplaySlug(slug)

        const raf = requestAnimationFrame(() => {
            window.scrollTo({ top: 0, behavior: 'smooth' })
        })

        const fadeTimer = window.setTimeout(() => {
            setOutgoingSlug(null)
        }, FADE_DURATION)

        const heightTimer = window.setTimeout(() => {
            setLockedHeight(null)
        }, HEIGHT_LOCK_DURATION)

        return () => {
            cancelAnimationFrame(raf)
            window.clearTimeout(fadeTimer)
            window.clearTimeout(heightTimer)
        }
    }, [slug])

    const handleLinkClick = (href: string) => navigate(`/${href}`)

    return (
        <article
            ref={articleRef}
            className="markdown-content page-stage"
            style={lockedHeight != null ? { minHeight: lockedHeight } : undefined}
        >
            <div key={displaySlug} className="page-layer page-layer--in">
                <PageLayer slug={displaySlug} onLinkClick={handleLinkClick} />
                <PageNavigation slug={displaySlug} />
            </div>
            {outgoingSlug && (
                <div key={outgoingSlug} className="page-layer page-layer--out" aria-hidden="true">
                    <PageLayer slug={outgoingSlug} onLinkClick={handleLinkClick} />
                    <PageNavigation slug={outgoingSlug} />
                </div>
            )}
        </article>
    )
}
