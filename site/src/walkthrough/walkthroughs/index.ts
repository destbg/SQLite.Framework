import type { Walkthrough } from "./types";
import { consoleWalkthrough } from "./console";
import { mauiWalkthrough } from "./maui";
import { avaloniaWalkthrough } from "./avalonia";
import { aspnetWalkthrough } from "./aspnet";
import { blazorWalkthrough } from "./blazor";

export const walkthroughs: Walkthrough[] = [
    consoleWalkthrough,
    mauiWalkthrough,
    avaloniaWalkthrough,
    aspnetWalkthrough,
    blazorWalkthrough,
];

export function findWalkthrough(slug: string | undefined): Walkthrough | null {
    if (!slug) return null;
    const lower = slug.toLowerCase();
    return walkthroughs.find((w) => w.slug === lower) ?? null;
}
