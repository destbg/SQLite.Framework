export interface Page {
    slug: string
    title: string
}

export const pages: Page[] = [
    { slug: 'Home', title: 'Home' },
    { slug: 'Getting Started', title: 'Getting Started' },
    { slug: 'Defining Models', title: 'Defining Models' },
    { slug: 'CRUD Operations', title: 'CRUD Operations' },
    { slug: 'Querying', title: 'Querying' },
    { slug: 'Expressions', title: 'Expressions' },
    { slug: 'Subqueries', title: 'Subqueries' },
    { slug: 'Joins', title: 'Joins' },
    { slug: 'Grouping and Aggregates', title: 'Grouping and Aggregates' },
    { slug: 'Bulk Operations', title: 'Bulk Operations' },
    { slug: 'Transactions', title: 'Transactions' },
    { slug: 'Raw SQL', title: 'Raw SQL' },
    { slug: 'Data Types', title: 'Data Types' },
    { slug: 'Performance', title: 'Performance' },
]
