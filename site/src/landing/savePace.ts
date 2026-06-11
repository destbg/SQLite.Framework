interface Lane {
    x: number;
    meanUs: number;
    stdUs: number;
    minUs: number;
    maxUs: number;
    spikeChance: number;
    labelEvery: number;
    color: () => string;
    dots: Dot[];
    nextSpawnAt: number;
    spawned: number;
}

interface Dot {
    xFrac: number;
    y: number;
    label: string | null;
}

const FALL_SPEED = 170;
const MS_PER_US = 0.5;
const PHONE_FACTOR = 100;
const LANE_HALF_SPREAD = 0.11;

function spreadFrac(lane: Lane, durationUs: number): number {
    const t = (durationUs - lane.minUs) / (lane.maxUs - lane.minUs);
    return lane.x + (Math.min(1, Math.max(0, t)) - 0.5) * LANE_HALF_SPREAD * 2;
}

function gaussian(): number {
    let u = 0;
    let v = 0;
    while (u === 0) u = Math.random();
    while (v === 0) v = Math.random();
    return Math.sqrt(-2 * Math.log(u)) * Math.cos(2 * Math.PI * v);
}

function sampleDuration(lane: Lane): number {
    let us = lane.meanUs + gaussian() * lane.stdUs;
    if (Math.random() < lane.spikeChance) us += lane.stdUs * 4;
    return Math.min(lane.maxUs, Math.max(lane.minUs, us));
}

function cssVar(name: string): string {
    return getComputedStyle(document.documentElement).getPropertyValue(name).trim();
}

export function initSavePace(): void {
    const canvas = document.getElementById("pace-canvas") as HTMLCanvasElement | null;
    const stage = canvas?.parentElement;
    if (!canvas || !stage) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const dpr = Math.min(window.devicePixelRatio || 1, 2);

    let width = 0;
    let height = 0;
    let efColor = "#f87171";
    let fwColor = "#58a6ff";
    let labelColor = "#93a59b";

    const lanes: Lane[] = [
        {
            x: 0.3,
            meanUs: 2160.3,
            stdUs: 68.9,
            minUs: 2049,
            maxUs: 2351,
            spikeChance: 0.07,
            labelEvery: 1,
            color: () => efColor,
            dots: [],
            nextSpawnAt: 0,
            spawned: 0,
        },
        {
            x: 0.7,
            meanUs: 130.9,
            stdUs: 8.9,
            minUs: 118.9,
            maxUs: 154,
            spikeChance: 0.06,
            labelEvery: 16,
            color: () => fwColor,
            dots: [],
            nextSpawnAt: 0,
            spawned: 0,
        },
    ];

    const readColors = () => {
        efColor = cssVar("--no") || efColor;
        fwColor = cssVar("--accent") || fwColor;
        labelColor = cssVar("--text-muted") || labelColor;
    };

    const resize = () => {
        const rect = stage.getBoundingClientRect();
        width = rect.width;
        height = rect.height;
        canvas.width = Math.round(width * dpr);
        canvas.height = Math.round(height * dpr);
        canvas.style.width = `${width}px`;
        canvas.style.height = `${height}px`;
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    };

    const drawDot = (lane: Lane, dot: Dot) => {
        const x = dot.xFrac * width;
        ctx.beginPath();
        ctx.arc(x, dot.y, 5, 0, Math.PI * 2);
        ctx.fillStyle = lane.color();
        ctx.fill();
        if (dot.label) {
            ctx.fillStyle = labelColor;
            ctx.font = "11px ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";
            ctx.textBaseline = "middle";
            ctx.fillText(dot.label, x + 12, dot.y);
        }
    };

    const labelFor = (lane: Lane, durationUs: number): string | null => {
        if (lane.spawned % lane.labelEvery !== 0) return null;
        const ms = (durationUs / 1000) * PHONE_FACTOR;
        return `${ms.toFixed(0)} ms`;
    };

    const drawStatic = () => {
        readColors();
        resize();
        ctx.clearRect(0, 0, width, height);
        for (const lane of lanes) {
            let y = 20;
            lane.spawned = 0;
            while (y < height - 10) {
                const duration = sampleDuration(lane);
                lane.spawned += 1;
                drawDot(lane, {
                    xFrac: spreadFrac(lane, duration),
                    y,
                    label: labelFor(lane, duration),
                });
                y += Math.max(14, duration * MS_PER_US * (FALL_SPEED / 1000));
            }
        }
    };

    if (reduced) {
        drawStatic();
        window.addEventListener("resize", drawStatic);
        return;
    }

    let running = false;
    let lastTime = 0;
    let frame = 0;

    const step = (time: number) => {
        if (!running) return;
        const dt = Math.min(0.05, (time - lastTime) / 1000);
        lastTime = time;

        ctx.clearRect(0, 0, width, height);
        for (const lane of lanes) {
            lane.nextSpawnAt -= dt * 1000;
            if (lane.nextSpawnAt <= 0) {
                const duration = sampleDuration(lane);
                lane.spawned += 1;
                lane.dots.push({
                    xFrac: spreadFrac(lane, duration),
                    y: -8,
                    label: labelFor(lane, duration),
                });
                lane.nextSpawnAt = duration * MS_PER_US;
            }
            for (const dot of lane.dots) {
                dot.y += FALL_SPEED * dt;
                drawDot(lane, dot);
            }
            lane.dots = lane.dots.filter((d) => d.y < height + 10);
        }
        frame = requestAnimationFrame(step);
    };

    const start = () => {
        if (running) return;
        running = true;
        lastTime = performance.now();
        readColors();
        resize();
        frame = requestAnimationFrame(step);
    };

    const stop = () => {
        running = false;
        cancelAnimationFrame(frame);
    };

    const visibility = new IntersectionObserver(
        (entries) => {
            for (const entry of entries) {
                if (entry.isIntersecting) start();
                else stop();
            }
        },
        { threshold: 0.1 },
    );
    visibility.observe(stage);

    window.addEventListener("resize", () => {
        if (running) resize();
    });

    new MutationObserver(readColors).observe(document.documentElement, {
        attributes: true,
        attributeFilter: ["data-theme"],
    });
}
