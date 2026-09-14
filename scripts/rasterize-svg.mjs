import { readFileSync, writeFileSync } from "node:fs";
import { Resvg } from "@resvg/resvg-js";

const [, , svgPath, pngPath, width] = process.argv;
const svg = readFileSync(svgPath);
const resvg = new Resvg(svg, {
  fitTo: width ? { mode: "width", value: Number(width) } : { mode: "original" },
  background: "rgba(0,0,0,0)",
});
writeFileSync(pngPath, resvg.render().asPng());
