import { highlightInto } from "../highlight/highlighter";

interface QueryTab {
    linq: string;
    sql: string;
}

const tabs: Record<string, QueryTab> = {
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
    const buttons = Array.from(
        document.querySelectorAll<HTMLButtonElement>(".translate-tabs [data-tab]"),
    );
    const linqEl = document.getElementById("tab-linq");
    const sqlEl = document.getElementById("tab-sql");
    const sqlPanel = document.querySelector<HTMLElement>(".translate-panel-sql");
    if (!linqEl || !sqlEl || buttons.length === 0) return;

    const apply = (key: string) => {
        const tab = tabs[key];
        if (!tab) return;
        highlightInto(linqEl, tab.linq, "csharp");
        highlightInto(sqlEl, tab.sql, "sql");
        if (sqlPanel) {
            sqlPanel.classList.remove("is-revealing");
            void sqlPanel.offsetWidth;
            sqlPanel.classList.add("is-revealing");
        }
    };

    for (const button of buttons) {
        button.addEventListener("click", () => {
            for (const other of buttons) {
                other.classList.toggle("is-active", other === button);
                other.setAttribute("aria-selected", other === button ? "true" : "false");
            }
            apply(button.dataset.tab ?? "");
        });
    }

    const observer = new IntersectionObserver(
        (entries) => {
            for (const entry of entries) {
                if (entry.isIntersecting && sqlPanel) {
                    sqlPanel.classList.add("is-revealing");
                    observer.disconnect();
                }
            }
        },
        { threshold: 0.4 },
    );
    if (sqlPanel) observer.observe(sqlPanel);
}
