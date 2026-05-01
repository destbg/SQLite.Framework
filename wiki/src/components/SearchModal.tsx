import { useEffect, useMemo, useRef, useState } from "react";
import type { KeyboardEvent, ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { search } from "../search";
import type { SearchHit } from "../search";

interface Props {
  open: boolean;
  initialQuery: string;
  onClose: () => void;
}

const EXIT_DURATION = 140;

function escapeRegex(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function highlight(text: string, query: string): ReactNode {
  const tokens = query.trim().split(/\s+/).filter(Boolean);
  if (tokens.length === 0) return text;
  const pattern = new RegExp(`(${tokens.map(escapeRegex).join("|")})`, "gi");
  const parts = text.split(pattern);
  return parts.map((part, i) =>
    i % 2 === 1 ? <mark key={i}>{part}</mark> : <span key={i}>{part}</span>,
  );
}

export default function SearchModal({ open, initialQuery, onClose }: Props) {
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLUListElement>(null);
  const [query, setQuery] = useState(initialQuery);
  const [activeIdx, setActiveIdx] = useState(0);
  const [mounted, setMounted] = useState(open);
  const [closing, setClosing] = useState(false);

  useEffect(() => {
    if (open) {
      setMounted(true);
      setClosing(false);
      return;
    }
    if (!mounted) return;
    setClosing(true);
    const timer = window.setTimeout(() => {
      setMounted(false);
      setClosing(false);
    }, EXIT_DURATION);
    return () => window.clearTimeout(timer);
  }, [open, mounted]);

  useEffect(() => {
    if (!open) return;
    setQuery(initialQuery);
    setActiveIdx(0);
    const raf = requestAnimationFrame(() => {
      const input = inputRef.current;
      if (!input) return;
      input.focus();
      const len = input.value.length;
      input.setSelectionRange(len, len);
    });
    return () => cancelAnimationFrame(raf);
  }, [open, initialQuery]);

  const results = useMemo(
    () => (mounted ? search(query) : []),
    [mounted, query],
  );

  useEffect(() => {
    if (activeIdx >= results.length) setActiveIdx(0);
  }, [results.length, activeIdx]);

  useEffect(() => {
    const list = listRef.current;
    if (!list) return;
    const active = list.querySelector<HTMLLIElement>(".search-result--active");
    active?.scrollIntoView({ block: "nearest" });
  }, [activeIdx]);

  function go(hit: SearchHit) {
    const path = hit.slug === "Home" ? "/" : `/${hit.slug}`;
    onClose();
    navigate(path);
  }

  function onKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Escape") {
      e.preventDefault();
      onClose();
    } else if (e.key === "ArrowDown") {
      e.preventDefault();
      setActiveIdx((i) =>
        results.length === 0 ? 0 : Math.min(i + 1, results.length - 1),
      );
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIdx((i) => Math.max(i - 1, 0));
    } else if (e.key === "Enter") {
      const hit = results[activeIdx];
      if (hit) {
        e.preventDefault();
        go(hit);
      }
    }
  }

  if (!mounted) return null;

  return (
    <div
      className={`search-overlay${closing ? " search-overlay--closing" : ""}`}
      onMouseDown={onClose}
    >
      <div
        className={`search-modal${closing ? " search-modal--closing" : ""}`}
        role="dialog"
        aria-modal="true"
        aria-label="Search documentation"
        onMouseDown={(e) => e.stopPropagation()}
      >
        <div className="search-input-wrap">
          <svg
            className="search-input-icon"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <circle cx="11" cy="11" r="7" />
            <line x1="21" y1="21" x2="16.65" y2="16.65" />
          </svg>
          <input
            ref={inputRef}
            className="search-input"
            type="text"
            placeholder="Search the docs…"
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
              setActiveIdx(0);
            }}
            onKeyDown={onKeyDown}
            autoComplete="off"
            spellCheck={false}
            aria-label="Search the docs"
          />
          <kbd className="search-esc" onClick={onClose}>
            Esc
          </kbd>
        </div>
        <ul ref={listRef} className="search-results">
          {query.trim() && results.length === 0 && (
            <li className="search-empty">No results for "{query.trim()}"</li>
          )}
          {!query.trim() && (
            <li className="search-empty">Type to search across all pages.</li>
          )}
          {results.map((hit, i) => (
            <li
              key={`${hit.slug}-${i}`}
              className={`search-result${i === activeIdx ? " search-result--active" : ""}`}
              onMouseEnter={() => setActiveIdx(i)}
              onMouseDown={(e) => {
                e.preventDefault();
                go(hit);
              }}
            >
              <div className="search-result-title">
                {hit.title}
                {hit.matchedHeading && hit.matchedHeading !== hit.title && (
                  <span className="search-result-heading">
                    {" "}
                    › {hit.matchedHeading}
                  </span>
                )}
              </div>
              <div className="search-result-snippet">
                {highlight(hit.snippet, query)}
              </div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
