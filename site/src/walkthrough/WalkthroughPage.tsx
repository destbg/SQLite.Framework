import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import WalkthroughStepView from "./WalkthroughStepView";
import type { Walkthrough } from "./walkthroughs/types";

const FADE_MS = 260;

interface Props {
    walkthrough: Walkthrough;
}

export default function WalkthroughPage({ walkthrough }: Props) {
    const navigate = useNavigate();
    const [searchParams, setSearchParams] = useSearchParams();
    const total = walkthrough.steps.length;

    const startIdx = useMemo(() => {
        const raw = parseInt(searchParams.get("step") ?? "1", 10);
        if (Number.isNaN(raw)) return 0;
        return Math.min(Math.max(0, raw - 1), total - 1);
    }, [searchParams, total]);

    const [currentIdx, setCurrentIdx] = useState(startIdx);
    const [outgoingIdx, setOutgoingIdx] = useState<number | null>(null);
    const [direction, setDirection] = useState<1 | -1>(1);
    const transitionTimer = useRef<number | null>(null);

    useEffect(() => {
        setCurrentIdx(startIdx);
    }, [startIdx]);

    useEffect(() => {
        const next = currentIdx + 1;
        const params = new URLSearchParams(searchParams);
        params.set("step", String(next));
        setSearchParams(params, { replace: true });
    }, [currentIdx]);

    const goTo = useCallback(
        (next: number) => {
            const clamped = Math.min(Math.max(0, next), total - 1);
            if (clamped === currentIdx) return;
            setDirection(clamped > currentIdx ? 1 : -1);
            setOutgoingIdx(currentIdx);
            setCurrentIdx(clamped);
            if (transitionTimer.current != null) {
                window.clearTimeout(transitionTimer.current);
            }
            transitionTimer.current = window.setTimeout(() => {
                setOutgoingIdx(null);
                transitionTimer.current = null;
            }, FADE_MS);
        },
        [currentIdx, total],
    );

    useEffect(() => {
        return () => {
            if (transitionTimer.current != null) {
                window.clearTimeout(transitionTimer.current);
            }
        };
    }, []);

    useEffect(() => {
        function onKey(e: KeyboardEvent) {
            if (e.metaKey || e.ctrlKey || e.altKey) return;
            const target = e.target as HTMLElement | null;
            if (target) {
                const tag = target.tagName;
                if (tag === "INPUT" || tag === "TEXTAREA" || target.isContentEditable) {
                    return;
                }
            }
            if (e.key === "ArrowRight" || e.key === "PageDown" || e.key === " ") {
                e.preventDefault();
                goTo(currentIdx + 1);
            } else if (e.key === "ArrowLeft" || e.key === "PageUp") {
                e.preventDefault();
                goTo(currentIdx - 1);
            } else if (e.key === "Home") {
                e.preventDefault();
                goTo(0);
            } else if (e.key === "End") {
                e.preventDefault();
                goTo(total - 1);
            } else if (e.key === "Escape") {
                e.preventDefault();
                navigate("/");
            }
        }
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [currentIdx, goTo, navigate, total]);

    const progress = ((currentIdx + 1) / total) * 100;
    const isLast = currentIdx === total - 1;
    const isFirst = currentIdx === 0;

    return (
        <div className="wt">
            <header className="wt-header">
                <Link to="/" className="wt-brand">
                    <img src="/SQLite.Framework.png" alt="" className="wt-logo" />
                    <span className="wt-brand-name">{walkthrough.title}</span>
                </Link>
                <div className="wt-progress" aria-hidden="true">
                    <div className="wt-progress-bar" style={{ width: `${progress}%` }} />
                </div>
                <div className="wt-actions">
                    <span className="wt-counter">
                        {currentIdx + 1} / {total}
                    </span>
                    <a href="/Docs/" className="wt-exit">Open the docs</a>
                </div>
            </header>

            <main className="wt-stage">
                {outgoingIdx != null && (
                    <WalkthroughStepView
                        key={`out-${outgoingIdx}`}
                        step={walkthrough.steps[outgoingIdx]}
                        index={outgoingIdx}
                        total={total}
                        phase="out"
                        direction={direction}
                    />
                )}
                <WalkthroughStepView
                    key={`in-${currentIdx}`}
                    step={walkthrough.steps[currentIdx]}
                    index={currentIdx}
                    total={total}
                    phase="in"
                    direction={direction}
                />
            </main>

            <footer className="wt-controls">
                <button
                    type="button"
                    className="wt-btn wt-btn-secondary"
                    onClick={() => goTo(currentIdx - 1)}
                    disabled={isFirst}
                >
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                        <line x1="19" y1="12" x2="5" y2="12" />
                        <polyline points="12 19 5 12 12 5" />
                    </svg>
                    Back
                </button>
                <div className="wt-dots" role="tablist" aria-label="Steps">
                    {walkthrough.steps.map((_, i) => (
                        <button
                            key={i}
                            type="button"
                            role="tab"
                            aria-selected={i === currentIdx}
                            aria-label={`Step ${i + 1}`}
                            className={`wt-dot${i === currentIdx ? " wt-dot--active" : ""}${i < currentIdx ? " wt-dot--past" : ""}`}
                            onClick={() => goTo(i)}
                        />
                    ))}
                </div>
                {isLast ? (
                    <Link to="/" className="wt-btn wt-btn-primary">
                        Pick another walkthrough
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                            <line x1="5" y1="12" x2="19" y2="12" />
                            <polyline points="12 5 19 12 12 19" />
                        </svg>
                    </Link>
                ) : (
                    <button
                        type="button"
                        className="wt-btn wt-btn-primary"
                        onClick={() => goTo(currentIdx + 1)}
                    >
                        Next
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                            <line x1="5" y1="12" x2="19" y2="12" />
                            <polyline points="12 5 19 12 12 19" />
                        </svg>
                    </button>
                )}
            </footer>
        </div>
    );
}
