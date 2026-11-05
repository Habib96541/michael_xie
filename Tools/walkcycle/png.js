// Minimal PNG codec: 8-bit non-interlaced PNGs, colour types 0/2/4/6 -> RGBA8.
// Only what the walk-cycle rig needs; no palette or 16-bit support.
const zlib = require('zlib');

const SIGNATURE = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);

const CRC_TABLE = (() => {
  const table = new Int32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    table[n] = c;
  }
  return table;
})();

function crc32(buf) {
  let c = 0xffffffff;
  for (let i = 0; i < buf.length; i++) c = CRC_TABLE[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
}

function paeth(a, b, c) {
  const p = a + b - c;
  const pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
  if (pa <= pb && pa <= pc) return a;
  return pb <= pc ? b : c;
}

function unfilter(raw, stride, height, bpp) {
  const out = Buffer.alloc(stride * height);
  let pos = 0;
  for (let y = 0; y < height; y++) {
    const filter = raw[pos++];
    const row = y * stride;
    const prev = row - stride;
    for (let x = 0; x < stride; x++) {
      const value = raw[pos + x];
      const a = x >= bpp ? out[row + x - bpp] : 0;
      const b = y > 0 ? out[prev + x] : 0;
      const c = x >= bpp && y > 0 ? out[prev + x - bpp] : 0;
      let recon;
      switch (filter) {
        case 0: recon = value; break;
        case 1: recon = value + a; break;
        case 2: recon = value + b; break;
        case 3: recon = value + ((a + b) >> 1); break;
        case 4: recon = value + paeth(a, b, c); break;
        default: throw new Error(`unsupported PNG filter ${filter} on row ${y}`);
      }
      out[row + x] = recon & 0xff;
    }
    pos += stride;
  }
  return out;
}

/** Decode a PNG file into { width, height, data } with RGBA8 pixels. */
function decode(buffer) {
  if (!buffer.subarray(0, 8).equals(SIGNATURE)) throw new Error('not a PNG file');
  let offset = 8;
  let header = null;
  let palette = null;
  let paletteAlpha = null;
  const idat = [];
  while (offset < buffer.length) {
    const length = buffer.readUInt32BE(offset);
    const type = buffer.toString('ascii', offset + 4, offset + 8);
    const body = buffer.subarray(offset + 8, offset + 8 + length);
    offset += length + 12;
    if (type === 'IHDR') {
      header = {
        width: body.readUInt32BE(0),
        height: body.readUInt32BE(4),
        bitDepth: body[8],
        colorType: body[9],
        interlace: body[12],
      };
    } else if (type === 'PLTE') {
      palette = body;
    } else if (type === 'tRNS') {
      paletteAlpha = body;
    } else if (type === 'IDAT') {
      idat.push(body);
    } else if (type === 'IEND') {
      break;
    }
  }
  if (!header) throw new Error('PNG has no IHDR chunk');
  if (header.interlace !== 0) throw new Error('interlaced PNGs are not supported');
  if (header.bitDepth === 16) throw new Error('16-bit PNGs are not supported');

  const channels = { 0: 1, 2: 3, 3: 1, 4: 2, 6: 4 }[header.colorType];
  if (!channels) throw new Error(`unsupported PNG colour type ${header.colorType}`);
  if (header.colorType === 3 && !palette) throw new Error('palette PNG has no PLTE chunk');

  const bitsPerPixel = channels * header.bitDepth;
  const stride = Math.ceil((header.width * bitsPerPixel) / 8);
  const raw = zlib.inflateSync(Buffer.concat(idat));
  const pixels = unfilter(raw, stride, header.height, Math.max(1, Math.ceil(bitsPerPixel / 8)));
  const data = new Uint8Array(header.width * header.height * 4);

  // Sample reader for sub-byte depths (palette/greyscale PNGs are often 1/2/4-bit).
  const readSample = (row, x) => {
    if (header.bitDepth === 8) return pixels[row + x];
    const bit = x * header.bitDepth;
    const byte = pixels[row + (bit >> 3)];
    const shift = 8 - header.bitDepth - (bit & 7);
    return (byte >> shift) & ((1 << header.bitDepth) - 1);
  };

  if (header.colorType === 3) {
    const entries = palette.length / 3;
    for (let y = 0; y < header.height; y++) {
      const row = y * stride;
      for (let x = 0; x < header.width; x++) {
        const index = readSample(row, x);
        const p = (y * header.width + x) * 4;
        if (index >= entries) continue;
        data[p] = palette[index * 3];
        data[p + 1] = palette[index * 3 + 1];
        data[p + 2] = palette[index * 3 + 2];
        data[p + 3] = paletteAlpha && index < paletteAlpha.length ? paletteAlpha[index] : 255;
      }
    }
    return { width: header.width, height: header.height, data };
  }

  for (let y = 0; y < header.height; y++) {
    const row = y * stride;
    for (let x = 0; x < header.width; x++) {
      const p = (y * header.width + x) * 4;
      if (channels === 1) {
        const v = readSample(row, x);
        data[p] = data[p + 1] = data[p + 2] = v;
        data[p + 3] = 255;
      } else if (channels === 2) {
        const v = readSample(row, x * 2);
        data[p] = data[p + 1] = data[p + 2] = v;
        data[p + 3] = readSample(row, x * 2 + 1);
      } else {
        data[p] = readSample(row, x * channels);
        data[p + 1] = readSample(row, x * channels + 1);
        data[p + 2] = readSample(row, x * channels + 2);
        data[p + 3] = channels === 4 ? readSample(row, x * channels + 3) : 255;
      }
    }
  }
  return { width: header.width, height: header.height, data };
}

function chunk(type, body) {
  const head = Buffer.alloc(8);
  head.writeUInt32BE(body.length, 0);
  head.write(type, 4, 'ascii');
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(Buffer.concat([head.subarray(4), body])), 0);
  return Buffer.concat([head, body, crc]);
}

/** Encode RGBA8 pixels into a PNG buffer using the "up" filter for every scanline. */
function encode({ width, height, data }) {
  const stride = width * 4;
  const raw = Buffer.alloc((stride + 1) * height);
  for (let y = 0; y < height; y++) {
    const rowStart = y * (stride + 1);
    raw[rowStart] = 2; // filter: up
    for (let x = 0; x < stride; x++) {
      const value = data[y * stride + x];
      const above = y > 0 ? data[(y - 1) * stride + x] : 0;
      raw[rowStart + 1 + x] = (value - above) & 0xff;
    }
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;   // bit depth
  ihdr[9] = 6;   // colour type: RGBA
  ihdr[10] = 0;  // deflate
  ihdr[11] = 0;  // adaptive filtering
  ihdr[12] = 0;  // no interlace
  return Buffer.concat([
    SIGNATURE,
    chunk('IHDR', ihdr),
    chunk('IDAT', zlib.deflateSync(raw, { level: 9 })),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

/** Create a blank RGBA canvas. */
function canvas(width, height) {
  return { width, height, data: new Uint8Array(width * height * 4) };
}

function copyRegion(src, rect) {
  const out = canvas(rect.width, rect.height);
  for (let y = 0; y < rect.height; y++) {
    for (let x = 0; x < rect.width; x++) {
      const srcX = rect.x + x, srcY = rect.y + y;
      if (srcX < 0 || srcY < 0 || srcX >= src.width || srcY >= src.height) continue;
      const from = (srcY * src.width + srcX) * 4;
      const to = (y * rect.width + x) * 4;
      out.data[to] = src.data[from];
      out.data[to + 1] = src.data[from + 1];
      out.data[to + 2] = src.data[from + 2];
      out.data[to + 3] = src.data[from + 3];
    }
  }
  return out;
}

function getPixel(img, x, y) {
  const px = Math.round(x), py = Math.round(y);
  if (px < 0 || py < 0 || px >= img.width || py >= img.height) return [0, 0, 0, 0];
  const i = (py * img.width + px) * 4;
  return [img.data[i], img.data[i + 1], img.data[i + 2], img.data[i + 3]];
}

function setPixel(img, x, y, rgba) {
  if (x < 0 || y < 0 || x >= img.width || y >= img.height) return;
  const i = (y * img.width + x) * 4;
  img.data[i] = rgba[0]; img.data[i + 1] = rgba[1];
  img.data[i + 2] = rgba[2]; img.data[i + 3] = rgba[3];
}

/** Alpha-blend a pixel over the canvas (nearest sampled). */
function blendPixel(img, x, y, rgba) {
  if (rgba[3] === 0) return;
  if (x < 0 || y < 0 || x >= img.width || y >= img.height) return;
  const i = (y * img.width + x) * 4;
  const sa = rgba[3] / 255;
  const da = img.data[i + 3] / 255;
  const outA = sa + da * (1 - sa);
  if (outA <= 0) return;
  for (let c = 0; c < 3; c++) {
    img.data[i + c] = Math.round((rgba[c] * sa + img.data[i + c] * da * (1 - sa)) / outA);
  }
  img.data[i + 3] = Math.round(outA * 255);
}

module.exports = { decode, encode, canvas, copyRegion, getPixel, setPixel, blendPixel };
