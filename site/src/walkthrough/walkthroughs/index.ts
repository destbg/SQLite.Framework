import type { Walkthrough } from "./types";
import { consoleWalkthrough } from "./console";
import { mauiWalkthrough } from "./maui";
import { avaloniaWalkthrough } from "./avalonia";
import { aspnetWalkthrough } from "./aspnet";
import { blazorWalkthrough } from "./blazor";

export const walkthroughList: Walkthrough[] = [
    consoleWalkthrough,
    mauiWalkthrough,
    avaloniaWalkthrough,
    aspnetWalkthrough,
    blazorWalkthrough,
];

export const walkthroughs: Record<string, Walkthrough> = Object.fromEntries(
    walkthroughList.map((w) => [w.slug, w]),
);
