export interface Page {
    slug: string
    title: string
}

export interface Section {
    title: string
    pages: Page[]
}

export const ungrouped: Page[] = [
    { slug: 'Home', title: 'Home' },
]

export const sections: Section[] = [
    {
        title: 'Getting Started',
        pages: [
            { slug: 'Getting Started', title: 'Getting Started' },
            { slug: 'Defining Models', title: 'Defining Models' },
        ],
    },
    {
        title: 'Querying',
        pages: [
            { slug: 'CRUD Operations', title: 'CRUD Operations' },
            { slug: 'Querying', title: 'Querying' },
            { slug: 'Expressions', title: 'Expressions' },
            { slug: 'Subqueries', title: 'Subqueries' },
            { slug: 'Joins', title: 'Joins' },
            { slug: 'Grouping and Aggregates', title: 'Grouping and Aggregates' },
            { slug: 'Bulk Operations', title: 'Bulk Operations' },
        ],
    },
    {
        title: 'Advanced',
        pages: [
            { slug: 'Transactions', title: 'Transactions' },
            { slug: 'Multi-threading', title: 'Multi-threading' },
            { slug: 'Raw SQL', title: 'Raw SQL' },
            { slug: 'Common Table Expressions', title: 'Common Table Expressions' },
        ],
    },
    {
        title: 'Data Types',
        pages: [
            { slug: 'Data Types', title: 'Data Types' },
            { slug: 'Storage Options', title: 'Storage Options' },
            { slug: 'Custom Converters', title: 'Custom Converters' },
        ],
    },
    {
        title: 'Extra Packages',
        pages: [
            { slug: 'JSON and JSONB', title: 'JSON and JSONB' },
            { slug: 'Window Functions', title: 'Window Functions' },
            { slug: 'Dependency Injection', title: 'Dependency Injection' },
        ],
    },
    {
        title: 'Deployment',
        pages: [
            { slug: 'Performance', title: 'Performance' },
            { slug: 'Native AOT', title: 'Native AOT' },
            { slug: 'Source Generator', title: 'Source Generator' },
            { slug: 'Migrating from sqlite-net-pcl', title: 'Migrating from sqlite-net-pcl' },
        ],
    },
]

export const pages: Page[] = [
    ...ungrouped,
    ...sections.flatMap(s => s.pages),
]
