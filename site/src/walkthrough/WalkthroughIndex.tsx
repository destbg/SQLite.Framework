import type { CSSProperties } from "react";
import { Link } from "react-router-dom";
import { walkthroughList } from "./walkthroughs";

const accents: Record<string, string> = {
    console: "#58a6ff",
    maui: "#c297ff",
    avalonia: "#3fb950",
    aspnet: "#f0883e",
    blazor: "#ff7b9d",
};

function icon(slug: string) {
    switch (slug) {
        case "console":
            return (
                <>
                    <polyline points="4 17 10 11 4 5" />
                    <line x1="12" y1="19" x2="20" y2="19" />
                </>
            );
        case "maui":
            return (
                <>
                    <rect x="6" y="2" width="12" height="20" rx="2.5" />
                    <line x1="11" y1="18" x2="13" y2="18" />
                </>
            );
        case "avalonia":
            return (
                <>
                    <polygon points="12 2 2 7 12 12 22 7 12 2" />
                    <polyline points="2 17 12 22 22 17" />
                    <polyline points="2 12 12 17 22 12" />
                </>
            );
        case "aspnet":
            return (
                <>
                    <rect x="2" y="3" width="20" height="8" rx="2" />
                    <rect x="2" y="13" width="20" height="8" rx="2" />
                    <line x1="6" y1="7" x2="6.01" y2="7" />
                    <line x1="6" y1="17" x2="6.01" y2="17" />
                </>
            );
        default:
            return (
                <>
                    <circle cx="12" cy="12" r="10" />
                    <line x1="2" y1="12" x2="22" y2="12" />
                    <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
                </>
            );
    }
}

export default function WalkthroughIndex() {
    return (
        <div className="index">
            <div className="index-grid-bg" aria-hidden="true" />
            <header className="index-header">
                <Link to="/" className="index-brand">
                    <img src="/SQLite.Framework.png" alt="" className="index-logo" />
                    <span>SQLite.Framework</span>
                </Link>
                <a href="/" className="index-exit">Back to home</a>
            </header>
            <main className="index-main">
                <div className="index-hero">
                    <span className="index-eyebrow">Guided setup</span>
                    <h1 className="index-title">Walkthroughs</h1>
                    <p className="index-sub">
                        Step-by-step guides for adding SQLite.Framework to a fresh project.
                    </p>
                </div>
                <div className="index-grid">
                    {walkthroughList.map((w, i) => (
                        <Link
                            key={w.slug}
                            to={`/${w.slug}`}
                            className="index-card"
                            style={{
                                "--card-accent": accents[w.slug] ?? "#58a6ff",
                                "--card-index": i,
                            } as CSSProperties}
                        >
                            <div className="index-card-top">
                                <span className="index-card-icon">
                                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                                        {icon(w.slug)}
                                    </svg>
                                </span>
                                <span className="index-card-steps">{w.steps.length} steps</span>
                            </div>
                            <h2 className="index-card-title">{w.title}</h2>
                            <p className="index-card-sub">{w.subtitle}</p>
                            <span className="index-card-cta">
                                Start
                                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                                    <line x1="5" y1="12" x2="19" y2="12" />
                                    <polyline points="12 5 19 12 12 19" />
                                </svg>
                            </span>
                        </Link>
                    ))}
                </div>
            </main>
        </div>
    );
}
