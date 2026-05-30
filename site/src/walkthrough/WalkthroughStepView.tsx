import { useEffect, useRef } from "react";
import { highlight } from "../highlight/highlighter";
import CopyButton from "../highlight/CopyButton";
import type { WalkthroughStep } from "./walkthroughs/types";

interface Props {
    step: WalkthroughStep;
    index: number;
    total: number;
    phase: "in" | "out";
    direction: 1 | -1;
}

export default function WalkthroughStepView({ step, index, total, phase, direction }: Props) {
    const codeRef = useRef<HTMLElement | null>(null);

    useEffect(() => {
        if (!codeRef.current || !step.code) return;
        codeRef.current.innerHTML = highlight(step.code.text, step.code.language);
    }, [step]);

    const stageClass = `wt-step wt-step--${phase} wt-step--dir-${direction > 0 ? "forward" : "back"}`;
    const layoutClass = step.code ? "wt-step-grid wt-step-grid--with-code" : "wt-step-grid";

    return (
        <article className={stageClass} aria-current={phase === "in"}>
            <div className={layoutClass}>
                <div className="wt-step-text">
                    <span className="wt-step-eyebrow">
                        Step {index + 1} of {total}
                    </span>
                    <h1 className="wt-step-title">{step.title}</h1>
                    <p className="wt-step-desc">{step.description}</p>
                </div>
                {step.code && (
                    <div className="wt-code-window">
                        <CopyButton text={step.code.text} />
                        <div className="wt-code-header">
                            <span className="wt-code-dot wt-code-dot--red" />
                            <span className="wt-code-dot wt-code-dot--yellow" />
                            <span className="wt-code-dot wt-code-dot--green" />
                            {step.code.filename && (
                                <span className="wt-code-filename">{step.code.filename}</span>
                            )}
                        </div>
                        <pre className="wt-code-block"><code ref={codeRef} className={`language-${step.code.language}`} /></pre>
                    </div>
                )}
            </div>
        </article>
    );
}
