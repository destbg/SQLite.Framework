import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import WalkthroughIndex from "./WalkthroughIndex";
import ResolvedWalkthrough from "./ResolvedWalkthrough";

export default function App() {
    return (
        <BrowserRouter basename="/Walkthrough">
            <Routes>
                <Route path="/" element={<WalkthroughIndex />} />
                <Route path="/:slug" element={<ResolvedWalkthrough />} />
                <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
        </BrowserRouter>
    );
}
