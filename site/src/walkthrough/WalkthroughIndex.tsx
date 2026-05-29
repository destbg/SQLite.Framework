import { Link } from "react-router-dom";
import { walkthroughList } from "./walkthroughs";

export default function WalkthroughIndex() {
    return (
        <div className="index">
            <header className="index-header">
                <Link to="/" className="index-brand">
                    <img src="/SQLite.Framework.png" alt="" className="index-logo" />
                    <span>SQLite.Framework</span>
                </Link>
                <a href="/" className="index-exit">Back to home</a>
            </header>
            <main className="index-main">
                <h1 className="index-title">Walkthroughs</h1>
                <p className="index-sub">
                    Step-by-step guides for adding SQLite.Framework to a fresh project.
                </p>
                <div className="index-grid">
                    {walkthroughList.map((w) => (
                        <Link key={w.slug} to={`/${w.slug}`} className="index-card">
                            <span className="index-card-eyebrow">
                                {w.steps.length} steps
                            </span>
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
