import { highlight } from "../highlight/highlighter";

interface Example {
    linq: string;
    sql: string;
}

const EXAMPLES: Record<string, Example> = {
    group: {
        linq: `var topAuthors = await (
    from b in db.Table<Book>()
    join a in db.Table<Author>()
        on b.AuthorId equals a.Id
    where b.Price < 30
    group b by a.Name into g
    orderby g.Sum(b => b.Sales) descending
    select new
    {
        Author = g.Key,
        Titles = g.Count(),
        Revenue = g.Sum(b => b.Sales),
    }
).Take(5).ToListAsync();`,
        sql: `SELECT a0."AuthorName" AS "Author",
       COUNT(*) AS "Titles",
       SUM(b0."BookSales") AS "Revenue"
FROM "Books" AS b0
INNER JOIN "Authors" AS a0
    ON b0."BookAuthorId" = a0."AuthorId"
WHERE b0."BookPrice" < @p0
GROUP BY a0."AuthorName"
ORDER BY SUM(b0."BookSales") DESC
LIMIT @p1;`,
    },
    join: {
        linq: `var results = await (
    from book in db.Table<Book>()
    join author in db.Table<Author>()
        on book.AuthorId equals author.Id
    select new { book.Title, author.Name, book.Price }
).ToListAsync();`,
        sql: `SELECT b0."BookTitle" AS "Title",
       a0."AuthorName" AS "Name",
       b0."BookPrice" AS "Price"
FROM "Books" AS b0
INNER JOIN "Authors" AS a0
    ON b0."BookAuthorId" = a0."AuthorId";`,
    },
    subquery: {
        linq: `var authorIds = db.Table<Book>()
    .Where(b => b.Price < 10)
    .Select(b => b.AuthorId);

var books = await db.Table<Book>()
    .Where(b => authorIds.Contains(b.AuthorId))
    .ToListAsync();`,
        sql: `SELECT b0."BookId" AS "Id",
       b0."BookTitle" AS "Title",
       b0."BookAuthorId" AS "AuthorId",
       b0."BookPrice" AS "Price"
FROM "Books" AS b0
WHERE b0."BookAuthorId" IN (
    SELECT b1."BookAuthorId" AS "AuthorId"
    FROM "Books" AS b1
    WHERE b1."BookPrice" < @p0
);`,
    },
    cte: {
        linq: `SQLiteCte<Book> expensive = db.With(() =>
    db.Table<Book>().Where(b => b.Price > 30));

var results = (from b in expensive select b).ToList();`,
        sql: `WITH cte0 AS (
    SELECT b1."BookId" AS "Id",
       b1."BookTitle" AS "Title",
       b1."BookAuthorId" AS "AuthorId",
       b1."BookPrice" AS "Price"
    FROM "Books" AS b1
    WHERE b1."BookPrice" > @p0
)
SELECT b0."Id" AS "Id",
       b0."Title" AS "Title",
       b0."AuthorId" AS "AuthorId",
       b0."Price" AS "Price"
FROM cte0 AS b0;`,
    },
};

export function initQueryTabs(): void {
    const linqCode = document.getElementById("linq-code");
    const sqlCode = document.getElementById("sql-code");
    const sqlPanel = document.getElementById("sql-panel");
    const tabs = document.querySelectorAll<HTMLButtonElement>(".tab-btn");
    if (!linqCode || !sqlCode || tabs.length === 0) return;

    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    tabs.forEach((tab) => {
        tab.addEventListener("click", () => {
            const key = tab.dataset.example;
            const example = key ? EXAMPLES[key] : undefined;
            if (!example) return;

            tabs.forEach((t) => t.classList.toggle("tab-btn--active", t === tab));
            linqCode.innerHTML = highlight(example.linq, "csharp");
            sqlCode.innerHTML = highlight(example.sql, "sql");

            if (sqlPanel && !reduceMotion) {
                const block = sqlPanel.querySelector<HTMLElement>(".code-block");
                sqlPanel.classList.remove("sql-panel-revealed");
                if (block) {
                    void block.offsetWidth;
                    sqlPanel.style.setProperty("--sql-height", `${block.offsetHeight}px`);
                }
                requestAnimationFrame(() => sqlPanel.classList.add("sql-panel-revealed"));
            }
        });
    });
}
