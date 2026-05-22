import { urlSlug } from "./utils";

export interface Page {
  slug: string;
  title: string;
}

export interface Section {
  title: string;
  pages: Page[];
}

function makePage(title: string, slug?: string): Page {
  return { slug: slug ?? urlSlug(title), title };
}

export const ungrouped: Page[] = [{ slug: "Home", title: "Home" }];

export const sections: Section[] = [
  {
    title: "Getting Started",
    pages: [
      makePage("Overview"),
      makePage("Getting Started"),
      makePage("Defining Models"),
      makePage("Samples"),
    ],
  },
  {
    title: "Querying",
    pages: [
      makePage("CRUD Operations"),
      makePage("Querying"),
      makePage("Expressions"),
      makePage("Subqueries"),
      makePage("Joins"),
      makePage("Grouping and Aggregates"),
      makePage("Bulk Operations"),
    ],
  },
  {
    title: "Advanced",
    pages: [
      makePage("Transactions"),
      makePage("Multi-threading"),
      makePage("Raw SQL"),
      makePage("Common Table Expressions"),
      makePage("Full Text Search"),
      makePage("R-Tree"),
      makePage("JSON and JSONB"),
      makePage("Window Functions"),
      makePage("SQLite Functions"),
      makePage("Pragmas"),
      makePage("Backup"),
      makePage("Attached Databases"),
      makePage("Schema"),
    ],
  },
  {
    title: "Data Types",
    pages: [
      makePage("Data Types"),
      makePage("Storage Options"),
      makePage("Custom Converters"),
    ],
  },
  {
    title: "Extra Packages",
    pages: [makePage("Dependency Injection")],
  },
  {
    title: "Deployment",
    pages: [
      makePage("Performance"),
      makePage("Logging"),
      makePage("Native AOT"),
      makePage("Source Generator"),
    ],
  },
  {
    title: "Migration Guides",
    pages: [
      makePage("Migrating from sqlite-net-pcl"),
      makePage("Migrating from EF Core"),
    ],
  },
  {
    title: "Tooling",
    pages: [makePage("AI Assistance")],
  },
];

export const pages: Page[] = [
  ...ungrouped,
  ...sections.flatMap((s) => s.pages),
];

export function findPageBySlug(slug: string): Page | undefined {
  return pages.find((p) => p.slug === slug);
}

export function findPageByLink(href: string): Page | undefined {
  const decoded = decodeURIComponent(href).trim();
  if (!decoded) return undefined;
  const lower = decoded.toLowerCase();
  return (
    pages.find((p) => p.title === decoded) ??
    pages.find((p) => p.slug === decoded) ??
    pages.find((p) => p.title.toLowerCase() === lower) ??
    pages.find((p) => p.slug.toLowerCase() === lower)
  );
}
