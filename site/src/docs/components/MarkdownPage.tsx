import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { type Page } from "../pages";
import PageLayer from "./PageLayer";
import PageNavigation from "./PageNavigation";

const FADE_DURATION = 200;
const HEIGHT_LOCK_DURATION = 700;

interface Props {
    page: Page;
}

export default function MarkdownPage({ page }: Props) {
    const navigate = useNavigate();
    const articleRef = useRef<HTMLElement>(null);
    const prevPageRef = useRef(page);
    const [displayPage, setDisplayPage] = useState(page);
    const [outgoingPage, setOutgoingPage] = useState<Page | null>(null);
    const [lockedHeight, setLockedHeight] = useState<number | null>(null);

    useEffect(() => {
        if (page.slug === prevPageRef.current.slug) return;

        const outgoing = prevPageRef.current;
        prevPageRef.current = page;

        const currentHeight = articleRef.current?.offsetHeight ?? 0;
        setLockedHeight(currentHeight);
        setOutgoingPage(outgoing);
        setDisplayPage(page);

        const raf = requestAnimationFrame(() => {
            window.scrollTo({ top: 0, behavior: "smooth" });
        });

        const fadeTimer = window.setTimeout(() => {
            setOutgoingPage(null);
        }, FADE_DURATION);

        const heightTimer = window.setTimeout(() => {
            setLockedHeight(null);
        }, HEIGHT_LOCK_DURATION);

        return () => {
            cancelAnimationFrame(raf);
            window.clearTimeout(fadeTimer);
            window.clearTimeout(heightTimer);
        };
    }, [page]);

    const handleLinkClick = (href: string) =>
        navigate(href === "Home" ? "/" : `/${href}`);

    return (
        <article
            ref={articleRef}
            className="markdown-content page-stage"
            style={lockedHeight != null ? { minHeight: lockedHeight } : undefined}
        >
            <div key={displayPage.slug} className="page-layer page-layer--in">
                <PageLayer page={displayPage} onLinkClick={handleLinkClick} />
                <PageNavigation slug={displayPage.slug} />
            </div>
            {outgoingPage && (
                <div
                    key={outgoingPage.slug}
                    className="page-layer page-layer--out"
                    aria-hidden="true"
                >
                    <PageLayer page={outgoingPage} onLinkClick={handleLinkClick} />
                    <PageNavigation slug={outgoingPage.slug} />
                </div>
            )}
        </article>
    );
}
