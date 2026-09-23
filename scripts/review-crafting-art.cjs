// Standalone asset-size study, not a Unity render.
const sharp = require('sharp'), path = require('path');
const root = path.resolve(__dirname, '..');
(async () => {
  const layers = [], w = 1040, h = 580;
  const text = (s, x, y, size = 20, colour = '#ded2bc') => layers.push({input: Buffer.from(
    `<svg width="${w}" height="${h}"><text x="${x}" y="${y}" font-family="serif" font-size="${size}" fill="${colour}">${s}</text></svg>`), left: 0, top: 0});
  const pic = async (name, x, y, width, height) => layers.push({input: await sharp(
    path.join(root, 'src/RestlessQoL/Assets', name + '.png')).resize(width, height, {fit:'fill'}).toBuffer(), left:x, top:y});
  text('RESTLESS / CRAFTING MATERIAL STUDY', 32, 46, 28);
  text('Asset placement and small-size checks — not an in-game screenshot', 32, 77, 17, '#a99d89');
  text('Station corner', 32, 130);
  await pic('craft-station-corner', 32, 152, 160, 160);
  text('Native station icon', 32, 347, 17);
  text('stays independent', 32, 373, 17);
  text('Requirement socket', 258, 130);
  await pic('craft-material-socket', 258, 152, 150, 173);
  text('25 / 30', 297, 307, 22);
  text('Separate icon + count', 258, 347, 17);
  text('Craft / Upgrade ribbons', 500, 130);
  await pic('craft-tab-ribbon', 500, 155, 210, 48);
  text('CRAFT', 562, 186, 22);
  await pic('craft-tab-ribbon', 736, 155, 210, 48);
  text('UPGRADE', 782, 186, 22, '#a99d89');
  text('Selection clasp', 500, 269);
  await pic('craft-selection-clasp', 502, 290, 12, 56);
  text('Stone Axe', 534, 325, 24);
  text('Smaller UI checks', 32, 435, 22);
  await pic('craft-station-corner', 32, 456, 72, 72);
  await pic('craft-material-socket', 145, 456, 72, 72);
  text('25', 172, 521, 16);
  await pic('craft-tab-ribbon', 270, 474, 140, 38);
  text('CRAFT', 311, 499, 17);
  await pic('craft-selection-clasp', 461, 478, 6, 28);
  text('Stone Axe', 482, 500, 20);
  text('Quiet materials. Live labels. Native icons.', 638, 499, 18, '#a99d89');
  await sharp({create:{width:w,height:h,channels:4,background:'#22221f'}})
    .composite(layers).png().toFile(path.join(root,'art/crafting/review-board.png'));
})().catch(e=>{console.error(e);process.exit(1)});
