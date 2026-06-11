export async function copyText(text: string): Promise<void> {
    try {
        await navigator.clipboard.writeText(text);
    } catch {
        const area = document.createElement("textarea");
        area.value = text;
        document.body.appendChild(area);
        area.select();
        document.execCommand("copy");
        area.remove();
    }
}

export function attachCopyButtons(root: ParentNode): void {
    for (const pre of root.querySelectorAll<HTMLPreElement>("pre[data-copy]")) {
        if (pre.querySelector(".copy-btn")) continue;
        const button = document.createElement("button");
        button.type = "button";
        button.className = "copy-btn";
        button.textContent = "Copy";
        button.addEventListener("click", async () => {
            const code = pre.querySelector("code");
            await copyText(code?.textContent ?? "");
            button.textContent = "Copied";
            button.classList.add("is-copied");
            window.setTimeout(() => {
                button.textContent = "Copy";
                button.classList.remove("is-copied");
            }, 1500);
        });
        pre.appendChild(button);
    }
}
