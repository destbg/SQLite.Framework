export type OpenInIconKind = "copy" | "file" | "chatgpt" | "claude" | "t3" | "github";

const ICON_SIZE = 18;

interface OpenInIconProps {
    kind: OpenInIconKind;
}

export function OpenInIcon({ kind }: OpenInIconProps) {
    switch (kind) {
        case "copy":
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width={ICON_SIZE} height={ICON_SIZE} aria-hidden="true">
                    <path
                        d="M5 2h7a1 1 0 0 1 1 1v9h-1V3H5V2z M3 4h7a1 1 0 0 1 1 1v9a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1zm0 1v9h7V5H3z"
                        fill="currentColor"
                    />
                </svg>
            );
        case "file":
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width={ICON_SIZE} height={ICON_SIZE} aria-hidden="true">
                    <path
                        d="M4 1.5A1.5 1.5 0 0 1 5.5 0h5L14 3.5v11a1.5 1.5 0 0 1-1.5 1.5h-7A1.5 1.5 0 0 1 4 14.5v-13zM5.5 1a.5.5 0 0 0-.5.5v13a.5.5 0 0 0 .5.5h7a.5.5 0 0 0 .5-.5V4h-3V1H5.5zM11 1.75V3h2.25L11 1.75z"
                        fill="currentColor"
                    />
                </svg>
            );
        case "chatgpt":
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width={ICON_SIZE} height={ICON_SIZE} aria-hidden="true">
                    <path
                        d="M8 1.5c-3.59 0-6.5 2.91-6.5 6.5 0 1.17.31 2.27.86 3.22L1.5 14.5l3.33-.88a6.5 6.5 0 1 0 3.17-12.12zm0 1.5a5 5 0 1 1-2.54 9.3l-.22-.13-1.97.52.53-1.93-.14-.23A5 5 0 0 1 8 3z"
                        fill="currentColor"
                    />
                </svg>
            );
        case "claude":
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width={ICON_SIZE} height={ICON_SIZE} aria-hidden="true">
                    <path
                        d="M8 1l1.7 4.8L14.5 8l-4.8 1.7L8 14.5l-1.7-4.8L1.5 8l4.8-1.7L8 1z"
                        fill="currentColor"
                    />
                </svg>
            );
        case "t3":
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width={ICON_SIZE} height={ICON_SIZE} aria-hidden="true">
                    <path d="M2 3h12v2H9v9H7V5H2V3z" fill="currentColor" />
                </svg>
            );
        case "github":
            return (
                <svg className="open-in-icon" viewBox="0 0 16 16" width={ICON_SIZE} height={ICON_SIZE} aria-hidden="true">
                    <path
                        d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2 .37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82a7.4 7.4 0 0 1 2-.27c.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8z"
                        fill="currentColor"
                    />
                </svg>
            );
    }
}
