const logger = require('../logger/LoggerService.cjs');
const { TYPE_PREFIX, readVarint } = require('./AnkamaMessages.cjs');

const SVC = 'StreamReassembler';

const MAX_STREAM_BUFFER = 4 * 1024 * 1024;
const MAX_MESSAGE_SIZE = 2 * 1024 * 1024;

// Recolle le flux serveur → client de chaque connexion et en extrait les messages complets
// (type.ankama.com/<nom> 12 <len> <valeur>). Un message peut s'étaler sur des dizaines de
// segments TCP : les prix moyens font ~70 Ko.
class StreamReassembler {
    // onMessage(name, value, stream)
    constructor(onMessage) {
        this.onMessage = onMessage;
        this.streams = new Map();
    }

    reset() {
        this.streams.clear();
    }

    push({ stream, fromServer, seq, data }) {
        if (!fromServer) return;

        let s = this.streams.get(stream);
        if (!s) {
            s = { buf: Buffer.alloc(0), nextSeq: null };
            this.streams.set(stream, s);
        }

        if (s.nextSeq !== null) {
            if (seq + data.length <= s.nextSeq) return; // retransmission complète
            if (seq < s.nextSeq) {
                data = data.subarray(s.nextSeq - seq);   // retransmission partielle
            } else if (seq > s.nextSeq) {
                s.buf = Buffer.alloc(0);                 // trou : le message en cours est perdu
            }
        }

        s.nextSeq = Math.max(s.nextSeq ?? 0, seq + data.length);
        s.buf = s.buf.length ? Buffer.concat([s.buf, data]) : Buffer.from(data);

        if (s.buf.length > MAX_STREAM_BUFFER) {
            logger.warn(SVC, `Flux ${stream} : tampon saturé, réinitialisé`);
            s.buf = Buffer.alloc(0);
            return;
        }

        this._drain(s, stream);
    }

    _drain(s, stream) {
        let buf = s.buf;
        for (;;) {
            const start = buf.indexOf(TYPE_PREFIX);
            if (start === -1) {
                buf = buf.subarray(Math.max(0, buf.length - TYPE_PREFIX.length));
                break;
            }

            let i = start + TYPE_PREFIX.length;
            while (i < buf.length && buf[i] >= 0x61 && buf[i] <= 0x7a) i++;
            if (i + 1 >= buf.length) { buf = buf.subarray(start); break; }

            const name = buf.subarray(start + TYPE_PREFIX.length, i).toString();
            if (buf[i] !== 0x12 || name.length === 0) { buf = buf.subarray(start + 1); continue; }

            const len = readVarint(buf, i + 1);
            if (!len) { buf = buf.subarray(start); break; }
            if (len.value > MAX_MESSAGE_SIZE) { buf = buf.subarray(start + 1); continue; }

            const end = len.next + len.value;
            if (end > buf.length) { buf = buf.subarray(start); break; }

            this.onMessage(name, buf.subarray(len.next, end), stream);
            buf = buf.subarray(end);
        }
        s.buf = Buffer.from(buf);
    }
}

module.exports = { StreamReassembler };
