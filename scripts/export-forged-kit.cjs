// NODE_PATH="$CODEX_PRIMARY_RUNTIME_NODE_MODULES" node scripts/export-forged-kit.cjs BADGE ACTION
// Mechanical alpha trim/resize of generated art; editable vector glyphs remain in Assets/_svg.
const sharp = require('sharp');
const fs = require('fs');
const path = require('path');
const root = path.resolve(__dirname, '../src/RestlessQoL/Assets');
const glyphs = {
  blunt: '<path d="M18 52L39 20M29 10L48 22L55 12L36 2Z" fill="none" stroke="white" stroke-width="7" stroke-linejoin="bevel"/>',
  slash: '<path d="M11 54L19 36L49 5L56 7L54 15L25 45Z" fill="white"/><path d="M12 36L29 52" stroke="white" stroke-width="5"/>',
  pierce: '<path d="M12 53L39 26M32 22L57 5L43 32Z" fill="none" stroke="white" stroke-width="5" stroke-linejoin="miter"/>',
  fire: '<path d="M34 3C41 20 51 20 52 37C54 55 36 62 22 54C4 44 12 29 21 21C19 35 27 35 30 27C34 20 28 13 34 3Z" fill="white"/>',
  frost: '<path d="M32 3V61M7 18L57 46M7 46L57 18M23 7L32 16L41 7M23 57L32 48L41 57M7 28L18 24L17 12M47 52L46 40L57 36M7 36L18 40L17 52M47 12L46 24L57 28" fill="none" stroke="white" stroke-width="4"/>',
  poison: '<path d="M32 4C29 18 12 29 12 42A20 17 0 0052 42C52 29 36 18 32 4Z" fill="white"/><path d="M23 38L28 43M41 38L36 43M26 50H38" stroke="#333" stroke-width="4"/>',
  lightning: '<path d="M35 3L12 36H29L23 61L54 24H36L44 3Z" fill="white"/>',
  spirit: '<path d="M32 3L39 24L59 32L39 39L32 61L25 39L5 32L25 24Z" fill="white"/>',
  chop: '<path d="M21 58L35 7M35 13C47 7 54 10 59 20L43 36L32 29" fill="none" stroke="white" stroke-width="6"/>',
  pickaxe: '<path d="M17 59L40 13M10 14Q34 1 58 33Q38 18 10 14Z" fill="none" stroke="white" stroke-width="5"/>',
  station: '<path d="M10 25H54L46 35H25L19 31H10ZM28 35V46H44V54H17V46H24V35" fill="white"/><path d="M29 21L37 7M30 5L45 12" stroke="white" stroke-width="6"/>',
};
async function source(name, body, w=64,h=64) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}" viewBox="0 0 ${w} ${h}">${body}</svg>`;
  fs.writeFileSync(path.join(root,'_svg',name+'.svg'),svg+'\n');
  await sharp(Buffer.from(svg)).png().toFile(path.join(root,name+'.png'));
}
async function trim(input,name,width) {
  const {data,info}=await sharp(input).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let l=info.width,t=info.height,r=0,b=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>8){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x);b=Math.max(b,y);}
  if(l>=r||t>=b)throw Error('Empty alpha: '+input);
  await sharp(input).extract({left:l,top:t,width:r-l+1,height:b-t+1}).resize({width}).extend({left:2,right:2,top:2,bottom:2,background:'#00000000'}).png().toFile(path.join(root,name+'.png'));
}
(async()=>{
  fs.mkdirSync(path.join(root,'_svg'),{recursive:true});
  if(process.argv.length===4){await trim(process.argv[2],'forged-badge',512);await trim(process.argv[3],'forged-action',768);}
  for(const [name,body] of Object.entries(glyphs))await source('glyph-'+name,body);
  await source('quality-gem','<path d="M16 1L30 15L16 31L2 16Z" fill="#55402a"/><path d="M16 3L27 15L16 28L5 16Z" fill="#bc8643"/><path d="M16 3L16 15L5 16Z" fill="#fae3a3"/><path d="M16 3L27 15L16 15Z" fill="#e9bb69"/><path d="M16 15L27 15L16 28Z" fill="#795225"/><path d="M16 15L16 28L5 16Z" fill="#c69953"/><path d="M16 8L22 15L16 22L10 15Z" fill="none" stroke="#efd59b" stroke-width="1"/>',32,32);
  await source('station-medallion','<path d="M20 3H44L61 20V44L44 61H20L3 44V20Z" fill="#332b22" stroke="#a87a3d" stroke-width="3"/><path d="M21 9H43L55 21V43L43 55H21L9 43V21Z" fill="#1e2020" stroke="#66513a" stroke-width="2"/>');
  await source('tab-inset','<path d="M8 2H152L158 8V48L152 54H8L2 48V8Z" fill="#292821" stroke="#706044" stroke-width="2"/><path d="M12 49H148" stroke="#bd9557" stroke-width="2"/>',160,56);
  await source('tab-marker','<path d="M2 7L30 6L38 1L46 6L74 7L46 8L38 13L30 8Z" fill="#e6bc71"/>',76,14);
  await source('category-ribbon','<path d="M8 2L157 3L151 9L158 17L151 24L157 30L6 29L2 21L5 12Z" fill="#3c3328"/><path d="M9 3L146 4M8 28L145 29" stroke="#8f7246" stroke-width="1"/>',160,32);
  await source('scroll-thumb','<path d="M7 1L13 7V57L7 63L1 57V7Z" fill="#aa7e40"/><path d="M7 4L10 8V55L7 60L4 55V8Z" fill="#e2ba76"/>',14,64);
  const layers=[];
  const caption=(text,x,y,size=18)=>layers.push({input:Buffer.from(`<svg width="900" height="540"><text x="${x}" y="${y}" fill="#e5d5b8" font-family="serif" font-size="${size}">${text}</text></svg>`),left:0,top:0});
  caption('RESTLESS / FORGED COMPONENTS',30,36,24);
  layers.push({input:await sharp(path.join(root,'forged-action.png')).resize(420,72).toBuffer(),left:30,top:60});
  caption('Craft',205,105,24);
  layers.push({input:await sharp(path.join(root,'forged-badge.png')).resize(240,48).toBuffer(),left:30,top:155});
  layers.push({input:await sharp(path.join(root,'glyph-fire.png')).resize(24,24).toBuffer(),left:48,top:167});
  caption('Fire 15 (4–8)',82,185,18);
  layers.push({input:await sharp(path.join(root,'category-ribbon.png')).resize(124,28).toBuffer(),left:30,top:224});
  caption('Two-handed',39,244,16);
  for(let i=0;i<3;i++)layers.push({input:await sharp(path.join(root,'quality-gem.png')).resize(16,20).toBuffer(),left:164+i*18,top:228});
  layers.push({input:await sharp(path.join(root,'station-medallion.png')).resize(56,56).toBuffer(),left:330,top:155});
  layers.push({input:await sharp(path.join(root,'glyph-station.png')).resize(26,26).toBuffer(),left:345,top:163});
  caption('2',355,201,16);
  layers.push({input:await sharp(path.join(root,'tab-inset.png')).resize(110,42).toBuffer(),left:330,top:225});
  layers.push({input:await sharp(path.join(root,'tab-marker.png')).resize(46,9).toBuffer(),left:362,top:263});
  caption('Upgrade',352,250,16);
  let i=0;for(const name of Object.keys(glyphs)){const x=30+(i%6)*140,y=315+Math.floor(i/6)*100;layers.push({input:await sharp(path.join(root,'glyph-'+name+'.png')).resize(32,32).toBuffer(),left:x,top:y});caption(name,x,y+53,15);i++;}
  caption('Actual exported assets · composition proof, not a Unity screenshot',30,523,16);
  await sharp({create:{width:900,height:540,channels:4,background:'#242321'}}).composite(layers).png().toFile(path.resolve(__dirname,'../docs/forged-kit-preview.png'));
  for(const name of fs.readdirSync(root).filter(n=>/^(forged-|glyph-|quality-gem|station-medallion|tab-inset|tab-marker|category-ribbon|scroll-thumb).*\.png$/.test(n))){const m=await sharp(path.join(root,name)).metadata();if(!m.hasAlpha)throw Error(name+' missing alpha');console.log(name,m.width,m.height);}
})().catch(e=>{console.error(e);process.exitCode=1;});
