interface LaneSpec {
    mean: number;
    std: number;
    min: number;
    max: number;
    spikeChance: number;
    spikeMin: number;
    spikeMax: number;
    tMin: number;
    tMax: number;
    labelEvery: number;
    staticCount: number;
}

interface PaceDot {
    lane: number;
    t: number;
    x: number;
    y: number;
    radius: number;
    spike: boolean;
    label: string | null;
}

const LANES: LaneSpec[] = [
    {
        mean: 2160.3, std: 68.9, min: 2049, max: 2351,
        spikeChance: 0.07, spikeMin: 2300, spikeMax: 2351,
        tMin: 1920, tMax: 2400, labelEvery: 1, staticCount: 5,
    },
    {
        mean: 130.9, std: 8.9, min: 118.9, max: 154,
        spikeChance: 0.06, spikeMin: 148, spikeMax: 154,
        tMin: 105, tMax: 160, labelEvery: 16, staticCount: 60,
    },
];

const LANE_PAD = 0.12;
const FALL = 170;
const GAP_PER_US = 0.5;
const PHONE_FACTOR = 100;

function clamp(value: number, min: number, max: number): number {
    return value < min ? min : value > max ? max : value;
}

function gauss(mean: number, std: number): number {
    let u = 0;
    let v = 0;
    while (u === 0) u = Math.random();
    while (v === 0) v = Math.random();
    const n = Math.sqrt(-2 * Math.log(u)) * Math.cos(2 * Math.PI * v);
    return mean + n * std;
}

function sample(spec: LaneSpec): { t: number; spike: boolean } {
    if (Math.random() < spec.spikeChance) {
        return { t: spec.spikeMin + Math.random() * (spec.spikeMax - spec.spikeMin), spike: true };
    }
    return { t: clamp(gauss(spec.mean, spec.std), spec.min, spec.max), spike: false };
}

function tipLabel(us: number): string {
    return Math.round((us / 1000) * PHONE_FACTOR) + " ms";
}

function readColors() {
    const styles = getComputedStyle(document.documentElement);
    const accent = styles.getPropertyValue("--accent").trim() || "#58a6ff";
    const light = document.documentElement.getAttribute("data-theme") === "light";
    return {
        ef: light ? "#b9791f" : "#e0a94a",
        fw: accent,
        grid: light ? "rgba(15,23,42,0.12)" : "rgba(110,118,129,0.22)",
        labelText: light ? "#1f2328" : "#e6edf3",
        labelBg: light ? "rgba(255,255,255,0.94)" : "rgba(22,27,34,0.92)",
    };
}

export function initSavePace(): void {
    const canvas = document.getElementById("pace-canvas") as HTMLCanvasElement | null;
    if (!canvas) {
        return;
    }
    const ctx = canvas.getContext("2d");
    if (!ctx) {
        return;
    }

    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    let colors = readColors();
    let width = 0;
    let height = 0;

    function resize() {
        const dpr = Math.min(window.devicePixelRatio || 1, 2);
        width = canvas!.clientWidth;
        height = canvas!.clientHeight;
        canvas!.width = Math.round(width * dpr);
        canvas!.height = Math.round(height * dpr);
        ctx!.setTransform(dpr, 0, 0, dpr, 0, 0);
    }

    function layout() {
        const topPad = 44;
        const bottomPad = 20;
        const mid = width / 2;
        return {
            topPad,
            bottomPad,
            mid,
            laneTop: topPad,
            laneBottom: height - bottomPad,
            lanes: [
                { x0: 44, x1: mid - 16 },
                { x0: mid + 16, x1: width - 18 },
            ],
        };
    }

    function timeToX(lane: number, t: number): number {
        const l = layout().lanes[lane];
        const spec = LANES[lane];
        const frac = clamp((t - spec.tMin) / (spec.tMax - spec.tMin), 0, 1);
        return l.x0 + (LANE_PAD + frac * (1 - 2 * LANE_PAD)) * (l.x1 - l.x0);
    }

    function makeDot(lane: number, y: number, s: { t: number; spike: boolean }, labeled: boolean): PaceDot {
        return {
            lane,
            t: s.t,
            x: timeToX(lane, s.t),
            y,
            radius: s.spike ? 4.6 : 3.1,
            spike: s.spike,
            label: labeled ? tipLabel(s.t) : null,
        };
    }

    function roundRect(x: number, y: number, w: number, h: number, r: number) {
        ctx!.beginPath();
        ctx!.moveTo(x + r, y);
        ctx!.arcTo(x + w, y, x + w, y + h, r);
        ctx!.arcTo(x + w, y + h, x, y + h, r);
        ctx!.arcTo(x, y + h, x, y, r);
        ctx!.arcTo(x, y, x + w, y, r);
        ctx!.closePath();
    }

    function dotAlpha(dot: PaceDot): number {
        const lay = layout();
        const fadeIn = clamp((dot.y - lay.laneTop) / 24, 0, 1);
        const fadeOut = clamp((lay.laneBottom - dot.y) / 40, 0, 1);
        return fadeIn * fadeOut;
    }

    function drawBackground() {
        const lay = layout();
        ctx!.clearRect(0, 0, width, height);
        ctx!.strokeStyle = colors.grid;
        ctx!.lineWidth = 1;
        ctx!.beginPath();
        ctx!.moveTo(lay.mid, lay.topPad - 8);
        ctx!.lineTo(lay.mid, lay.laneBottom + 8);
        ctx!.stroke();
    }

    function drawDot(dot: PaceDot) {
        const alpha = dotAlpha(dot);
        const color = dot.lane === 0 ? colors.ef : colors.fw;

        ctx!.save();
        ctx!.globalAlpha = alpha;
        if (dot.spike) {
            ctx!.shadowColor = color;
            ctx!.shadowBlur = 14;
        }
        ctx!.fillStyle = color;
        ctx!.beginPath();
        ctx!.arc(dot.x, dot.y, dot.radius, 0, Math.PI * 2);
        ctx!.fill();
        ctx!.restore();
    }

    function drawLabel(dot: PaceDot) {
        if (!dot.label) {
            return;
        }
        const alpha = dotAlpha(dot);
        if (alpha <= 0.05) {
            return;
        }
        const color = dot.lane === 0 ? colors.ef : colors.fw;
        ctx!.font = "600 12px ui-monospace, SFMono-Regular, Menlo, Consolas, monospace";
        const tw = ctx!.measureText(dot.label).width;
        const padX = 8;
        const h = 20;
        const w = tw + padX * 2;
        const gap = 12;
        let bx = dot.lane === 0 ? dot.x - gap - w : dot.x + gap;
        bx = clamp(bx, 4, width - w - 4);
        const by = dot.y - h / 2;
        const connectX = dot.lane === 0 ? bx + w : bx;

        ctx!.save();
        ctx!.globalAlpha = alpha * 0.45;
        ctx!.strokeStyle = color;
        ctx!.lineWidth = 1;
        ctx!.beginPath();
        ctx!.moveTo(dot.x, dot.y);
        ctx!.lineTo(connectX, dot.y);
        ctx!.stroke();
        ctx!.restore();

        ctx!.save();
        ctx!.globalAlpha = alpha;
        roundRect(bx, by, w, h, 7);
        ctx!.fillStyle = colors.labelBg;
        ctx!.fill();
        ctx!.strokeStyle = color;
        ctx!.lineWidth = 1;
        ctx!.stroke();
        ctx!.fillStyle = colors.labelText;
        ctx!.textAlign = "center";
        ctx!.textBaseline = "middle";
        ctx!.fillText(dot.label, bx + w / 2, by + h / 2 + 0.5);
        ctx!.restore();
        ctx!.textAlign = "left";
        ctx!.textBaseline = "alphabetic";
    }

    if (reduceMotion) {
        const paint = () => {
            resize();
            drawBackground();
            const lay = layout();
            const span = lay.laneBottom - lay.laneTop - 12;
            const placed: PaceDot[] = [];
            for (let lane = 0; lane < 2; lane++) {
                const count = LANES[lane].staticCount;
                for (let i = 0; i < count; i++) {
                    const y = lay.laneTop + 6 + (i / (count - 1)) * span;
                    const labeled = i % LANES[lane].labelEvery === 0;
                    const dot = makeDot(lane, y, sample(LANES[lane]), labeled);
                    drawDot(dot);
                    if (dot.label) {
                        placed.push(dot);
                    }
                }
            }
            for (const dot of placed) {
                drawLabel(dot);
            }
        };
        paint();
        window.addEventListener("resize", paint);
        return;
    }

    const dots: PaceDot[] = [];
    const gapTimer = [0, 0];
    const currentGap = [LANES[0].mean * GAP_PER_US, LANES[1].mean * GAP_PER_US];
    const spawnCount = [0, 0];
    let lastTime = 0;
    let running = false;
    let started = false;

    function frame(now: number) {
        if (!running) {
            return;
        }
        const dt = lastTime === 0 ? 0 : Math.min((now - lastTime) / 1000, 0.05);
        lastTime = now;

        const lay = layout();
        for (let lane = 0; lane < 2; lane++) {
            gapTimer[lane] += dt * 1000;
            while (gapTimer[lane] >= currentGap[lane]) {
                gapTimer[lane] -= currentGap[lane];
                const s = sample(LANES[lane]);
                spawnCount[lane]++;
                const labeled = spawnCount[lane] % LANES[lane].labelEvery === 0;
                dots.push(makeDot(lane, lay.laneTop, s, labeled));
                currentGap[lane] = s.t * GAP_PER_US;
            }
        }

        for (let i = dots.length - 1; i >= 0; i--) {
            dots[i].y += FALL * dt;
            if (dots[i].y > lay.laneBottom + 6) {
                dots.splice(i, 1);
            }
        }

        drawBackground();
        for (const dot of dots) {
            drawDot(dot);
        }
        for (const dot of dots) {
            drawLabel(dot);
        }
        requestAnimationFrame(frame);
    }

    function start() {
        if (running) {
            return;
        }
        running = true;
        lastTime = 0;
        if (!started) {
            started = true;
            resize();
        }
        requestAnimationFrame(frame);
    }

    function stop() {
        running = false;
    }

    window.addEventListener("resize", () => {
        if (started) {
            resize();
            for (const dot of dots) {
                dot.x = timeToX(dot.lane, dot.t);
            }
        }
    });

    const themeObserver = new MutationObserver(() => {
        colors = readColors();
    });
    themeObserver.observe(document.documentElement, {
        attributes: true,
        attributeFilter: ["data-theme"],
    });

    if ("IntersectionObserver" in window) {
        const io = new IntersectionObserver(
            (entries) => {
                for (const entry of entries) {
                    if (entry.isIntersecting) {
                        start();
                    } else {
                        stop();
                    }
                }
            },
            { threshold: 0.1 },
        );
        io.observe(canvas);
    } else {
        start();
    }
}
