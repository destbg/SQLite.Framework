import { NavLink } from 'react-router-dom'
import { pages } from '../pages'

interface Props {
    slug: string
}

export default function PageNavigation({ slug }: Props) {
    const index = pages.findIndex(p => p.slug === slug)
    if (index === -1) return null

    const prev = index > 0 ? pages[index - 1] : null
    const next = index < pages.length - 1 ? pages[index + 1] : null

    if (!prev && !next) return null

    return (
        <nav className="page-nav" aria-label="Page navigation">
            {prev ? (
                <NavLink
                    to={prev.slug === 'Home' ? '/' : `/${prev.slug}`}
                    className="page-nav-link page-nav-link--prev"
                >
                    <span className="page-nav-arrow" aria-hidden="true">&larr;</span>
                    <span className="page-nav-text">
                        <span className="page-nav-label">Previous</span>
                        <span className="page-nav-title">{prev.title}</span>
                    </span>
                </NavLink>
            ) : <span />}

            {next ? (
                <NavLink
                    to={next.slug === 'Home' ? '/' : `/${next.slug}`}
                    className="page-nav-link page-nav-link--next"
                >
                    <span className="page-nav-text">
                        <span className="page-nav-label">Next</span>
                        <span className="page-nav-title">{next.title}</span>
                    </span>
                    <span className="page-nav-arrow" aria-hidden="true">&rarr;</span>
                </NavLink>
            ) : <span />}
        </nav>
    )
}
