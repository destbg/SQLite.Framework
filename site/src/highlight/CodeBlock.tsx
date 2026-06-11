import { useState } from "react";
import { highlight } from "./highlighter";
import { copyText } from "./copy";

interface CodeBlockProps {
    code: string;
    language?: string;
    filename?: string;
}

export function CodeBlock({ code, language, filename }: CodeBlockProps) {
    const [copied, setCopied] = useState(false);

    const onCopy = async () => {
        await copyText(code);
        setCopied(true);
        window.setTimeout(() => setCopied(false), 1500);
    };

    return (
        <div className="code-block">
            {filename && (
                <div className="code-block-head">
                    <span className="code-block-file">{filename}</span>
                </div>
            )}
            <pre>
                <code dangerouslySetInnerHTML={{ __html: highlight(code, language) }} />
                <button
                    type="button"
                    className={copied ? "copy-btn is-copied" : "copy-btn"}
                    onClick={onCopy}
                >
                    {copied ? "Copied" : "Copy"}
                </button>
            </pre>
        </div>
    );
}
