namespace HS.Domains.App
{
    public class FileChunk
    {
        //public FileChunk(int chunkIndex, int offset, ReadOnlyMemory<byte> payload, string md5)
        public FileChunk(int chunkIndex, int offset, byte[] payload, string md5)
        {
            ChunkIndex = chunkIndex;
            Offset = offset;
            Payload = payload;
            CheckSumMD5 = md5;
        }

        public int ChunkIndex { get; set; }
        public int Offset { get; set; }
        //public ReadOnlyMemory<byte> Payload { get; set; }
        public byte[] Payload { get; set; }

        public string CheckSumMD5 { get; set; }
    }
}
