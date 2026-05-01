import { pages } from "./pages";
import { loadContent } from "./markdownFiles";

export interface SearchHit {
  slug: string;
  title: string;
  matchedHeading?: string;
  snippet: string;
  score: number;
}

const MAX_RESULTS = 30;
const SNIPPET_PAD = 60;

interface PageIndex {
  slug: string;
  title: string;
  titleLower: string;
  content: string;
  contentLower: string;
  headings: string[];
}

let cachedIndex: PageIndex[] | null = null;

function buildIndex(): PageIndex[] {
  if (cachedIndex) return cachedIndex;
  cachedIndex = pages.map((page) => {
    const content = loadContent(page.slug);
    const headings: string[] = [];
    for (const line of content.split("\n")) {
      const m = line.match(/^#{1,6}\s+(.+?)\s*$/);
      if (m) headings.push(m[1]);
    }
    return {
      slug: page.slug,
      title: page.title,
      titleLower: page.title.toLowerCase(),
      content,
      contentLower: content.toLowerCase(),
      headings,
    };
  });
  return cachedIndex;
}

function buildSnippet(content: string, idx: number, queryLen: number): string {
  const start = Math.max(0, idx - SNIPPET_PAD);
  const end = Math.min(content.length, idx + queryLen + SNIPPET_PAD * 2);
  let slice = content.slice(start, end);
  slice = slice
    .replace(/```[\s\S]*?```/g, " ")
    .replace(/[*_`#>]/g, "")
    .replace(/\s+/g, " ")
    .trim();
  return (start > 0 ? "… " : "") + slice + (end < content.length ? " …" : "");
}

export function search(rawQuery: string): SearchHit[] {
  const query = rawQuery.trim().toLowerCase();
  if (!query) return [];

  const tokens = query.split(/\s+/).filter(Boolean);
  if (tokens.length === 0) return [];

  const index = buildIndex();
  const hits: SearchHit[] = [];

  for (const page of index) {
    let allFound = true;
    for (const token of tokens) {
      if (
        !page.contentLower.includes(token) &&
        !page.titleLower.includes(token)
      ) {
        allFound = false;
        break;
      }
    }
    if (!allFound) continue;

    let score = 0;
    for (const token of tokens) {
      if (page.titleLower.includes(token)) score += 100;
    }

    let matchedHeading: string | undefined;
    for (const heading of page.headings) {
      const headingLower = heading.toLowerCase();
      let allInHeading = true;
      for (const token of tokens) {
        if (!headingLower.includes(token)) {
          allInHeading = false;
          break;
        }
      }
      if (allInHeading) {
        score += 50;
        if (!matchedHeading) matchedHeading = heading;
      }
    }

    const firstIdx = page.contentLower.indexOf(tokens[0]);
    if (firstIdx >= 0) score += Math.max(1, 30 - Math.floor(firstIdx / 200));

    let snippet: string;
    if (firstIdx >= 0) {
      snippet = buildSnippet(page.content, firstIdx, tokens[0].length);
    } else if (matchedHeading) {
      snippet = matchedHeading;
    } else {
      snippet = page.title;
    }

    hits.push({
      slug: page.slug,
      title: page.title,
      matchedHeading,
      snippet,
      score,
    });
  }

  return hits.sort((a, b) => b.score - a.score).slice(0, MAX_RESULTS);
}
