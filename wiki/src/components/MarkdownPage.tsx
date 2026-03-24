import { useEffect } from 'react'
import type { ReactNode } from 'react'
import ReactMarkdown from 'react-markdown'
import rehypeHighlight from 'rehype-highlight'
import remarkGfm from 'remark-gfm'
import { useNavigate } from 'react-router-dom'
import { slugify } from '../utils'

// @ts-expect-error
const markdownFiles = import.meta.glob('../../*.md', {
    query: '?raw',
    import: 'default',
    eager: true,
}) as Record<string, string>

function childrenToText(node: ReactNode): string {
    if (typeof node === 'string') return node
    if (typeof node === 'number') return String(node)
    if (Array.isArray(node)) return node.map(childrenToText).join('')
    if (node && typeof node === 'object' && 'props' in node)
        return childrenToText((node as { props: { children: ReactNode } }).props.children)
    return ''
}

interface Props {
    slug: string
}

export default function MarkdownPage({ slug }: Props) {
    const navigate = useNavigate()
    const content = markdownFiles[`../../${slug}.md`] ?? `# Not found\n\nThe page **${slug}** does not exist.`

    useEffect(() => {
        window.scrollTo({ top: 0, behavior: 'instant' })
    }, [slug])

    return (
        <article className="markdown-content">
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
                                        navigate(`/${href}`)
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
                {content}
            </ReactMarkdown>
        </article>
    )
}
