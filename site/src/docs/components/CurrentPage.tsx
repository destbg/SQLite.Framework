import { Navigate, useLocation } from "react-router-dom";
import MarkdownPage from "./MarkdownPage";
import { findPageBySlug } from "../pages";

export default function CurrentPage() {
    const { pathname } = useLocation();
    const slug = decodeURIComponent(pathname.replace(/^\/+/, "")) || "Home";
    const page = findPageBySlug(slug);
    if (!page) return <Navigate to="/" replace />;
    return <MarkdownPage page={page} />;
}
