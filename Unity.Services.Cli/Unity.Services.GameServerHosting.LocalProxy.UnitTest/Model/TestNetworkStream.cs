using System;
using System.IO;
using System.Threading;

namespace Unity.Services.GameServerHosting.LocalProxy.UnitTest.Model
{
    public class CreatePairResult
    {
        public CreatePairResult(
            TestNetworkStream clientStream,
            TestNetworkStream serverStream
        )
        {
            ClientStream = clientStream;
            ServerStream = serverStream;
        }

        public TestNetworkStream ClientStream { get; private set; }
        public TestNetworkStream ServerStream { get; private set; }
    }

    public class TestNetworkStream : Stream
    {
        bool m_Closed;

        readonly Stream m_InboundStream;
        readonly Stream m_OutboundStream;
        readonly Mutex m_InboundMutex;
        readonly Mutex m_OutboundMutex;

        TestNetworkStream(
            Stream inboundStream,
            Stream outboundStream,
            Mutex inboundMutex,
            Mutex outboundMutex
            )
        {
            m_InboundStream = inboundStream;
            m_OutboundStream = outboundStream;
            m_InboundMutex = inboundMutex;
            m_OutboundMutex = outboundMutex;
        }

        public override void Flush()
        {
            m_OutboundStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            while (true)
            {
                // This is required because a read on a NetworkInterface is usually blocking when no new data is available.
                // This sleep gives us some room to actually write data to the stream.
                Thread.Sleep(50);

                try
                {
                    m_InboundMutex.WaitOne();
                    // TODO: It would be nice to cleanup the memory usage here, but for tests it's not a big deal.
                    var read = m_InboundStream.Read(buffer, offset, count);

                    var isEmpty = read == 0;
                    if (!isEmpty)
                    {
                        return read;
                    }
                }
                finally
                {
                    m_InboundMutex.ReleaseMutex();
                }

            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException("Seek is not supported");
        }

        public override void SetLength(long value)
        {
            // Not supported
            throw new NotImplementedException("SetLength is not supported");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                m_OutboundMutex.WaitOne();

                // Writing to a MemoryStream sets the Position. By resetting it after writing, we allow the other pair
                // to actually read the data.
                var pos = m_OutboundStream.Position;
                m_OutboundStream.Write(buffer, offset, count);
                m_OutboundStream.Position = pos;
            }
            finally
            {
                m_OutboundMutex.ReleaseMutex();
            }
        }

        public override void Close()
        {
            if (m_Closed) return;

            base.Close();
            m_Closed = true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            m_InboundStream.Dispose();
            m_InboundMutex.Dispose();
            m_OutboundStream.Dispose();
            m_OutboundMutex.Dispose();
        }

        public override bool CanRead => !m_Closed;
        public override bool CanSeek => false;
        public override bool CanWrite => !m_Closed;
        public override long Length => m_InboundStream.Length;
        public override long Position
        {
            get => m_InboundStream.Position;
            set => throw new NotImplementedException("Position is not implemented");
        }

        public static CreatePairResult CreatePair()
        {
            var clientOutStream = new MemoryStream(512 * 1024);
            var serverOutStream = new MemoryStream(512 * 1024);
            var clientOutMutex = new Mutex();
            var serverOutMutex = new Mutex();

            var clientStream = new TestNetworkStream(serverOutStream, clientOutStream, serverOutMutex, clientOutMutex);
            var serverStream = new TestNetworkStream(clientOutStream, serverOutStream, clientOutMutex, serverOutMutex);

            return new CreatePairResult(clientStream, serverStream);
        }
    }
}
