namespace SimpleKeybindProxy.Models.SocketResponse
{
    public class SocketResponseChunk
    {
        public int Id { get; set; }
        public int TotalChunks { get; set; }
        public string ChunkValue { get; set; }
    }
}
