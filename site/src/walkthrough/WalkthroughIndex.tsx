import { useEffect } from "react";
import { Link } from "react-router-dom";
import { walkthroughs } from "./walkthroughs";
import { toggleTheme } from "../shared/theme";

const icons: Record<string, React.ReactNode> = {
    console: (
        <svg viewBox="0 0 24 24" width="28" height="28" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><rect x="2" y="4" width="20" height="16" rx="2" /><path d="m6 9 3 3-3 3M12 15h6" /></svg>
    ),
    maui: (
        <svg viewBox="0 0 24 24" width="28" height="28" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><rect x="7" y="2" width="10" height="20" rx="2" /><path d="M11 18h2" /></svg>
    ),
    avalonia: (
        <svg viewBox="0 0 24 24" width="28" height="28" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><rect x="2" y="3" width="20" height="13" rx="2" /><path d="M8 21h8M12 16v5" /></svg>
    ),
    aspnet: (
        <svg viewBox="0 0 24 24" width="28" height="28" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10" /><path d="M2 12h20M12 2a15 15 0 0 1 0 20 15 15 0 0 1 0-20z" /></svg>
    ),
    blazor: (
        <svg viewBox="0 0 24 24" width="28" height="28" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M12 2c3 4-2 6 1 9 2-1 2-2.5 2-4 3 2.5 4 5 4 7a7 7 0 0 1-14 0c0-3 2-5.5 4-7.5C10 5 10 3.5 12 2z" /></svg>
    ),
};

export function WalkthroughIndex() {
    useEffect(() => {
        document.title = "SQLite.Framework Walkthroughs";
    }, []);

    return (
        <div className="wt-index">
            <header className="wt-header">
                <div className="wt-header-inner">
                    <a className="wt-brand" href="/">
                        <img src="/SQLite.Framework.png" alt="" width="28" height="28" />
                        <span>SQLite.Framework</span>
                    </a>
                    <nav className="wt-header-links">
                        <a href="/">Back to home</a>
                        <a href="/Docs/">Docs</a>
                        <button type="button" className="wt-iconbtn" aria-label="Toggle theme" onClick={toggleTheme}>
                            <svg className="icon-sun" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="12" cy="12" r="4" /><path d="M12 2v2M12 20v2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M2 12h2M20 12h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" /></svg>
                            <svg className="icon-moon" viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M21 12.8A9 9 0 1 1 11.2 3a7 7 0 0 0 9.8 9.8z" /></svg>
                        </button>
                    </nav>
                </div>
            </header>
            <main className="wt-index-main">
                <p className="eyebrow">guided setup</p>
                <h1>Walkthroughs</h1>
                <p className="wt-index-sub">
                    Step-by-step guides for adding SQLite.Framework to a fresh project.
                </p>
                <div className="wt-grid">
                    {walkthroughs.map((w) => (
                        <Link
                            key={w.slug}
                            to={`/${w.slug}`}
                            className="wt-card"
                            data-accent={w.slug}
                        >
                            <div className="wt-card-top">
                                <span className="wt-card-icon">{icons[w.slug]}</span>
                                <span className="wt-card-steps">{w.steps.length} steps</span>
                            </div>
                            <h2>{w.title}</h2>
                            <p>{w.subtitle}</p>
                            <span className="wt-card-cta">
                                Start
                                <svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12h14M13 6l6 6-6 6" /></svg>
                            </span>
                        </Link>
                    ))}
                </div>
            </main>
        </div>
    );
}
