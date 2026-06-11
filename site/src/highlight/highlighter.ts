interface Token {
    value: string;
    cls: string | null;
}

interface Sig {
    value: string;
    cls: string | null;
}

const ESCAPES: Record<string, string> = {
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
};

function escapeHtml(text: string): string {
    return text.replace(/[&<>"]/g, (c) => ESCAPES[c]);
}

function render(tokens: Token[]): string {
    let html = "";
    for (const t of tokens) {
        const value = escapeHtml(t.value);
        html += t.cls ? `<span class="${t.cls}">${value}</span>` : value;
    }
    return html;
}

const CS_LITERALS = new Set(["true", "false", "null"]);

const CS_TYPES = new Set([
    "bool", "byte", "sbyte", "char", "decimal", "double", "float",
    "int", "uint", "long", "ulong", "short", "ushort", "nint", "nuint",
    "object", "string", "void",
]);

const CS_KEYWORDS = new Set([
    "abstract", "as", "async", "await", "base", "break", "case", "catch",
    "checked", "class", "const", "continue", "default", "delegate", "do",
    "else", "enum", "event", "explicit", "extern", "finally", "fixed", "for",
    "foreach", "goto", "if", "implicit", "in", "interface", "internal", "is",
    "lock", "namespace", "new", "operator", "out", "override", "params",
    "private", "protected", "public", "readonly", "ref", "return", "sealed",
    "sizeof", "stackalloc", "static", "struct", "switch", "this", "throw",
    "try", "typeof", "unchecked", "unsafe", "using", "virtual", "volatile",
    "while", "var", "dynamic", "nameof", "when", "where", "select", "from",
    "join", "on", "equals", "group", "by", "into", "orderby", "ascending",
    "descending", "let", "yield", "get", "set", "add", "remove", "value",
    "init", "record", "required", "with", "and", "or", "not", "partial",
    "global", "file", "scoped",
]);

function isIdentStart(c: string): boolean {
    return /[A-Za-z_]/.test(c);
}

function isIdentChar(c: string): boolean {
    return /[A-Za-z0-9_]/.test(c);
}

function classifyIdent(
    id: string,
    prev: Sig | null,
    nextSig: string,
    nextIsIdent: boolean,
): string | null {
    const prevCh = prev ? prev.value[prev.value.length - 1] : "";
    const prevKeyword = prev && prev.cls === "tok-keyword" ? prev.value : null;
    const prevIsType = !!(prev && prev.cls === "tok-type");
    const isUpper = id[0] >= "A" && id[0] <= "Z";

    if (prevCh === ".") return nextSig === "(" ? "tok-method" : null;
    if (prevKeyword === "new") return isUpper ? "tok-type" : null;
    if (prevCh === "[") return isUpper ? "tok-type" : null;
    if (prevKeyword === "using") return nextIsIdent ? "tok-type" : null;
    if (prevCh === "<") return isUpper ? "tok-type" : null;
    if (nextSig === "(") return "tok-method";
    if (prevIsType) return null;
    if (isUpper && nextSig !== "=") return "tok-type";
    return null;
}

function tokenizeCSharp(code: string): Token[] {
    const tokens: Token[] = [];
    let prev: Sig | null = null;
    const push = (value: string, cls: string | null, significant: boolean) => {
        tokens.push({ value, cls });
        if (significant) prev = { value, cls };
    };

    const n = code.length;
    let i = 0;
    while (i < n) {
        const c = code[i];

        if (/\s/.test(c)) {
            let j = i + 1;
            while (j < n && /\s/.test(code[j])) j++;
            push(code.slice(i, j), null, false);
            i = j;
            continue;
        }

        if (c === "/" && code[i + 1] === "/") {
            let j = i + 2;
            while (j < n && code[j] !== "\n") j++;
            push(code.slice(i, j), "tok-comment", false);
            i = j;
            continue;
        }

        if (c === "/" && code[i + 1] === "*") {
            let j = i + 2;
            while (j < n && !(code[j] === "*" && code[j + 1] === "/")) j++;
            j = Math.min(n, j + 2);
            push(code.slice(i, j), "tok-comment", false);
            i = j;
            continue;
        }

        if (c === '"' || c === "'" || ((c === "@" || c === "$") && (code[i + 1] === '"' || code[i + 1] === "@" || code[i + 1] === "$"))) {
            let j = i;
            let verbatim = false;
            while (code[j] === "@" || code[j] === "$") {
                if (code[j] === "@") verbatim = true;
                j++;
            }
            const quote = code[j];
            j++;
            while (j < n) {
                if (!verbatim && code[j] === "\\") {
                    j += 2;
                    continue;
                }
                if (code[j] === quote) {
                    if (verbatim && code[j + 1] === quote) {
                        j += 2;
                        continue;
                    }
                    j++;
                    break;
                }
                j++;
            }
            push(code.slice(i, j), "tok-string", true);
            i = j;
            continue;
        }

        if (/[0-9]/.test(c) || (c === "." && /[0-9]/.test(code[i + 1] ?? ""))) {
            let j = i + 1;
            while (j < n && /[0-9a-fA-FxXeE._]/.test(code[j])) j++;
            while (j < n && /[uUlLmMfFdD]/.test(code[j])) j++;
            push(code.slice(i, j), "tok-number", true);
            i = j;
            continue;
        }

        if (isIdentStart(c)) {
            let j = i + 1;
            while (j < n && isIdentChar(code[j])) j++;
            const id = code.slice(i, j);

            let cls: string | null;
            if (CS_LITERALS.has(id)) cls = "tok-literal";
            else if (CS_TYPES.has(id)) cls = "tok-type";
            else if (CS_KEYWORDS.has(id)) cls = "tok-keyword";
            else {
                let k = j;
                if (code[k] === "<") {
                    let depth = 0;
                    while (k < n) {
                        if (code[k] === "<") depth++;
                        else if (code[k] === ">") {
                            depth--;
                            if (depth === 0) {
                                k++;
                                break;
                            }
                        } else if (!/[\s,A-Za-z0-9_.<>?\[\]]/.test(code[k])) break;
                        k++;
                    }
                }
                while (k < n && /\s/.test(code[k])) k++;
                const nextSig = k < n ? code[k] : "";
                const nextIsIdent = nextSig !== "" && isIdentStart(nextSig);
                cls = classifyIdent(id, prev, nextSig, nextIsIdent);
            }
            push(id, cls, true);
            i = j;
            continue;
        }

        push(c, null, true);
        i++;
    }

    return tokens;
}

const SQL_KEYWORDS = new Set([
    "select", "from", "where", "and", "or", "not", "as", "join", "inner",
    "left", "right", "outer", "full", "cross", "on", "group", "by", "order",
    "having", "limit", "offset", "distinct", "union", "all", "insert", "into",
    "values", "update", "set", "delete", "create", "table", "primary", "key",
    "foreign", "references", "index", "view", "asc", "desc", "case", "when",
    "then", "else", "end", "in", "is", "null", "like", "between", "exists",
    "with", "default", "constraint", "unique", "autoincrement", "integer",
    "text", "real", "blob", "begin", "commit", "rollback", "transaction",
]);

const SQL_FUNCTIONS = new Set([
    "count", "sum", "avg", "min", "max", "coalesce", "abs", "round", "length",
    "lower", "upper", "substr", "replace", "json", "json_extract", "group_concat",
]);

function tokenizeSql(code: string): Token[] {
    const tokens: Token[] = [];
    const n = code.length;
    let i = 0;
    while (i < n) {
        const c = code[i];
        if (/\s/.test(c)) {
            let j = i + 1;
            while (j < n && /\s/.test(code[j])) j++;
            tokens.push({ value: code.slice(i, j), cls: null });
            i = j;
            continue;
        }
        if (c === "-" && code[i + 1] === "-") {
            let j = i + 2;
            while (j < n && code[j] !== "\n") j++;
            tokens.push({ value: code.slice(i, j), cls: "tok-comment" });
            i = j;
            continue;
        }
        if (c === "/" && code[i + 1] === "*") {
            let j = i + 2;
            while (j < n && !(code[j] === "*" && code[j + 1] === "/")) j++;
            j = Math.min(n, j + 2);
            tokens.push({ value: code.slice(i, j), cls: "tok-comment" });
            i = j;
            continue;
        }
        if (c === "'" || c === '"') {
            let j = i + 1;
            while (j < n) {
                if (code[j] === c) {
                    if (code[j + 1] === c) {
                        j += 2;
                        continue;
                    }
                    j++;
                    break;
                }
                j++;
            }
            tokens.push({ value: code.slice(i, j), cls: "tok-string" });
            i = j;
            continue;
        }
        if (/[0-9]/.test(c)) {
            let j = i + 1;
            while (j < n && /[0-9.]/.test(code[j])) j++;
            tokens.push({ value: code.slice(i, j), cls: "tok-number" });
            i = j;
            continue;
        }
        if (c === "@" || c === ":" || c === "$") {
            let j = i + 1;
            while (j < n && isIdentChar(code[j])) j++;
            if (j > i + 1) {
                tokens.push({ value: code.slice(i, j), cls: "tok-param" });
                i = j;
                continue;
            }
        }
        if (c === "?") {
            let j = i + 1;
            while (j < n && /[0-9]/.test(code[j])) j++;
            tokens.push({ value: code.slice(i, j), cls: "tok-param" });
            i = j;
            continue;
        }
        if (isIdentStart(c)) {
            let j = i + 1;
            while (j < n && isIdentChar(code[j])) j++;
            const word = code.slice(i, j);
            const lower = word.toLowerCase();
            let cls: string | null = null;
            if (SQL_KEYWORDS.has(lower)) cls = "tok-keyword";
            else if (SQL_FUNCTIONS.has(lower)) cls = "tok-method";
            tokens.push({ value: word, cls });
            i = j;
            continue;
        }
        tokens.push({ value: c, cls: null });
        i++;
    }
    return tokens;
}

function tokenizeXml(code: string): Token[] {
    const tokens: Token[] = [];
    const n = code.length;
    let i = 0;
    let inTag = false;
    while (i < n) {
        if (!inTag) {
            if (code.startsWith("<!--", i)) {
                let j = i + 4;
                while (j < n && !code.startsWith("-->", j)) j++;
                j = Math.min(n, j + 3);
                tokens.push({ value: code.slice(i, j), cls: "tok-comment" });
                i = j;
                continue;
            }
            if (code[i] === "<") {
                let j = i + 1;
                if (code[j] === "/" || code[j] === "?" || code[j] === "!") j++;
                tokens.push({ value: code.slice(i, j), cls: null });
                let k = j;
                while (k < n && /[A-Za-z0-9_:.-]/.test(code[k])) k++;
                if (k > j) tokens.push({ value: code.slice(j, k), cls: "tok-keyword" });
                i = k;
                inTag = true;
                continue;
            }
            let j = i;
            while (j < n && code[j] !== "<") j++;
            tokens.push({ value: code.slice(i, j), cls: null });
            i = j;
            continue;
        }

        const c = code[i];
        if (/\s/.test(c)) {
            let j = i + 1;
            while (j < n && /\s/.test(code[j])) j++;
            tokens.push({ value: code.slice(i, j), cls: null });
            i = j;
            continue;
        }
        if (c === ">" || (c === "/" && code[i + 1] === ">") || (c === "?" && code[i + 1] === ">")) {
            const len = c === ">" ? 1 : 2;
            tokens.push({ value: code.slice(i, i + len), cls: null });
            i += len;
            inTag = false;
            continue;
        }
        if (c === '"' || c === "'") {
            let j = i + 1;
            while (j < n && code[j] !== c) j++;
            j = Math.min(n, j + 1);
            tokens.push({ value: code.slice(i, j), cls: "tok-string" });
            i = j;
            continue;
        }
        if (/[A-Za-z_]/.test(c)) {
            let j = i + 1;
            while (j < n && /[A-Za-z0-9_:.-]/.test(code[j])) j++;
            tokens.push({ value: code.slice(i, j), cls: "tok-type" });
            i = j;
            continue;
        }
        tokens.push({ value: c, cls: null });
        i++;
    }
    return tokens;
}

function tokenizeBash(code: string): Token[] {
    const tokens: Token[] = [];
    const n = code.length;
    let i = 0;
    let atCommand = true;
    while (i < n) {
        const c = code[i];
        if (c === "#") {
            let j = i + 1;
            while (j < n && code[j] !== "\n") j++;
            tokens.push({ value: code.slice(i, j), cls: "tok-comment" });
            i = j;
            continue;
        }
        if (c === '"' || c === "'") {
            let j = i + 1;
            while (j < n && code[j] !== c) j++;
            j = Math.min(n, j + 1);
            tokens.push({ value: code.slice(i, j), cls: "tok-string" });
            i = j;
            atCommand = false;
            continue;
        }
        if (c === "\n" || c === "|" || c === "&" || c === ";") {
            tokens.push({ value: c, cls: null });
            atCommand = true;
            i++;
            continue;
        }
        if (/\s/.test(c)) {
            let j = i + 1;
            while (j < n && /\s/.test(code[j]) && code[j] !== "\n") j++;
            tokens.push({ value: code.slice(i, j), cls: null });
            i = j;
            continue;
        }
        if (/[A-Za-z0-9_./-]/.test(c)) {
            let j = i + 1;
            while (j < n && /[A-Za-z0-9_./-]/.test(code[j])) j++;
            const word = code.slice(i, j);
            const isFlag = word.startsWith("-");
            tokens.push({ value: word, cls: atCommand && !isFlag ? "tok-method" : null });
            if (!isFlag) atCommand = false;
            i = j;
            continue;
        }
        tokens.push({ value: c, cls: null });
        i++;
    }
    return tokens;
}

export function highlight(code: string, lang: string | undefined): string {
    switch (lang?.toLowerCase()) {
        case "csharp":
        case "cs":
            return render(tokenizeCSharp(code));
        case "sql":
            return render(tokenizeSql(code));
        case "xml":
        case "html":
            return render(tokenizeXml(code));
        case "bash":
        case "sh":
        case "shell":
            return render(tokenizeBash(code));
        default:
            return escapeHtml(code);
    }
}

export function highlightInto(el: HTMLElement, code: string, lang: string | undefined): void {
    el.innerHTML = highlight(code, lang);
}
