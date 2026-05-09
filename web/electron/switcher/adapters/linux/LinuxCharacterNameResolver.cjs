const fs = require('fs')

class LinuxCharacterNameResolver {
  async resolve(pid) {
    if (pid <= 0) return null

    try {
      const maps = fs.readFileSync(`/proc/${pid}/maps`, 'utf8')
      const regions = this._parseRegions(maps)
      const found = []

      const fd = fs.openSync(`/proc/${pid}/mem`, 'r')
      try {
        for (const { start, end } of regions) {
          const size = Number(end - start)
          if (size <= 0) continue
          const CHUNK = 4 * 1024 * 1024
          let offset = 0
          while (offset < size) {
            const chunkSize = Math.min(CHUNK, size - offset)
            const buf = Buffer.alloc(chunkSize)
            try {
              fs.readSync(fd, buf, 0, chunkSize, Number(start) + offset)
              this._extract(buf, found)
            } catch { }
            offset += CHUNK
          }
        }
      } finally {
        fs.closeSync(fd)
      }

      if (!found.length) return null
      found.sort((a, b) => b.timestamp.localeCompare(a.timestamp))
      return found[0].name
    } catch (e) {
      return null
    }
  }

  _parseRegions(maps) {
    const regions = []
    for (const line of maps.split('\n')) {
      const parts = line.split(/\s+/)
      if (!parts[1] || !parts[1].includes('r')) continue
      const [startHex, endHex] = parts[0].split('-')
      if (!startHex || !endHex || startHex === '' || endHex === '') continue
      regions.push({
        start: BigInt('0x' + startHex),
        end: BigInt('0x' + endHex),
      })
    }
    return regions
  }

  _extract(buf, found) {
    for (let i = 0; i < buf.length - 30; i++) {
      if (buf[i] !== 0x0A) continue

      const lenByte = buf[i + 1]
      if (lenByte < 0x02 || lenByte > 0x20) continue

      const nameLen = lenByte
      const nameStart = i + 2
      const nameEnd = nameStart + nameLen

      if (nameEnd >= buf.length) continue
      if (!this._isValidName(buf, nameStart, nameLen)) continue

      const searchEnd = Math.min(nameEnd + 15, buf.length - 22)
      for (let j = nameEnd; j < searchEnd; j++) {
        if (buf[j] === 0x2A && buf[j + 1] === 0x1D) {
          const tsStart = j + 2
          const tsEnd = tsStart + 19
          if (tsEnd > buf.length) break

          const ts = buf.slice(tsStart, tsEnd).toString('ascii')
          if (/^20\d\d-\d\d-\d\dT\d\d:\d\d:\d\d$/.test(ts)) {
            const name = buf.slice(nameStart, nameEnd).toString('utf8')
            found.push({ timestamp: ts, name })
          }
          break
        }
      }
    }
  }

  _isValidName(buf, start, len) {
    if (len < 2 || len > 20) return false
    const first = buf[start]
    if (!((first >= 65 && first <= 90) || (first >= 97 && first <= 122))) return false
    for (let i = start + 1; i < start + len; i++) {
      const c = buf[i]
      if (!((c >= 65 && c <= 90) || (c >= 97 && c <= 122) || (c >= 48 && c <= 57) || c === 45)) {
        return false
      }
    }
    return true
  }
}

module.exports = { LinuxCharacterNameResolver }