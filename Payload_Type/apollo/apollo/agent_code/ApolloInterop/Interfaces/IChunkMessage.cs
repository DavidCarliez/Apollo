namespace AgInterop.Interfaces
{
    public interface IChunkMessage
    {
        int GetChunkNumber();
        int GetTotalChunks();
        int GetChunkSize();
    }
}
