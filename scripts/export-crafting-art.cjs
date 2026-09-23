// Asset preparation only: alpha-bound crop, downsample and transparent padding.
// npm install sharp; node scripts/export-crafting-art.cjs
const sharp = require('sharp');
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '..');
const names = [
  ['craft-station-corner', 256],
  ['craft-material-socket', 192],
  ['craft-tab-ribbon', 512],
  ['craft-selection-clasp', 48]
];
(async () => {
  const output = path.join(root, 'src/RestlessQoL/Assets');
  fs.mkdirSync(output, {recursive: true});
  const manifest = [];
  for (const [name, width] of names) {
    const source = path.join(root, 'art/crafting/originals', name + '.png');
    const {data, info} = await sharp(source).ensureAlpha().raw().toBuffer({resolveWithObject: true});
    let l = info.width, t = info.height, r = -1, b = -1;
    for (let y = 0; y < info.height; y++) for (let x = 0; x < info.width; x++) {
      if (data[(y * info.width + x) * 4 + 3] <= 8) continue;
      l = Math.min(l, x); t = Math.min(t, y); r = Math.max(r, x); b = Math.max(b, y);
    }
    if (r < l) throw new Error('Empty asset: ' + name);
    const file = path.join(output, name + '.png');
    await sharp(source).extract({left: l, top: t, width: r-l+1, height: b-t+1})
      .resize({width}).extend({left: 2, right: 2, top: 2, bottom: 2, background: '#00000000'})
      .png().toFile(file);
    const m = await sharp(file).metadata();
    manifest.push({name, width: m.width, height: m.height, bytes: fs.statSync(file).size,
      crop: {left: l, top: t, width: r-l+1, height: b-t+1}});
  }
  fs.writeFileSync(path.join(root, 'art/crafting/manifest.json'), JSON.stringify(manifest, null, 2) + '\n');
})().catch(e => { console.error(e); process.exit(1); });
