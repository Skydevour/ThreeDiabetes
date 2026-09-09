const colors = {
  red: '#F5384D', orange: '#FC7D14', yellow: '#FFBF0D', green: '#1AB861',
  cyan: '#08B3E6', blue: '#1F5CE0', purple: '#8038D1', pink: '#F23394',
  cream: '#FFEEAD', brown: '#753832', white: '#FEFFFF', sky: '#7CC4FF'
};
const c = value => colors[value] || value;
const stroke = (color, width) => color ? ` stroke="${c(color)}" stroke-width="${width}" stroke-linejoin="round" stroke-linecap="round"` : '';
const path = (d, fill, edge, width = 1.5) => `<path d="${d}" fill="${fill ? c(fill) : 'none'}"${stroke(edge, width)}/>`;
const rect = (x, y, w, h, fill, radius = 0, edge) => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${radius}" fill="${c(fill)}"${stroke(edge, 1.5)}/>`;
const ellipse = (x, y, rx, ry, fill, edge) => `<ellipse cx="${x}" cy="${y}" rx="${rx}" ry="${ry}" fill="${c(fill)}"${stroke(edge, 1.5)}/>`;
const line = (x1, y1, x2, y2, color, width = 2) => path(`M${x1} ${y1}L${x2} ${y2}`, null, color, width);
const group = (transform, ...parts) => `<g transform="${transform}">${parts.flat().join('')}</g>`;
const clip = (id, silhouette, ...parts) => `<defs><clipPath id="${id}">${silhouette}</clipPath></defs><g clip-path="url(#${id})">${parts.flat().join('')}</g>`;
const leaf = (x, y, angle = 0, color = 'green', size = 1) => group(`translate(${x} ${y}) rotate(${angle}) scale(${size})`,
  path('M0 0Q-12 -2 -14 -16Q0 -17 0 0', color), path('M0 0L-10 -12', null, 'cream', 1));
const star = (x, y, radius, color = 'yellow', points = 5, inner = 0.48) => {
  let d = '';
  for (let i = 0; i < points * 2; i++) {
    const angle = i * Math.PI / points - Math.PI / 2;
    const r = radius * (i % 2 ? inner : 1);
    d += `${i ? 'L' : 'M'}${(x + Math.cos(angle) * r).toFixed(2)} ${(y + Math.sin(angle) * r).toFixed(2)}`;
  }
  return path(d + 'Z', color);
};
const eyes = (x, y, gap = 11, radius = 3) => [
  ellipse(x - gap / 2, y, radius, radius + 1, 'white'), ellipse(x + gap / 2, y, radius, radius + 1, 'white'),
  ellipse(x - gap / 2 + 0.5, y + 0.5, radius * 0.5, radius * 0.65, 'brown'),
  ellipse(x + gap / 2 + 0.5, y + 0.5, radius * 0.5, radius * 0.65, 'brown')
].join('');
const pot = (color = 'orange') => [path('M19 44H45L42 59H22Z', color), rect(17, 41, 30, 6, 'brown', 2),
  rect(23, 48, 5, 8, 'cream', 1), line(31, 53, 40, 53, 'yellow', 2)].join('');
const stem = (x = 32, top = 26) => [line(x, 55, x, top, 'green', 4), leaf(x, 45, -15, 'green', 0.8), leaf(x, 38, 85, 'cyan', 0.7)].join('');
const wheel = (x, y, r = 6) => [ellipse(x, y, r, r, 'brown'), ellipse(x, y, r * 0.58, r * 0.58, 'sky'), ellipse(x, y, r * 0.2, r * 0.2, 'white')].join('');
const seeds = (points, color = 'brown') => points.map(([x, y]) => ellipse(x, y, 1.2, 2.2, color)).join('');
const ribs = (x, y, count, step, height, color = 'cream') => Array.from({length: count}, (_, i) => line(x + i * step, y, x + i * step, y + height, color, 1)).join('');
const bow = (x, y, color = 'pink') => [path(`M${x} ${y}Q${x-14} ${y-12} ${x-11} ${y+7}Z`, color),
  path(`M${x} ${y}Q${x+14} ${y-12} ${x+11} ${y+7}Z`, color), ellipse(x,y,3,3,'yellow')].join('');
module.exports = {colors, path, rect, ellipse, line, group, clip, leaf, star, eyes, pot, stem, wheel, seeds, ribs, bow};
