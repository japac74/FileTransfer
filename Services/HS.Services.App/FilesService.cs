using EnsureThat;
using HS.Domains.App;
using HS.Services.App.Interfaces;
using HS.Services.App.ModelsDto;
using Microsoft.Extensions.Configuration;
using System.Buffers;
using System.Security.Cryptography;
using System.Threading.Channels;

namespace HS.Services.App
{
    public class FilesService : IFilesService
    {
        private readonly IConfiguration _configuration;

        private readonly Channel<FileChunk> _transferChannel;

        private readonly int _chunkSize;

        public FilesService(IConfiguration configuration)
        {
            _configuration = EnsureArg.IsNotNull(configuration, nameof(IConfiguration));


            _transferChannel = Channel.CreateUnbounded<FileChunk>();

            _chunkSize = Convert.ToInt32(_configuration["ChunkSize"]);
        }

        public async Task<bool> CopyFile(FileCopyDto fileCopyDto)
        {

            byte[] buffer = ArrayPool<byte>.Shared.Rent(_chunkSize);

            //TODO to remove for sure the payload
            List<FileChunk> videoChunks = new List<FileChunk>();

            try
            {
                using FileStream fs = new FileStream(fileCopyDto.FullSourcePath, FileMode.Open, FileAccess.Read);

                int bytesRead;
                int chunkIndex = 0;
                int offset = 0;

                while ((bytesRead = fs.Read(buffer, 0, _chunkSize)) > 0)
                {
                    // Chunck read in memory
                    ReadOnlySpan<byte> chunkSpan = new ReadOnlySpan<byte>(buffer, 0, bytesRead);

                    // Create MD5 hash
                    byte[] hashBytes = MD5.HashData(chunkSpan);
                    string md5HashHex = Convert.ToHexString(hashBytes);

                    // Create FileChunk
                    var chunkMessage = new FileChunk(chunkIndex, offset, buffer.ToArray(), md5HashHex);

                    videoChunks.Add(chunkMessage);

                    // Write to channel
                    await _transferChannel.Writer.WriteAsync(chunkMessage);

                    //TODO Update according to requirements
                    Console.WriteLine($"Index: {chunkIndex}, Position: {offset}, Bytes Read: {bytesRead}, Hash: {md5HashHex}");

                    chunkIndex++;
                    offset += bytesRead;
                }

                // Channel completed notification
                _transferChannel.Writer.Complete();





                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message");
            }

            return false;
        }
    }
}
