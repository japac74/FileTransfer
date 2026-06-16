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

            // Channel options
            var options = new BoundedChannelOptions(capacity: 5)
            {
                SingleWriter = true,
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait 
            };

            _transferChannel = Channel.CreateBounded<FileChunk>(options);

            _chunkSize = Convert.ToInt32(_configuration["ChunkSize"]);
        }

        /// <summary>
        /// Copy file
        /// </summary>
        /// <param name="fileCopyDto">file copy object</param>
        /// <returns></returns>
        public async Task<bool> CopyFile(FileCopyDto fileCopyDto)
        {         
            try
            {
                Console.WriteLine("Starting to copy, please wait...");

                // Read file
                Task producerTask = ProduceChunkAsync(fileCopyDto, _transferChannel.Writer);

                // Save file via channel
                Task consumerTask = ConsumeChunksAsync(fileCopyDto, _transferChannel.Reader);

                // Wait for both producer and consumer to complete - PROBLEM
                //await Task.WhenAll(producerTask, consumerTask);


                Console.WriteLine("File copied successfully.");

                //COMPUTE SHA256 HASHES OF SOURCE AND TARGET FILES TO VERIFY INTEGRITY
                string sourceSHA = Encrypt.ComputeSHA256(fileCopyDto.FullSourcePath);
                //string targetSHA = Encrypt.ComputeSHA256(fileCopyDto.FullTargetPath);

                Console.WriteLine($"Source SHA256: {sourceSHA}");
                //Console.WriteLine($"Target SHA256: {targetSHA}");

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
        private async Task ProduceChunkAsync(FileCopyDto fileCopyDto, ChannelWriter<FileChunk> writer)
        {
            try
            {
                FileStream stream = new FileStream(fileCopyDto.FullSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _chunkSize, useAsync: true);

                int chunkIndex = 0;
                long offset = 0;

                while (true)
                {
                    // Allocate array
                    var buffer = new byte[_chunkSize];
                    int bytesRead = await stream.ReadAsync(buffer, 0, _chunkSize);

                    if (bytesRead == 0) break;

                    // Last chunk, resize array
                    if (bytesRead < _chunkSize)
                    {
                        Array.Resize(ref buffer, bytesRead);
                    }

                    // Create MD5 hash
                    string md5HashHex =Encrypt.ComputeMD5(buffer);

                    var chunkMessage = new FileChunk(chunkIndex, offset, buffer.Take(bytesRead).ToArray(), md5HashHex);

                    Console.WriteLine($"Index: {chunkIndex}, Position: {offset}, Bytes Read: {bytesRead}, Hash: {md5HashHex}");

                    // Write to channel
                    await writer.WriteAsync(chunkMessage);

                    chunkIndex++;
                    offset += bytesRead;
                }

                writer.Complete();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"Producer Exception: {ex.Message}");
                
                writer.TryComplete(ex);

                Console.ResetColor();
            }
        }



        /// <summary>
        /// Save file by reading from channel
        /// </summary>
        /// <param name="fileCopyDto">file chunk object</param>>
        /// <param name="reader">channel reader</param>
        /// <returns></returns>
        private async Task ConsumeChunksAsync(FileCopyDto fileCopyDto, ChannelReader<FileChunk> reader)
        {
            try
            {
                await using var outputStream = new FileStream(fileCopyDto.FullTargetPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: _chunkSize, useAsync: true);

                await foreach (var chunk in reader.ReadAllAsync())
                {
                    // Create MD5 hash
                    string md5HashHex = Encrypt.ComputeMD5(chunk.Payload);

                    // Save the chunk data
                    if (md5HashHex == chunk.CheckSumMD5)
                        await outputStream.WriteAsync(chunk.Payload, 0, chunk.Payload.Length);
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;

                        Console.WriteLine($"File corrupted.");

                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"Consumer Exception: {ex.Message}");

                Console.ResetColor();
                throw; // Must rethrow to fail the Task.WhenAll bundle                
            }
        }
    }
}
