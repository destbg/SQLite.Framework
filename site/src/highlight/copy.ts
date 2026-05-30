export function addCopyButtons(root: ParentNode = document): void {
    root.querySelectorAll<HTMLElement>("pre").forEach((pre) => {
        const code = pre.querySelector("code");
        if (!code || pre.querySelector(".copy-btn")) return;
        const button = document.createElement("button");
        button.type = "button";
        button.className = "copy-btn";
        button.textContent = "Copy";
        button.setAttribute("aria-label", "Copy code");
        button.addEventListener("click", () => {
            navigator.clipboard
                .writeText(code.textContent ?? "")
                .then(() => {
                    button.textContent = "Copied";
                    button.classList.add("copy-btn--copied");
                    window.setTimeout(() => {
                        button.textContent = "Copy";
                        button.classList.remove("copy-btn--copied");
                    }, 1500);
                })
                .catch(() => undefined);
        });
        pre.appendChild(button);
    });
}
