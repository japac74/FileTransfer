using EnsureThat;
using HS.Domains.App;
using HS.Services.App.Interfaces;
using HS.Services.App.ModelsDto;
using Microsoft.Extensions.Configuration;
using System.Buffers;
using System.Collections.Concurrent;
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
            try
            {

                // Read file
                var producerTask = ProduceChunkAsync(fileCopyDto, _transferChannel.Writer);

                // Save file via channel
                var consumerTask = ConsumeChunksAsync(_transferChannel.Reader);

                Console.WriteLine("File copy completed.");

                //TODO Need to take care of treads to Wait
                Console.ReadLine();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return true;
        }


        /// <summary>
        /// Read file and push to channel
        /// </summary>
        /// <param name="fileCopyDto">file chunk object</param>
        /// <param name="writer">Channel writer</param>
        /// <returns></returns>
        private bool ProduceChunkAsync(FileCopyDto fileCopyDto, ChannelWriter<FileChunk> writer)
        {
            // Run the file reading on a ThreadPool thread.
            Task.Run(async () =>
            {
                try
                {
                    List<FileChunk> videoChunks = new List<FileChunk>();            

                    int chunkIndex = 0;
                    int offset = 0;

                    await using var stream = new FileStream(fileCopyDto.FullSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _chunkSize, useAsync: true);

                    while (true)
                    {                        
                        var buffer = new byte[_chunkSize];
                        int bytesRead = await stream.ReadAsync(buffer, 0, _chunkSize);

                        // Create MD5 hash
                        byte[] hashBytes = MD5.HashData(buffer);
                        string md5HashHex = Convert.ToHexString(hashBytes);

                        if (bytesRead == 0) break; // End of file (EOF)

                        //If the last chunk is smaller than the requested size, truncate the array
                        if (bytesRead < _chunkSize)
                        {
                            Array.Resize(ref buffer, bytesRead);                            
                        }

                        // Create FileChunk
                        //var chunkMessage = new FileChunk(chunkIndex, offset, buffer.Take(bytesRead).ToArray(), md5HashHex);
                        var chunkMessage = new FileChunk(chunkIndex, offset, buffer, md5HashHex);


                        videoChunks.Add(chunkMessage);


                        // Write to channel
                        await writer.WriteAsync(chunkMessage);

                        //TODO Update according to requirements
                        Console.WriteLine($"{bytesRead} - Index: {chunkIndex}, Position: {offset}, Bytes Read: {bytesRead}, Hash: md5HashHex");

                        chunkIndex++;
                        offset += bytesRead;
                    }

                    writer.Complete();

                    //return true;
                }
                catch (Exception ex)
                {
                    // Send the error to the channel consumer before closing it out
                    //await writer.WriteAsync(new FileChunk(Array.Empty<byte>(), ex));
                    //writer.Complete(ex);
                }
            });

            return true;
        }


     
        private async Task ConsumeChunksAsync(ChannelReader<FileChunk> reader)
        {
            int chunkCounter = 0;

            // Open the destination stream for writing
            await using var outputStream = new FileStream(
                "C:\\SoftwareDevelopment\\HornetSecurity\\target\\Source.mp4",
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: _chunkSize,
                useAsync: true
            );

            await foreach (var chunk in reader.ReadAllAsync())
            {

                
                // Save the chunk data immediately to disk
                await outputStream.WriteAsync(chunk.Payload, 0, chunk.Payload.Length);

                //await Task.Delay(1000);
            }
        }
    }
}
