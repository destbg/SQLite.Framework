import { Navigate, Route, Routes, useParams } from "react-router-dom";
import "../shared/tokens.css";
import "../highlight/syntax.css";
import "./walkthrough.css";
import { findWalkthrough } from "./walkthroughs";
import { WalkthroughIndex } from "./WalkthroughIndex";
import { WalkthroughPlayer } from "./WalkthroughPlayer";

export function App() {
    return (
        <Routes>
            <Route path="/" element={<WalkthroughIndex />} />
            <Route path="/:slug" element={<ResolvedWalkthrough />} />
            <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
    );
}

function ResolvedWalkthrough() {
    const { slug } = useParams();
    const walkthrough = findWalkthrough(slug);
    if (!walkthrough) return <Navigate to="/" replace />;
    return <WalkthroughPlayer key={walkthrough.slug} walkthrough={walkthrough} />;
}
