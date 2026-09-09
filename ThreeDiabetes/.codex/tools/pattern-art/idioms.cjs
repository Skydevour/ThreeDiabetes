const {colors} = require('./shapes.cjs');
const captions = [
  ['idiom','心想事成'], ['goodfortune','吉星高照'], ['smoothsailing','一帆风顺'],
  ['springflowers','春暖花开'], ['brightfuture','前程似锦'], ['happylife','安居乐业'],
  ['perseverance','持之以恒'], ['contentment','知足常乐'], ['singingbirds','鸟语花香'],
  ['fullmoon','花好月圆']
];
const ink = ['red','blue','green','purple'];
module.exports = captions.map(([id,title]) => ({
  id, title, category:'idioms', minSize:44,
  art:Array.from(title).map((character,i) =>
    `<text x="${i%2*32+2}" y="${Math.floor(i/2)*32+28}" font-family="SimHei" font-size="29" fill="${colors[ink[i]]}">${character}</text>`
  ).join('')
}));
