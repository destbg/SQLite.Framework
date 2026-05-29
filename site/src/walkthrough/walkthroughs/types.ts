export interface WalkthroughStep {
    title: string;
    description: string;
    code?: {
        language: string;
        filename?: string;
        text: string;
    };
}

export interface Walkthrough {
    slug: string;
    title: string;
    subtitle: string;
    steps: WalkthroughStep[];
}
