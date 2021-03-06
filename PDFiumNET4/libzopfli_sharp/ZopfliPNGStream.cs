using System.IO;

namespace LibZopfliSharp
{
    public class ZopfliPNGStream : Stream
    {
        private Stream _innerStream;
        private bool _CanWrite = true;
        private ZopfliPNGOptions options;
        private bool leaveOpen;

        public ZopfliPNGStream(Stream inner)
        {
            _innerStream = inner;
            this.options = new ZopfliPNGOptions();
            this.leaveOpen = false;
        }

        public ZopfliPNGStream(Stream inner, bool leaveOpen)
        {
            _innerStream = inner;
            this.options = new ZopfliPNGOptions();
            this.leaveOpen = leaveOpen;
        }

        public ZopfliPNGStream(Stream inner, ZopfliPNGOptions options)
        {
            _innerStream = inner;
            this.options = options;
            this.leaveOpen = false;
        }

        public ZopfliPNGStream(Stream inner, ZopfliPNGOptions options, bool leaveOpen)
        {
            _innerStream = inner;
            this.options = options;
            this.leaveOpen = leaveOpen;
        }

        public override bool CanRead
        {
            get { return _innerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _innerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _CanWrite; }
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override long Length
        {
            get { return _innerStream.Length; }
        }

        public override long Position
        {
            get { return _innerStream.Position; }
            set { _innerStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_CanWrite)
            {
                byte[] data = ZopfliPNG.compress(buffer, options);
                _innerStream.Write(data, offset, data.Length);
                _CanWrite = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!leaveOpen)
                _innerStream.Dispose();
            options = null;
        }
    }
}