using System;
using System.IO;
using System.Collections.Generic;

namespace MortalKombatCompiler.API.Compiler
{
    public class Token
    {
        public int kind;    // token kind
        public int pos;     // token position in bytes in the source text (starting at 0)
        public int charPos;  // token position in characters in the source text (starting at 0)
        public int col;     // token column (starting at 1)
        public int line;    // token line (starting at 1)
        public string val;  // token value
        public Token next;  // ML 2005-03-11 Tokens are kept in linked list
    }

    //-----------------------------------------------------------------------------------
    // Buffer
    //-----------------------------------------------------------------------------------
    public class Buffer
    {
        public const int EOF = char.MaxValue + 1;
        const int MIN_BUFFER_LENGTH = 1024; // 1KB
        const int MAX_BUFFER_LENGTH = MIN_BUFFER_LENGTH * 64; // 64KB
        byte[] buf;         // input buffer
        int bufStart;       // position of first byte in buffer relative to input stream
        int bufLen;         // length of buffer
        int fileLen;        // length of input stream (may change if the stream is no file)
        int bufPos;         // current position in buffer
        Stream stream;      // input stream (seekable)
        bool isUserStream;  // was the stream opened by the user?

        public Buffer(Stream s, bool isUserStream)
        {
            stream = s; this.isUserStream = isUserStream;

            if (stream.CanSeek)
            {
                fileLen = (int)stream.Length;
                bufLen = Math.Min(fileLen, MAX_BUFFER_LENGTH);
                bufStart = Int32.MaxValue; // nothing in the buffer so far
            }
            else
            {
                fileLen = bufLen = bufStart = 0;
            }

            buf = new byte[(bufLen > 0) ? bufLen : MIN_BUFFER_LENGTH];
            if (fileLen > 0) Pos = 0; // setup buffer to position 0 (start)
            else bufPos = 0; // index 0 is already after the file, thus Pos = 0 is invalid
            if (bufLen == fileLen && stream.CanSeek) Close();
        }

        protected Buffer(Buffer b)
        {
            buf = b.buf;
            bufStart = b.bufStart;
            bufLen = b.bufLen;
            fileLen = b.fileLen;
            bufPos = b.bufPos;
            stream = b.stream;
            b.stream = null;
            isUserStream = b.isUserStream;
        }

        ~Buffer() { Close(); }

        protected void Close()
        {
            if (!isUserStream && stream != null)
            {
                stream.Close();
                stream = null;
            }
        }

        public virtual int Read()
        {
            if (bufPos < bufLen)
            {
                return buf[bufPos++];
            }
            else if (Pos < fileLen)
            {
                Pos = Pos;
                return buf[bufPos++];
            }
            else if (stream != null && !stream.CanSeek && ReadNextStreamChunk() > 0)
            {
                return buf[bufPos++];
            }
            else
            {
                return EOF;
            }
        }

        public int Peek()
        {
            int curPos = Pos;
            int ch = Read();
            Pos = curPos;
            return ch;
        }

        public string GetString(int beg, int end)
        {
            int len = 0;
            char[] buf = new char[end - beg];
            int oldPos = Pos;
            Pos = beg;
            while (Pos < end) buf[len++] = (char)Read();
            Pos = oldPos;
            return new String(buf, 0, len);
        }

        public int Pos
        {
            get { return bufPos + bufStart; }
            set
            {
                if (value >= fileLen && stream != null && !stream.CanSeek)
                {
                    while (value >= fileLen && ReadNextStreamChunk() > 0) ;
                }

                if (value < 0 || value > fileLen)
                {
                    throw new Exception("buffer out of bounds access, position: " + value);
                }

                if (value >= bufStart && value < bufStart + bufLen)
                {
                    bufPos = value - bufStart;
                }
                else if (stream != null)
                {
                    stream.Seek(value, SeekOrigin.Begin);
                    bufLen = stream.Read(buf, 0, buf.Length);
                    bufStart = value; bufPos = 0;
                }
                else
                {
                    bufPos = fileLen - bufStart;
                }
            }
        }

        private int ReadNextStreamChunk()
        {
            int free = buf.Length - bufLen;
            if (free == 0)
            {
                byte[] newBuf = new byte[bufLen * 2];
                Array.Copy(buf, newBuf, bufLen);
                buf = newBuf;
                free = bufLen;
            }
            int read = stream.Read(buf, bufLen, free);
            if (read > 0)
            {
                fileLen = bufLen = (bufLen + read);
                return read;
            }
            return 0;
        }
    }

    //-----------------------------------------------------------------------------------
    // Scanner
    //-----------------------------------------------------------------------------------
    public class Scanner
    {
        const char EOL = '\n';
        const int eofSym = 0;
        const int maxT = 16;
        const int noSym = 16;

        public Buffer buffer; // scanner buffer
        Token t;          // current token
        int ch;           // current input character
        int pos;          // byte position of current character
        int charPos;      // position by unicode characters starting with 0
        int col;          // column number of current character
        int line;         // line number of current character
        int oldEols;      // EOLs that appeared in a comment;
        static readonly Dictionary<int, int> start; // maps first token character to start state

        Token tokens;     // list of tokens already peeked (first token is a dummy)
        Token pt;         // current peek token

        char[] tval = new char[128]; // text of current token
        int tlen;         // length of current token

        static Scanner()
        {
            start = new Dictionary<int, int>
            {
                [85] = 1,   // U
                [68] = 3,   // D
                [76] = 42,  // L
                [82] = 43,  // R
                [70] = 14,  // F
                [66] = 44,  // B
                [72] = 45,  // H
                [84] = 31,  // T
                [83] = 46,  // S
                [Buffer.EOF] = -1
            };
        }

        public Scanner(string source)
        {
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(source));
            buffer = new Buffer(stream, false);
            Init();
        }

        public Scanner(Stream s)
        {
            buffer = new Buffer(s, true);
            Init();
        }

        void Init()
        {
            pos = -1; line = 1; col = 0; charPos = -1;
            oldEols = 0;
            NextCh();
            if (ch == 0xEF)
            {
                NextCh(); int ch1 = ch;
                NextCh(); int ch2 = ch;
                if (ch1 != 0xBB || ch2 != 0xBF)
                {
                    throw new Exception($"illegal byte order mark: EF {ch1:X2} {ch2:X2}");
                }
                col = 0; charPos = -1;
                NextCh();
            }
            pt = tokens = new Token();
        }

        void NextCh()
        {
            if (oldEols > 0) { ch = EOL; oldEols--; }
            else
            {
                pos = buffer.Pos;
                ch = buffer.Read(); col++; charPos++;
                if (ch == '\r' && buffer.Peek() != '\n') ch = EOL;
                if (ch == EOL) { line++; col = 0; }
            }
        }

        void AddCh()
        {
            if (tlen >= tval.Length)
            {
                char[] newBuf = new char[2 * tval.Length];
                Array.Copy(tval, 0, newBuf, 0, tval.Length);
                tval = newBuf;
            }
            if (ch != Buffer.EOF)
            {
                tval[tlen++] = (char)ch;
                NextCh();
            }
        }

        void CheckLiteral()
        {
            switch (t.val)
            {
                default: break;
            }
        }

        Token NextToken()
        {
            while (ch == ' ' || ch >= 9 && ch <= 10 || ch == 13) NextCh();

            int recKind = noSym;
            int recEnd = pos;
            t = new Token();
            t.pos = pos; t.col = col; t.line = line; t.charPos = charPos;
            int state;
            if (start.ContainsKey(ch)) { state = start[ch]; }
            else { state = 0; }
            tlen = 0; AddCh();

            switch (state)
            {
                case -1: { t.kind = eofSym; break; }
                case 0:
                    {
                        if (recKind != noSym)
                        {
                            tlen = recEnd - t.pos;
                            SetScannerBehindT();
                        }
                        t.kind = recKind; break;
                    }
                case 1:
                    if (ch == 'P') { AddCh(); goto case 2; }
                    else { goto case 0; }
                case 2:
                    { t.kind = 1; break; }
                case 3:
                    if (ch == 'O') { AddCh(); goto case 4; }
                    else { goto case 0; }
                case 4:
                    if (ch == 'W') { AddCh(); goto case 5; }
                    else { goto case 0; }
                case 5:
                    if (ch == 'N') { AddCh(); goto case 6; }
                    else { goto case 0; }
                case 6:
                    { t.kind = 2; break; }
                case 7:
                    if (ch == 'F') { AddCh(); goto case 8; }
                    else { goto case 0; }
                case 8:
                    if (ch == 'T') { AddCh(); goto case 9; }
                    else { goto case 0; }
                case 9:
                    { t.kind = 3; break; }
                case 10:
                    if (ch == 'G') { AddCh(); goto case 11; }
                    else { goto case 0; }
                case 11:
                    if (ch == 'H') { AddCh(); goto case 12; }
                    else { goto case 0; }
                case 12:
                    if (ch == 'T') { AddCh(); goto case 13; }
                    else { goto case 0; }
                case 13:
                    { t.kind = 4; break; }
                case 14:
                    if (ch == 'O') { AddCh(); goto case 15; }
                    else { goto case 0; }
                case 15:
                    if (ch == 'R') { AddCh(); goto case 16; }
                    else { goto case 0; }
                case 16:
                    if (ch == 'W') { AddCh(); goto case 17; }
                    else { goto case 0; }
                case 17:
                    if (ch == 'A') { AddCh(); goto case 18; }
                    else { goto case 0; }
                case 18:
                    if (ch == 'R') { AddCh(); goto case 19; }
                    else { goto case 0; }
                case 19:
                    if (ch == 'D') { AddCh(); goto case 20; }
                    else { goto case 0; }
                case 20:
                    { t.kind = 5; break; }
                case 21:
                    if (ch == 'C') { AddCh(); goto case 22; }
                    else { goto case 0; }
                case 22:
                    if (ch == 'K') { AddCh(); goto case 23; }
                    else { goto case 0; }
                case 23:
                    { t.kind = 6; break; }
                case 24:
                    { t.kind = 7; break; }
                case 25:
                    { t.kind = 8; break; }
                case 26:
                    { t.kind = 9; break; }
                case 27:
                    { t.kind = 10; break; }
                case 28:
                    { t.kind = 11; break; }
                case 29:
                    if (ch == 'N') { AddCh(); goto case 30; }
                    else { goto case 0; }
                case 30:
                    { t.kind = 12; break; }
                case 31:
                    if (ch == ':') { AddCh(); goto case 32; }
                    else { goto case 0; }
                case 32:
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 33; }
                    else { goto case 0; }
                case 33:
                    recEnd = pos; recKind = 13;
                    if (ch >= '0' && ch <= '9') { AddCh(); goto case 33; }
                    else { t.kind = 13; break; }
                case 34:
                    if (ch == 'T') { AddCh(); goto case 35; }
                    else { goto case 0; }
                case 35:
                    if (ch == 'A') { AddCh(); goto case 36; }
                    else { goto case 0; }
                case 36:
                    if (ch == 'R') { AddCh(); goto case 37; }
                    else { goto case 0; }
                case 37:
                    if (ch == 'T') { AddCh(); goto case 38; }
                    else { goto case 0; }
                case 38:
                    { t.kind = 14; break; }
                case 39:
                    if (ch == 'N') { AddCh(); goto case 40; }
                    else { goto case 0; }
                case 40:
                    if (ch == 'D') { AddCh(); goto case 41; }
                    else { goto case 0; }
                case 41:
                    { t.kind = 15; break; }
                case 42:
                    if (ch == 'E') { AddCh(); goto case 7; }
                    else if (ch == 'P') { AddCh(); goto case 24; }
                    else if (ch == 'K') { AddCh(); goto case 26; }
                    else { goto case 0; }
                case 43:
                    if (ch == 'I') { AddCh(); goto case 10; }
                    else if (ch == 'U') { AddCh(); goto case 29; }
                    else { goto case 0; }
                case 44:
                    if (ch == 'A') { AddCh(); goto case 21; }
                    else if (ch == 'L') { AddCh(); goto case 28; }
                    else { goto case 0; }
                case 45:
                    if (ch == 'P') { AddCh(); goto case 25; }
                    else if (ch == 'K') { AddCh(); goto case 27; }
                    else { goto case 0; }
                case 46:
                    if (ch == 'E') { AddCh(); goto case 47; }
                    else { goto case 0; }
                case 47:
                    if (ch == 'Q') { AddCh(); goto case 48; }
                    else { goto case 0; }
                case 48:
                    if (ch == 'U') { AddCh(); goto case 49; }
                    else { goto case 0; }
                case 49:
                    if (ch == 'E') { AddCh(); goto case 50; }
                    else { goto case 0; }
                case 50:
                    if (ch == 'N') { AddCh(); goto case 51; }
                    else { goto case 0; }
                case 51:
                    if (ch == 'C') { AddCh(); goto case 52; }
                    else { goto case 0; }
                case 52:
                    if (ch == 'E') { AddCh(); goto case 53; }
                    else { goto case 0; }
                case 53:
                    if (ch == '_') { AddCh(); goto case 54; }
                    else { goto case 0; }
                case 54:
                    if (ch == 'S') { AddCh(); goto case 34; }
                    else if (ch == 'E') { AddCh(); goto case 39; }
                    else { goto case 0; }
            }
            t.val = new String(tval, 0, tlen);
            return t;
        }

        private void SetScannerBehindT()
        {
            buffer.Pos = t.pos;
            NextCh();
            line = t.line; col = t.col; charPos = t.charPos;
            for (int i = 0; i < tlen; i++) NextCh();
        }

        public Token Scan()
        {
            if (tokens.next == null)
            {
                return NextToken();
            }
            else
            {
                pt = tokens = tokens.next;
                return tokens;
            }
        }

        public Token Peek()
        {
            do
            {
                if (pt.next == null)
                {
                    pt.next = NextToken();
                }
                pt = pt.next;
            } while (pt.kind > maxT);
            return pt;
        }

        public void ResetPeek() { pt = tokens; }
    }
}