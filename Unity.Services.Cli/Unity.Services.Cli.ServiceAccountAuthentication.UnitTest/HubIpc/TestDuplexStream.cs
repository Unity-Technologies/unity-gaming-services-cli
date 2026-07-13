#if FEATURE_HUB_AUTH
namespace Unity.Services.Cli.Authentication.UnitTest.HubIpc;

class TestDuplexStream : Stream
{
    readonly MemoryStream m_ReadStream;
    readonly MemoryStream m_WriteStream = new();

    public TestDuplexStream(byte[] readData)
    {
        m_ReadStream = new MemoryStream(readData);
    }

    public byte[] WrittenBytes => m_WriteStream.ToArray();

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => m_ReadStream.Length;
    public override long Position
    {
        get => m_ReadStream.Position;
        set => m_ReadStream.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
        => m_ReadStream.Read(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => m_ReadStream.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        => m_ReadStream.ReadAsync(buffer, cancellationToken);

    public override void Write(byte[] buffer, int offset, int count)
        => m_WriteStream.Write(buffer, offset, count);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => m_WriteStream.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => m_WriteStream.WriteAsync(buffer, cancellationToken);

    public override void Flush() => m_WriteStream.Flush();
    public override Task FlushAsync(CancellationToken cancellationToken) => m_WriteStream.FlushAsync(cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
#endif
