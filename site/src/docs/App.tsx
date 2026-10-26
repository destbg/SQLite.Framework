import { useEffect, useState } from "react";
import { Navigate, Route, Routes, useLocation, useParams } from "react-router-dom";
import "../shared/tokens.css";
import "../highlight/syntax.css";
import "./app.css";
import { findPage } from "./pages";
import { loadMarkdown, markdownFor } from "./markdownFiles";
import { Sidebar } from "./components/Sidebar";
import { MarkdownPage } from "./components/MarkdownPage";
import { PageNavigation } from "./components/PageNavigation";
import { TableOfContents } from "./components/TableOfContents";
import { SearchModal } from "./components/SearchModal";
import { OpenInMenu } from "./components/OpenInMenu";
import { TocMenu } from "./components/TocMenu";

export function App() {
    return (
        <Routes>
            <Route path="/:slug?" element={<DocLayout />} />
            <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
    );
}

function DocLayout() {
    const { slug } = useParams();
    const location = useLocation();
    const [sidebarOpen, setSidebarOpen] = useState(false);
    const [searchSeed, setSearchSeed] = useState<string | null>(null);
    const [loadedPage, setLoadedPage] = useState<{ fileName: string; markdown: string } | null>(null);
    const [loadError, setLoadError] = useState<string | null>(null);

    const page = findPage(slug);
    const markdown = page
        ? markdownFor(page.fileName) ?? (loadedPage?.fileName === page.fileName ? loadedPage.markdown : null)
        : null;

    useEffect(() => {
        if (!page || markdownFor(page.fileName) !== null) return;
        let active = true;
        loadMarkdown(page.fileName)
            .then((content) => {
                if (!active) return;
                if (content === null) {
                    setLoadError(page.fileName);
                    return;
                }
                setLoadedPage({ fileName: page.fileName, markdown: content });
                setLoadError(null);
            })
            .catch(() => {
                if (active) setLoadError(page.fileName);
            });
        return () => {
            active = false;
        };
    }, [page?.fileName]);

    useEffect(() => {
        const onKeyDown = (e: KeyboardEvent) => {
            if (e.ctrlKey || e.metaKey || e.altKey) {
                if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
                    e.preventDefault();
                    setSearchSeed((prev) => prev ?? "");
                }
                return;
            }
            const target = e.target as HTMLElement;
            if (
                target.tagName === "INPUT" ||
                target.tagName === "TEXTAREA" ||
                target.isContentEditable
            ) {
                return;
            }
            if (/^[a-zA-Z0-9]$/.test(e.key)) {
                e.preventDefault();
                setSearchSeed((prev) => prev ?? e.key);
            }
        };
        window.addEventListener("keydown", onKeyDown);
        return () => window.removeEventListener("keydown", onKeyDown);
    }, []);

    useEffect(() => {
        setSidebarOpen(false);
        if (location.hash) {
            const el = document.getElementById(location.hash.slice(1));
            if (el) {
                el.scrollIntoView();
                return;
            }
        }
        window.scrollTo({ top: 0 });
    }, [page?.slug, location.hash, markdown]);

    useEffect(() => {
        document.title = page
            ? page.slug === ""
                ? "SQLite ORM for .NET Documentation | SQLite.Framework"
                : `${page.title} | SQLite.Framework Docs`
            : "SQLite.Framework Docs";
    }, [page]);

    if (!page) return <Navigate to="/" replace />;

    return (
        <div className="docs-shell">
            <Sidebar
                open={sidebarOpen}
                onClose={() => setSidebarOpen(false)}
                onSearch={() => setSearchSeed("")}
            />
            {sidebarOpen && (
                <div className="docs-overlay" onClick={() => setSidebarOpen(false)} />
            )}
            <main className="docs-main" aria-busy={markdown === null && loadError !== page.fileName}>
                <div className="docs-mobilebar">
                    <button
                        type="button"
                        className="docs-iconbtn"
                        aria-label="Open navigation"
                        onClick={() => setSidebarOpen(true)}
                    >
                        <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M3 6h18M3 12h18M3 18h18" /></svg>
                    </button>
                    <a className="docs-mobilebrand" href="/">
                        <img src="/SQLite.Framework.png" alt="" width="24" height="24" />
                        <span>SQLite.Framework</span>
                    </a>
                    <button
                        type="button"
                        className="docs-iconbtn"
                        aria-label="Search docs"
                        onClick={() => setSearchSeed("")}
                    >
                        <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
                    </button>
                </div>
                <article className="docs-article" key={page.slug || "home"}>
                    {markdown !== null ? (
                        <>
                            <div className="docs-article-tools">
                                <TocMenu markdown={markdown} />
                                <OpenInMenu fileName={page.fileName} markdown={markdown} />
                            </div>
                            <MarkdownPage markdown={markdown} />
                            <PageNavigation current={page} />
                        </>
                    ) : (
                        <div className="docs-markdown">
                            <h1>{page.title}</h1>
                            {loadError === page.fileName ? (
                                <p role="alert">
                                    This page could not be loaded. <a href={`/Docs${location.pathname}${location.search}${location.hash}`}>Reload the page</a> to try again.
                                </p>
                            ) : (
                                <p role="status">Loading documentation...</p>
                            )}
                        </div>
                    )}
                </article>
            </main>
            <aside className="docs-rail">
                <TableOfContents markdown={markdown ?? ""} pageKey={page.slug} />
            </aside>
            {searchSeed !== null && (
                <SearchModal initialQuery={searchSeed} onClose={() => setSearchSeed(null)} />
            )}
        </div>
    );
}
