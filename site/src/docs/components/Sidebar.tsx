import { useState } from "react";
import { NavLink } from "react-router-dom";
import { docGroups } from "../pages";
import { toggleTheme } from "../../shared/theme";

interface SidebarProps {
    open: boolean;
    onClose: () => void;
    onSearch: () => void;
}

export function Sidebar({ open, onClose, onSearch }: SidebarProps) {
    const [collapsed, setCollapsed] = useState<Set<string>>(new Set());

    const toggleGroup = (title: string) => {
        setCollapsed((prev) => {
            const next = new Set(prev);
            if (next.has(title)) next.delete(title);
            else next.add(title);
            return next;
        });
    };

    return (
        <aside className={open ? "docs-sidebar is-open" : "docs-sidebar"}>
            <div className="docs-sidebar-head">
                <a className="docs-sidebar-brand" href="/">
                    <img src="/SQLite.Framework.png" alt="" width="28" height="28" />
                    <span>
                        SQLite.Framework
                        <small>Docs</small>
                    </span>
                </a>
                <button
                    type="button"
                    className="docs-iconbtn docs-sidebar-close"
                    aria-label="Close navigation"
                    onClick={onClose}
                >
                    <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><path d="M18 6 6 18M6 6l12 12" /></svg>
                </button>
            </div>
            <button type="button" className="docs-search-trigger" onClick={onSearch}>
                <svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
                <span>Search docs</span>
                <kbd>type to search</kbd>
            </button>
            <nav className="docs-nav" aria-label="Documentation">
                {docGroups.map((group) =>
                    group.title === null ? (
                        <ul key="root" className="docs-nav-list">
                            {group.pages.map((p) => (
                                <li key={p.slug}>
                                    <NavLink to={`/${p.slug}`} end>
                                        {p.title}
                                    </NavLink>
                                </li>
                            ))}
                        </ul>
                    ) : (
                        <div key={group.title} className="docs-nav-group">
                            <button
                                type="button"
                                className="docs-nav-grouphead"
                                aria-expanded={!collapsed.has(group.title)}
                                onClick={() => toggleGroup(group.title!)}
                            >
                                <span>{group.title}</span>
                                <svg viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="m6 9 6 6 6-6" /></svg>
                            </button>
                            {!collapsed.has(group.title) && (
                                <ul className="docs-nav-list">
                                    {group.pages.map((p) => (
                                        <li key={p.slug}>
                                            <NavLink to={`/${p.slug}`} end>
                                                {p.title}
                                            </NavLink>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </div>
                    ),
                )}
            </nav>
            <div className="docs-sidebar-foot">
                <a href="https://github.com/destbg/SQLite.Framework" target="_blank" rel="noopener noreferrer">
                    GitHub
                </a>
                <a href="https://www.nuget.org/packages/SQLite.Framework/" target="_blank" rel="noopener noreferrer">
                    NuGet
                </a>
                <button type="button" className="docs-iconbtn" aria-label="Toggle theme" onClick={toggleTheme}>
                    <svg className="icon-sun" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="12" cy="12" r="4" /><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" /></svg>
                    <svg className="icon-moon" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z" /></svg>
                </button>
            </div>
        </aside>
    );
}
