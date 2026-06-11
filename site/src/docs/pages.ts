export interface DocPage {
    title: string;
    slug: string;
    fileName: string;
}

export interface DocGroup {
    title: string | null;
    pages: DocPage[];
}

function page(title: string): DocPage {
    return { title, slug: title.replaceAll(" ", "-"), fileName: title };
}

export const docGroups: DocGroup[] = [
    {
        title: null,
        pages: [{ title: "Home", slug: "", fileName: "Home" }],
    },
    {
        title: "Getting Started",
        pages: [page("Overview"), page("Getting Started"), page("Defining Models"), page("Samples")],
    },
    {
        title: "Querying",
        pages: [
            page("CRUD Operations"),
            page("Querying"),
            page("Expressions"),
            page("Subqueries"),
            page("Joins"),
            page("Grouping and Aggregates"),
            page("Bulk Operations"),
        ],
    },
    {
        title: "Advanced",
        pages: [
            page("Transactions"),
            page("Multi-threading"),
            page("Raw SQL"),
            page("Common Table Expressions"),
            page("Full Text Search"),
            page("R-Tree"),
            page("JSON and JSONB"),
            page("Window Functions"),
            page("SQLite Functions"),
            page("Pragmas"),
            page("Backup"),
            page("Attached Databases"),
            page("Schema"),
            page("Limitations"),
        ],
    },
    {
        title: "Data Types",
        pages: [page("Data Types"), page("Storage Options"), page("Custom Converters")],
    },
    {
        title: "Extra Packages",
        pages: [page("Dependency Injection")],
    },
    {
        title: "Deployment",
        pages: [page("Performance"), page("Logging"), page("Native AOT"), page("Source Generator")],
    },
    {
        title: "Migration Guides",
        pages: [page("Migrating from sqlite-net-pcl"), page("Migrating from EF Core")],
    },
    {
        title: "Tooling",
        pages: [page("AI Assistance")],
    },
];

export const allPages: DocPage[] = docGroups.flatMap((group) => group.pages);

export function findPage(rawSlug: string | undefined): DocPage | null {
    const slug = decodeURIComponent(rawSlug ?? "").replaceAll(" ", "-").toLowerCase();
    if (slug === "") return allPages[0];
    return (
        allPages.find(
            (p) => p.slug.toLowerCase() === slug || p.title.toLowerCase() === slug,
        ) ?? null
    );
}
