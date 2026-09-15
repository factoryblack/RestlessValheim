// Mechanical export of generated artwork: alpha trim, scale and tintable edge masks.
// Usage: node scripts/prepare-materials.cjs PANEL CORNER CHIP
const sharp = require('sharp');
const path = require('path');
const fs = require('fs');
const root = path.resolve(__dirname, '../src/RestlessQoL/Assets');

async function exportAsset(source, name, width) {
  const {data, info} = await sharp(source).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let x0=info.width, y0=info.height, x1=0, y1=0;
  for(let y=0;y<info.height;y++) for(let x=0;x<info.width;x++) {
    if(data[(y*info.width+x)*4+3]>4) {x0=Math.min(x0,x); y0=Math.min(y0,y); x1=Math.max(x1,x); y1=Math.max(y1,y);}
  }
  await sharp(source).extract({left:x0,top:y0,width:x1-x0+1,height:y1-y0+1})
    .resize({width}).extend({top:2,bottom:2,left:2,right:2,background:'#00000000'})
    .png().toFile(path.join(root,name+'.png'));
}

async function edgeMask(name) {
  const {data,info}=await sharp(path.join(root,name+'.png')).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const output=Buffer.alloc(data.length);
  for(let y=0;y<info.height;y++) for(let x=0;x<info.width;x++) {
    const index=(y*info.width+x)*4; let inside=255;
    for(let dy=-2;dy<=2;dy++) for(let dx=-2;dx<=2;dx++) {
      const xx=x+dx, yy=y+dy;
      inside=Math.min(inside,xx<0||yy<0||xx>=info.width||yy>=info.height?0:data[(yy*info.width+xx)*4+3]);
    }
    output[index]=output[index+1]=output[index+2]=255;
    output[index+3]=Math.max(0,data[index+3]-inside);
  }
  await sharp(output,{raw:info}).png().toFile(path.join(root,name+'-rim.png'));
}

(async()=>{
  if(process.argv.length!==5) throw Error('Pass panel, corner and chip source PNGs.');
  await exportAsset(process.argv[2],'paper-panel',512);
  await exportAsset(process.argv[3],'paper-corner',256);
  await exportAsset(process.argv[4],'paper-chip',512);
  await edgeMask('paper-panel');
  await edgeMask('paper-chip');
  for(const name of ['paper-tree','paper-knot'])
    await sharp(path.join(root,'_svg',name+'.svg')).png().toFile(path.join(root,name+'.png'));
  for(const name of fs.readdirSync(root).filter(n=>n.startsWith('paper-')&&n.endsWith('.png'))) {
    const m=await sharp(path.join(root,name)).metadata();
    if(!m.hasAlpha) throw Error(name+' lost alpha');
    console.log(name,m.width+'x'+m.height,fs.statSync(path.join(root,name)).size+' bytes');
  }
})().catch(e=>{console.error(e);process.exitCode=1;});
