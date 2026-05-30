import { useState } from "react";

interface Props {
    text: string;
}

export default function CopyButton({ text }: Props) {
    const [copied, setCopied] = useState(false);
    return (
        <button
            type="button"
            className={copied ? "copy-btn copy-btn--copied" : "copy-btn"}
            aria-label="Copy code"
            onClick={() => {
                navigator.clipboard
                    .writeText(text)
                    .then(() => {
                        setCopied(true);
                        window.setTimeout(() => setCopied(false), 1500);
                    })
                    .catch(() => undefined);
            }}
        >
            {copied ? "Copied" : "Copy"}
        </button>
    );
}
