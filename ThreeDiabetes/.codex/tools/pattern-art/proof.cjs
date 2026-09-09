const sharp = require('sharp');

function bounds(template) {
  let left=64, top=64, right=0, bottom=0;
  template.mask.forEach((row,y)=>Array.from(row).forEach((value,x)=>{
    if (value!=='1') return;
    left=Math.min(left,x); top=Math.min(top,y);
    right=Math.max(right,x); bottom=Math.max(bottom,y);
  }));
  return {left,top,width:right-left+1,height:bottom-top+1};
}

// Offline art proof: use the runtime sampler's occupied-area rule, without palette reduction.
function sample(template, detail, region=bounds(template)) {
  const width=detail, height=Math.max(1,Math.min(100,Math.round(detail*region.height/region.width)));
  const scale=Math.min(width/region.width,height/region.height);
  const offsetX=(width-region.width*scale)/2, offsetY=(height-region.height*scale)/2;
  const cells=[], used=new Set(), span=1/scale;
  for (let y=0;y<height;y++) for (let x=0;x<width;x++) {
    const left=region.left+(x-offsetX)/scale, top=region.top+(y-offsetY)/scale;
    const weights=new Array(template.palette.length).fill(0);
    let covered=0;
    for (let sy=Math.max(0,Math.floor(top));sy<Math.min(64,Math.ceil(top+span));sy++) {
      for (let sx=Math.max(0,Math.floor(left));sx<Math.min(64,Math.ceil(left+span));sx++) {
        if (template.mask[sy][sx]!=='1') continue;
        const area=(Math.min(sx+1,left+span)-Math.max(sx,left))*(Math.min(sy+1,top+span)-Math.max(sy,top));
        weights[parseInt(template.rows[sy][sx],36)]+=area;
        covered+=area;
      }
    }
    const color=covered<span*span*.25 ? -1 : weights.indexOf(Math.max(...weights));
    cells.push(color);
    if (color>=0) used.add(color);
  }
  return {width,height,cells,colorCount:used.size};
}

function minimumDetail(template) {
  const region=bounds(template);
  let minimum=template.minSize;
  for (let detail=minimum;detail<=50;detail++) {
    if (sample(template,detail,region).colorCount<4) minimum=detail+1;
  }
  if (minimum>50) throw new Error(`${template.id}: source accents need more area to survive sampling`);
  return minimum;
}

async function proofImage(template, detail) {
  const {width,height,cells,colorCount}=sample(template,detail);
  const pixels=Buffer.alloc(width*height*4);
  cells.forEach((color,i)=>{
    if (color<0) return;
    const rgb=Number.parseInt(template.palette[color],16);
    pixels.set([(rgb>>16)&255,(rgb>>8)&255,rgb&255,255],i*4);
  });
  return {colorCount,png:await sharp(pixels,{raw:{width,height,channels:4}})
    .resize(144,144,{fit:'contain',background:'#00000000',kernel:'nearest'}).png().toBuffer()};
}

module.exports={minimumDetail,proofImage,sample};
