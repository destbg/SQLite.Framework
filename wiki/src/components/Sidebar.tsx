import { useEffect, useState } from "react";
import { NavLink } from "react-router-dom";
import { sections, ungrouped } from "../pages";

interface Props {
  onOpenSearch: () => void;
}

export default function Sidebar({ onOpenSearch }: Props) {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [isDark, setIsDark] = useState(
    () => localStorage.getItem("theme") !== "light",
  );
  const [collapsed, setCollapsed] = useState<Record<string, boolean>>({});

  useEffect(() => {
    if (isDark) {
      document.documentElement.removeAttribute("data-theme");
      localStorage.removeItem("theme");
    } else {
      document.documentElement.setAttribute("data-theme", "light");
      localStorage.setItem("theme", "light");
    }
  }, [isDark]);

  function toggleSection(section: string) {
    setCollapsed((prev) => ({ ...prev, [section]: !prev[section] }));
  }

  return (
    <>
      <button
        className="mobile-menu-btn"
        onClick={() => setMobileOpen((o) => !o)}
        aria-label="Toggle navigation"
      >
        <span />
        <span />
        <span />
      </button>

      <aside className={`sidebar${mobileOpen ? " sidebar--open" : ""}`}>
        <div className="sidebar-header">
          <NavLink
            to="/"
            className="sidebar-title"
            onClick={() => setMobileOpen(false)}
          >
            <img
              src={`${import.meta.env.BASE_URL}sqlite.png`}
              alt=""
              className="sidebar-logo"
            />
            <span>SQLite.Framework</span>
            <span className="sidebar-subtitle">Docs</span>
          </NavLink>
        </div>

        <nav className="sidebar-nav">
          {ungrouped.map((page) => (
            <NavLink
              key={page.slug}
              to="/"
              end
              className={({ isActive }) =>
                `nav-item${isActive ? " nav-item--active" : ""}`
              }
              onClick={() => setMobileOpen(false)}
            >
              {page.title}
            </NavLink>
          ))}

          {sections.map(({ title, pages: sectionPages }) => (
            <div key={title} className="nav-section">
              <button
                className="nav-section-header"
                onClick={() => toggleSection(title)}
                aria-expanded={!collapsed[title]}
              >
                <span>{title}</span>
                <svg
                  className={`nav-section-chevron${collapsed[title] ? " nav-section-chevron--collapsed" : ""}`}
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                >
                  <polyline points="6 9 12 15 18 9" />
                </svg>
              </button>

              {!collapsed[title] &&
                sectionPages.map((page) => (
                  <NavLink
                    key={page.slug}
                    to={`/${page.slug}`}
                    className={({ isActive }) =>
                      `nav-item nav-item--indented${isActive ? " nav-item--active" : ""}`
                    }
                    onClick={() => setMobileOpen(false)}
                  >
                    {page.title}
                  </NavLink>
                ))}
            </div>
          ))}
        </nav>

        <div className="sidebar-footer">
          <a
            href="https://github.com/destbg/SQLite.Framework"
            target="_blank"
            rel="noopener noreferrer"
            className="sidebar-link"
          >
            GitHub
          </a>
          <a
            href="https://www.nuget.org/packages/SQLite.Framework/"
            target="_blank"
            rel="noopener noreferrer"
            className="sidebar-link"
          >
            NuGet
          </a>
          <button
            className="sidebar-icon-btn sidebar-search-btn"
            onClick={() => {
              setMobileOpen(false);
              onOpenSearch();
            }}
            aria-label="Search the docs"
            title="Search (or press any letter)"
          >
            <svg
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            >
              <circle cx="11" cy="11" r="7" />
              <line x1="21" y1="21" x2="16.65" y2="16.65" />
            </svg>
          </button>
          <button
            className="sidebar-icon-btn theme-toggle"
            onClick={() => setIsDark((d) => !d)}
            aria-label={
              isDark ? "Switch to light theme" : "Switch to dark theme"
            }
          >
            {isDark ? (
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <circle cx="12" cy="12" r="5" />
                <line x1="12" y1="1" x2="12" y2="3" />
                <line x1="12" y1="21" x2="12" y2="23" />
                <line x1="4.22" y1="4.22" x2="5.64" y2="5.64" />
                <line x1="18.36" y1="18.36" x2="19.78" y2="19.78" />
                <line x1="1" y1="12" x2="3" y2="12" />
                <line x1="21" y1="12" x2="23" y2="12" />
                <line x1="4.22" y1="19.78" x2="5.64" y2="18.36" />
                <line x1="18.36" y1="5.64" x2="19.78" y2="4.22" />
              </svg>
            ) : (
              <svg
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
              </svg>
            )}
          </button>
        </div>
      </aside>

      {mobileOpen && (
        <div className="sidebar-overlay" onClick={() => setMobileOpen(false)} />
      )}
    </>
  );
}
