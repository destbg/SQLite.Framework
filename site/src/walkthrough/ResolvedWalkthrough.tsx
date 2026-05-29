import { Navigate, useParams } from "react-router-dom";
import WalkthroughPage from "./WalkthroughPage";
import { walkthroughs } from "./walkthroughs";

export default function ResolvedWalkthrough() {
    const { slug = "" } = useParams();
    const w = walkthroughs[slug];
    if (!w) return <Navigate to="/" replace />;
    return <WalkthroughPage walkthrough={w} />;
}
