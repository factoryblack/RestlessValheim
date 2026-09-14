import { readFileSync, writeFileSync, readdirSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { Resvg } from "@resvg/resvg-js";

const root = dirname(fileURLToPath(import.meta.url));
const svgDir = join(root, "..", "src", "RestlessQoL", "Assets", "_svg");
const outDir = join(root, "..", "src", "RestlessQoL", "Assets");
mkdirSync(outDir, { recursive: true });

function clean(svg) {
  const width = svg.match(/\bwidth="([\d.]+)"/);
  const height = svg.match(/\bheight="([\d.]+)"/);
  if (width && height) {
    const plate = new RegExp(
      `<rect width="${width[1]}" height="${height[1]}" fill="#[0-9A-Fa-f]{3,8}"/>`,
    );
    svg = svg.replace(plate, "");
  }
  svg = svg.replace(
    /<path d="M-[\d.]+ -[\d.]+C[^"]+" fill="black" fill-opacity="0\.1"\/>/g,
    "",
  );
  svg = svg.replace(/<rect[^>]*stroke="#8A38F5"[^>]*\/>/g, "");
  return svg;
}

const scale = Number(process.argv[2] || 2);
const files = readdirSync(svgDir).filter((name) => name.endsWith(".svg"));
for (const name of files) {
  const raw = readFileSync(join(svgDir, name), "utf8");
  const svg = clean(raw);
  const pngName = name.replace(/\.svg$/, ".png");
  const resvg = new Resvg(svg, {
    fitTo: { mode: "zoom", value: scale },
    background: "rgba(0,0,0,0)",
  });
  const png = resvg.render().asPng();
  writeFileSync(join(outDir, pngName), png);
  console.log(pngName, png.length);
}
