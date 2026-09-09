const {sample} = require('./proof.cjs');
const {colors} = require('./shapes.cjs');
const rgb=Object.values(colors).map(hex=>parseInt(hex.slice(1),16));
const distance=(a,b)=>2*((a>>16&255)-(b>>16&255))**2
  +4*((a>>8&255)-(b>>8&255))**2+3*((a&255)-(b&255))**2;

function colorMap(cells,limit) {
  const counts=rgb.map(()=>0);
  cells.forEach(color=>{if(color>=0) counts[color]++;});
  const kept=[counts.indexOf(Math.max(...counts))];
  while(kept.length<limit) {
    let best=-1,score=-1;
    counts.forEach((count,color)=>{
      if(!count||kept.includes(color)) return;
      const value=Math.min(...kept.map(other=>distance(rgb[color],rgb[other])))*Math.sqrt(count);
      if(value>score) {best=color;score=value;}
    });
    if(best<0) break;
    kept.push(best);
  }
  return rgb.map((value,color)=>kept.includes(color)?color:kept.reduce((best,other)=>
    distance(value,rgb[other])<distance(value,rgb[best])?other:best,kept[0]));
}

function pressure(grid,map) {
  const counts=rgb.map(()=>0), opening=rgb.map(()=>0);
  let shortRuns=0, runs=0;
  for(let x=0;x<grid.width;x++) {
    let last=-1,length=0,first=true;
    for(let y=grid.height-1;y>=0;y--) {
      const source=grid.cells[y*grid.width+x];
      if(source<0) continue;
      const color=map[source];
      counts[color]++;
      if(color!==last) {
        if(length>0) {runs++;if(length<3) shortRuns++;first=false;}
        last=color;length=0;
      }
      length++;
      if(first) opening[color]++;
    }
    if(length>0) {runs++;if(length<3) shortRuns++;}
  }
  let partial=0,exposed=0;
  opening.forEach((count,color)=>{
    if(count<=0) return;
    exposed++;
    if(count<3&&counts[color]>=3) partial++;
  });
  // A heuristic for subject selection, not a proof that every player choice wins.
  return Math.round(1000*(.55*shortRuns/Math.max(1,runs)+.45*partial/Math.max(1,exposed)));
}

module.exports=template=>{
  const profiles=[];
  for(let detail=template.minSize;detail<=50;detail++) {
    const grid=sample(template,detail);
    profiles.push({cells:grid.cells.filter(color=>color>=0).length,colors:grid.colorCount,
      pressure:Array.from({length:6},(_,i)=>pressure(grid,colorMap(grid.cells,i+4)))});
  }
  return profiles;
};
