import { useCallback, useEffect, useState } from "react";
import {
  HashRouter,
  Navigate,
  Route,
  Routes,
  useLocation,
} from "react-router-dom";
import Sidebar from "./components/Sidebar";
import MarkdownPage from "./components/MarkdownPage";
import SearchModal from "./components/SearchModal";
import TableOfContents from "./components/TableOfContents";
import { pages } from "./pages";

function CurrentPage() {
  const { pathname } = useLocation();
  const slug = decodeURIComponent(pathname.replace(/^\/+/, "")) || "Home";
  const page = pages.find((p) => p.slug === slug);
  if (!page) return <Navigate to="/" replace />;
  return <MarkdownPage slug={page.slug} />;
}

export default function App() {
  const [searchOpen, setSearchOpen] = useState(false);
  const [searchInitial, setSearchInitial] = useState("");

  const openSearch = useCallback((initial = "") => {
    setSearchInitial(initial);
    setSearchOpen(true);
  }, []);

  const closeSearch = useCallback(() => setSearchOpen(false), []);

  useEffect(() => {
    function onKeyDown(e: KeyboardEvent) {
      if (e.metaKey || e.ctrlKey || e.altKey) return;
      if (e.key.length !== 1 || !/[a-zA-Z0-9]/.test(e.key)) return;
      const target = e.target as HTMLElement | null;
      if (target) {
        const tag = target.tagName;
        if (
          tag === "INPUT" ||
          tag === "TEXTAREA" ||
          tag === "SELECT" ||
          target.isContentEditable
        )
          return;
      }
      e.preventDefault();
      openSearch(e.key);
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [openSearch]);

  return (
    <HashRouter>
      <div className="app">
        <Sidebar onOpenSearch={() => openSearch("")} />
        <main className="content">
          <Routes>
            <Route path="/*" element={<CurrentPage />} />
          </Routes>
        </main>
        <TableOfContents />
      </div>
      <SearchModal
        open={searchOpen}
        initialQuery={searchInitial}
        onClose={closeSearch}
      />
    </HashRouter>
  );
}
