const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const sharp = require('sharp');
const {colors} = require('./shapes.cjs');
const {minimumDetail,proofImage} = require('./proof.cjs');
const buildProfiles = require('./profiles.cjs');

const root = path.resolve(__dirname, '../../..');
const output = path.join(root, '.codex/previews/local-patterns');
const resources = path.join(root, 'Assets/Resources/YarnMatch/Patterns');
const publish = process.argv.includes('--publish');
const categories = ['food', 'nature', 'animals', 'objects', 'idioms'];
const headings = ['水果与美食', '植物与自然', '动物', '生活与物品', '四字成语'];
const palette = Object.values(colors).map(color => color.slice(1));
const rgb = palette.map(hex => [parseInt(hex.slice(0,2),16),parseInt(hex.slice(2,4),16),parseInt(hex.slice(4,6),16)]);
const symbols = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ';
const escape = value => value.replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&apos;'}[c]));

function closest(r, g, b) {
  let best = 0, score = Infinity;
  for (let i = 0; i < rgb.length; i++) {
    const delta = 2*(r-rgb[i][0])**2 + 4*(g-rgb[i][1])**2 + 3*(b-rgb[i][2])**2;
    if (delta < score) { score=delta; best=i; }
  }
  return best;
}

async function render(item) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 64 64">${item.art}</svg>`;
  const {data} = await sharp(Buffer.from(svg)).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const pixels = Buffer.alloc(64*64*4);
  const rows = [], mask = [], used = new Set();
  let occupied = 0;
  for (let y=0; y<64; y++) {
    let row='', occupancy='';
    for (let x=0; x<64; x++) {
      const offset=(y*64+x)*4;
      const visible = data[offset+3]>=128;
      const color = visible ? closest(data[offset],data[offset+1],data[offset+2]) : 0;
      row+=symbols[color]; occupancy+=visible?'1':'0';
      if (!visible) continue;
      occupied++; used.add(color);
      pixels.set([...rgb[color],255],offset);
    }
    rows.push(row); mask.push(occupancy);
  }
  if (used.size<4) throw new Error(`${item.id}: only ${used.size} authored foreground colors`);
  const png = await sharp(pixels,{raw:{width:64,height:64,channels:4}}).png().toBuffer();
  const template={id:item.id,title:item.title,category:item.category,minSize:item.minSize,palette,rows,mask};
  template.minSize=minimumDetail(template);
  template.profiles=buildProfiles(template);
  return {item, png, occupied, colorCount:used.size, hash:crypto.createHash('sha256').update(pixels).digest('hex'),
    template};
}

async function contactSheet(batch, destination, heading) {
  const columns=5, cellWidth=176, cellHeight=175, header=42;
  const layers=[];
  let labels=`<text x="12" y="28" font-family="SimHei" font-size="20" fill="#29323A">${escape(heading)}</text>`;
  for (let i=0;i<batch.length;i++) {
    const x=(i%columns)*cellWidth, y=Math.floor(i/columns)*cellHeight+header;
    layers.push({input:await sharp(batch[i].png).resize(144,144,{kernel:'nearest'}).png().toBuffer(),left:x+16,top:y});
    labels+=`<text x="${x+88}" y="${y+163}" text-anchor="middle" font-family="SimHei" font-size="16" fill="#29323A">${escape(batch[i].item.title)}</text>`;
  }
  const height=Math.ceil(batch.length/columns)*cellHeight+header;
  layers.push({input:Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${columns*cellWidth}" height="${height}">${labels}</svg>`),left:0,top:0});
  await sharp({create:{width:columns*cellWidth,height,channels:4,background:'#EDF1F5'}}).composite(layers).png().toFile(destination);
}

async function main() {
  fs.mkdirSync(output,{recursive:true});
  const rendered=[],ids=new Set(),hashes=new Set();
  for (const category of categories) {
    if (!fs.existsSync(path.join(__dirname,`${category}.cjs`))) continue;
    const items=require(`./${category}.cjs`),batch=[];
    for (const item of items) {
      if (ids.has(item.id)) throw new Error(`Duplicate subject: ${item.id}`);
      ids.add(item.id);
      const result=await render(item);
      if (hashes.has(result.hash)) throw new Error(`Duplicate image: ${item.id}`);
      hashes.add(result.hash);
      batch.push(result); rendered.push(result);
      fs.writeFileSync(path.join(output,`${item.id}.png`),result.png);
    }
    await contactSheet(batch,path.join(output,`${category}-sheet.png`),`${headings[categories.indexOf(category)]} / ${items.length}`);
    const proofs=[];
    for (const result of batch) {
      const detail=result.template.minSize;
      proofs.push({png:(await proofImage(result.template,detail)).png,item:{title:`${result.item.title} / ${detail}列`}});
    }
    await contactSheet(proofs,path.join(output,`${category}-detail-sheet.png`),`${headings[categories.indexOf(category)]} / 最低密度原色预览`);
    console.log(`${category}: ${items.length} local subjects`);
  }
  const featured = ['apple','umbrella','cat','sunflower','idiom','watermelon','sailboat','fox','lotus','rainbow',
    'donut','rocket','frog','monstera','butterfly','camera','gift','penguin','cactus','smoothsailing'];
  await contactSheet(featured.map(id=>rendered.find(result=>result.item.id===id)).filter(Boolean),
    path.join(output,'review-sheet.png'),`本地图案 / 共 ${rendered.length} 张 / 精选预览`);
  fs.writeFileSync(path.join(output,'manifest.json'),JSON.stringify(rendered.map(({item,png,...data})=>({
    id:item.id,title:item.title,category:item.category,minSize:data.template.minSize,colors:data.colorCount,occupied:data.occupied,sha256:data.hash
  })),null,2)+'\n');
  if (!publish) { console.log(`Preview only: ${rendered.length} images in ${output}`); return; }
  if (rendered.length!==200) throw new Error(`Publish requires 200 subjects, got ${rendered.length}`);
  const previous=JSON.parse(fs.readFileSync(path.join(resources,'PatternTemplates.json'),'utf8')).patterns;
  for (const item of previous) {
    if (!ids.has(item.id.replace(/^pattern_\d+_/,''))) throw new Error(`Missing existing subject: ${item.id}`);
  }
  const existing=new Map(previous.map(item=>[item.id.replace(/^pattern_\d+_/,''),item.id]));
  let next=Math.max(...previous.map(item=>Number(item.id.match(/^pattern_(\d+)_/)[1])))+1;
  const meta=fs.readFileSync(path.join(resources,'pattern_40_umbrella.png.meta'),'utf8');
  for (const result of rendered) {
    const id=existing.get(result.item.id)||`pattern_${String(next++).padStart(3,'0')}_${result.item.id}`;
    result.template.id=id;
    fs.writeFileSync(path.join(resources,`${id}.png`),result.png);
    if (!fs.existsSync(path.join(resources,`${id}.png.meta`))) {
      const guid=crypto.createHash('md5').update(`YarnMatch.LocalPattern.${id}`).digest('hex');
      fs.writeFileSync(path.join(resources,`${id}.png.meta`),meta.replace(/^guid: .+$/m,`guid: ${guid}`)
        .replace(/spriteID: .+/,`spriteID: ${crypto.createHash('md5').update(id+'.sprite').digest('hex')}`));
    }
  }
  // Compile authored data, not runtime artwork. PNG and grid share these exact RGBA pixels.
  fs.writeFileSync(path.join(resources,'PatternTemplates.json'),JSON.stringify({patterns:rendered.map(item=>item.template)},null,2)+'\n');
  console.log(`Published ${rendered.length} unique 64x64 PNGs and their palette/mask data.`);
}
main().catch(error=>{ console.error(error.message); process.exitCode=1; });
