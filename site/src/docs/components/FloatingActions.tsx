import { useLocation } from "react-router-dom";
import { findPageBySlug } from "../pages";
import { loadContent } from "../markdownFiles";
import { parseHeadings } from "../utils";
import OpenInButton from "./OpenInButton";
import FloatingTocButton from "./FloatingTocButton";

export default function FloatingActions() {
    const { pathname } = useLocation();
    const slug = decodeURIComponent(pathname.replace(/^\/+/, "")) || "Home";
    const page = findPageBySlug(slug);
    if (!page) return null;
    const markdown = loadContent(page.title);
    const headings = parseHeadings(markdown);
    return (
        <div className="floating-actions">
            <OpenInButton fileName={page.title} markdown={markdown} />
            <FloatingTocButton headings={headings} />
        </div>
    );
}
