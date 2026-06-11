import { allPages, type DocPage } from "./pages";
import { markdownFor } from "./markdownFiles";
import { extractHeadings } from "./utils";

export interface SearchResult {
    page: DocPage;
    heading: string | null;
    snippet: string;
    score: number;
}

interface IndexEntry {
    page: DocPage;
    titleLower: string;
    headings: { text: string; lower: string }[];
    body: string;
    bodyLower: string;
}

let index: IndexEntry[] | null = null;

function buildIndex(): IndexEntry[] {
    const entries: IndexEntry[] = [];
    for (const page of allPages) {
        const markdown = markdownFor(page.fileName);
        if (markdown == null) continue;
        const body = markdown
            .replace(/```[\s\S]*?```/g, " ")
            .replace(/[#`*_>|]/g, "")
            .replace(/\[([^\]]*)\]\([^)]*\)/g, "$1")
            .replace(/\s+/g, " ");
        entries.push({
            page,
            titleLower: page.title.toLowerCase(),
            headings: extractHeadings(markdown).map((h) => ({
                text: h.text,
                lower: h.text.toLowerCase(),
            })),
            body,
            bodyLower: body.toLowerCase(),
        });
    }
    return entries;
}

function snippetAround(body: string, position: number): string {
    const start = Math.max(0, position - 50);
    const end = Math.min(body.length, position + 110);
    let snippet = body.slice(start, end).trim();
    if (start > 0) snippet = `...${snippet}`;
    if (end < body.length) snippet = `${snippet}...`;
    return snippet;
}

export function searchDocs(query: string): SearchResult[] {
    const tokens = query.toLowerCase().split(/\s+/).filter(Boolean);
    if (tokens.length === 0) return [];
    index ??= buildIndex();

    const results: SearchResult[] = [];
    for (const entry of index) {
        let score = 0;
        let heading: string | null = null;
        let firstBodyMatch = -1;
        let missed = false;

        for (const token of tokens) {
            let tokenScore = 0;
            if (entry.titleLower.includes(token)) tokenScore += 100;
            const headingMatch = entry.headings.find((h) => h.lower.includes(token));
            if (headingMatch) {
                tokenScore += 50;
                heading ??= headingMatch.text;
            }
            const bodyPos = entry.bodyLower.indexOf(token);
            if (bodyPos >= 0) {
                tokenScore += Math.max(0, 30 - bodyPos / 500);
                if (firstBodyMatch < 0 || bodyPos < firstBodyMatch) firstBodyMatch = bodyPos;
            }
            if (tokenScore === 0) {
                missed = true;
                break;
            }
            score += tokenScore;
        }

        if (missed) continue;
        results.push({
            page: entry.page,
            heading,
            snippet: snippetAround(entry.body, Math.max(0, firstBodyMatch)),
            score,
        });
    }

    return results.sort((a, b) => b.score - a.score).slice(0, 30);
}
