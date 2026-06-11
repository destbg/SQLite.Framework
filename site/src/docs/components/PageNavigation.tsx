import { Link } from "react-router-dom";
import { allPages, type DocPage } from "../pages";

interface PageNavigationProps {
    current: DocPage;
}

export function PageNavigation({ current }: PageNavigationProps) {
    const index = allPages.findIndex((p) => p.slug === current.slug);
    const prev = index > 0 ? allPages[index - 1] : null;
    const next = index >= 0 && index < allPages.length - 1 ? allPages[index + 1] : null;

    return (
        <nav className="docs-pagenav" aria-label="Page navigation">
            {prev ? (
                <Link to={`/${prev.slug}`} className="docs-pagenav-link docs-pagenav-prev">
                    <small>Previous</small>
                    <span>{prev.title}</span>
                </Link>
            ) : (
                <span />
            )}
            {next ? (
                <Link to={`/${next.slug}`} className="docs-pagenav-link docs-pagenav-next">
                    <small>Next</small>
                    <span>{next.title}</span>
                </Link>
            ) : (
                <span />
            )}
        </nav>
    );
}
