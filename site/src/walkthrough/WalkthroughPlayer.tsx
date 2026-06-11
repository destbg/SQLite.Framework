import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import type { Walkthrough } from "./walkthroughs/types";
import { CodeBlock } from "../highlight/CodeBlock";

interface WalkthroughPlayerProps {
    walkthrough: Walkthrough;
}

export function WalkthroughPlayer({ walkthrough }: WalkthroughPlayerProps) {
    const navigate = useNavigate();
    const [searchParams, setSearchParams] = useSearchParams();
    const total = walkthrough.steps.length;

    const initial = (() => {
        const raw = Number(searchParams.get("step"));
        return Number.isInteger(raw) && raw >= 1 && raw <= total ? raw - 1 : 0;
    })();

    const [index, setIndex] = useState(initial);
    const [direction, setDirection] = useState<"fwd" | "back">("fwd");
    const stageRef = useRef<HTMLElement>(null);

    const goTo = useCallback(
        (next: number) => {
            const clamped = Math.max(0, Math.min(total - 1, next));
            setIndex((current) => {
                if (clamped === current) return current;
                setDirection(clamped > current ? "fwd" : "back");
                return clamped;
            });
        },
        [total],
    );

    useEffect(() => {
        setSearchParams(index === 0 ? {} : { step: String(index + 1) }, { replace: true });
        stageRef.current?.scrollTo({ top: 0 });
    }, [index, setSearchParams]);

    useEffect(() => {
        document.title = `${walkthrough.title} - SQLite.Framework`;
    }, [walkthrough.title]);

    useEffect(() => {
        const onKeyDown = (e: KeyboardEvent) => {
            if (e.ctrlKey || e.metaKey || e.altKey) return;
            switch (e.key) {
                case "ArrowRight":
                case "PageDown":
                case " ":
                    e.preventDefault();
                    goTo(index + 1);
                    break;
                case "ArrowLeft":
                case "PageUp":
                    e.preventDefault();
                    goTo(index - 1);
                    break;
                case "Home":
                    e.preventDefault();
                    goTo(0);
                    break;
                case "End":
                    e.preventDefault();
                    goTo(total - 1);
                    break;
                case "Escape":
                    e.preventDefault();
                    navigate("/");
                    break;
            }
        };
        window.addEventListener("keydown", onKeyDown);
        return () => window.removeEventListener("keydown", onKeyDown);
    }, [index, total, goTo, navigate]);

    const step = walkthrough.steps[index];
    const isLast = index === total - 1;

    return (
        <div className="wt-player" data-accent={walkthrough.slug}>
            <header className="wt-player-head">
                <Link to="/" className="wt-brand">
                    <img src="/SQLite.Framework.png" alt="" width="26" height="26" />
                    <span>{walkthrough.title}</span>
                </Link>
                <div className="wt-progress" role="progressbar" aria-valuemin={1} aria-valuemax={total} aria-valuenow={index + 1}>
                    <div
                        className="wt-progress-fill"
                        style={{ width: `${((index + 1) / total) * 100}%` }}
                    />
                </div>
                <span className="wt-counter">
                    {index + 1} / {total}
                </span>
                <a className="wt-docs-link" href="/Docs/">
                    Open the docs
                </a>
            </header>

            <main className="wt-stage" ref={stageRef}>
                <section
                    key={index}
                    className={`wt-step wt-step-${direction}`}
                    aria-label={`Step ${index + 1} of ${total}`}
                >
                    <p className="wt-step-number">step {String(index + 1).padStart(2, "0")}</p>
                    <h1>{step.title}</h1>
                    <p className="wt-step-desc">{step.description}</p>
                    {step.code && (
                        <CodeBlock
                            code={step.code.text}
                            language={step.code.language}
                            filename={step.code.filename}
                        />
                    )}
                </section>
            </main>

            <footer className="wt-controls">
                <button
                    type="button"
                    className="wt-nav-btn"
                    disabled={index === 0}
                    onClick={() => goTo(index - 1)}
                >
                    <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M19 12H5M11 18l-6-6 6-6" /></svg>
                    Back
                </button>
                <div className="wt-dots">
                    {walkthrough.steps.map((s, i) => (
                        <button
                            key={s.title}
                            type="button"
                            aria-label={`Go to step ${i + 1}`}
                            className={
                                i === index ? "is-current" : i < index ? "is-done" : undefined
                            }
                            onClick={() => goTo(i)}
                        />
                    ))}
                </div>
                {isLast ? (
                    <Link to="/" className="wt-nav-btn wt-nav-primary">
                        Pick another walkthrough
                    </Link>
                ) : (
                    <button
                        type="button"
                        className="wt-nav-btn wt-nav-primary"
                        onClick={() => goTo(index + 1)}
                    >
                        Next
                        <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M5 12h14M13 6l6 6-6 6" /></svg>
                    </button>
                )}
            </footer>
        </div>
    );
}
