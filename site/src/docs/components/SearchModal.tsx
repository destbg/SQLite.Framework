import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { searchDocs } from "../search";

interface SearchModalProps {
    initialQuery: string;
    onClose: () => void;
}

function markTokens(text: string, tokens: string[]): ReactNode[] {
    if (tokens.length === 0) return [text];
    const escaped = tokens.map((t) => t.replace(/[.*+?^${}()|[\]\\]/g, "\\$&"));
    const splitter = new RegExp(`(${escaped.join("|")})`, "ig");
    return text.split(splitter).map((part, i) =>
        tokens.some((t) => part.toLowerCase() === t) ? <mark key={i}>{part}</mark> : part,
    );
}

export function SearchModal({ initialQuery, onClose }: SearchModalProps) {
    const [query, setQuery] = useState(initialQuery);
    const [active, setActive] = useState(0);
    const inputRef = useRef<HTMLInputElement>(null);
    const listRef = useRef<HTMLUListElement>(null);
    const navigate = useNavigate();

    const results = useMemo(() => searchDocs(query), [query]);
    const tokens = useMemo(
        () => query.toLowerCase().split(/\s+/).filter(Boolean),
        [query],
    );

    useEffect(() => {
        const input = inputRef.current;
        if (input) {
            input.focus();
            input.setSelectionRange(input.value.length, input.value.length);
        }
    }, []);

    useEffect(() => {
        setActive(0);
    }, [query]);

    useEffect(() => {
        listRef.current
            ?.querySelector(".is-active")
            ?.scrollIntoView({ block: "nearest" });
    }, [active]);

    const open = (index: number) => {
        const result = results[index];
        if (!result) return;
        navigate(`/${result.page.slug}`);
        onClose();
    };

    const onKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === "Escape") {
            e.preventDefault();
            onClose();
        } else if (e.key === "ArrowDown") {
            e.preventDefault();
            setActive((i) => Math.min(results.length - 1, i + 1));
        } else if (e.key === "ArrowUp") {
            e.preventDefault();
            setActive((i) => Math.max(0, i - 1));
        } else if (e.key === "Enter") {
            e.preventDefault();
            open(active);
        }
    };

    return (
        <div className="docs-search-backdrop" onClick={onClose}>
            <div
                className="docs-search-modal"
                role="dialog"
                aria-label="Search documentation"
                onClick={(e) => e.stopPropagation()}
            >
                <div className="docs-search-inputrow">
                    <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round"><circle cx="11" cy="11" r="7" /><path d="m21 21-4.3-4.3" /></svg>
                    <input
                        ref={inputRef}
                        value={query}
                        placeholder="Search the docs"
                        onChange={(e) => setQuery(e.target.value)}
                        onKeyDown={onKeyDown}
                    />
                    <kbd>esc</kbd>
                </div>
                {query.trim() !== "" && (
                    <ul className="docs-search-results" ref={listRef}>
                        {results.length === 0 && (
                            <li className="docs-search-empty">No results for "{query}"</li>
                        )}
                        {results.map((result, i) => (
                            <li key={result.page.slug || "home"}>
                                <button
                                    type="button"
                                    className={i === active ? "is-active" : undefined}
                                    onMouseEnter={() => setActive(i)}
                                    onClick={() => open(i)}
                                >
                                    <span className="docs-search-result-title">
                                        {result.page.title}
                                        {result.heading && (
                                            <small> &gt; {result.heading}</small>
                                        )}
                                    </span>
                                    <span className="docs-search-result-snippet">
                                        {markTokens(result.snippet, tokens)}
                                    </span>
                                </button>
                            </li>
                        ))}
                    </ul>
                )}
            </div>
        </div>
    );
}
