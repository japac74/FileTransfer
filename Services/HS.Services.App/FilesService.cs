using EnsureThat;
using HS.Domains.App;
using HS.Services.App.Interfaces;
using HS.Services.App.ModelsDto;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace HS.Services.App
{
    public class FilesService : IFilesService
    {
        private readonly IConfiguration _configuration;

        private ConcurrentDictionary<long, bool> _cb;
        private readonly int _chunkSize;

        public FilesService(IConfiguration configuration)
        {
            _configuration = EnsureArg.IsNotNull(configuration, nameof(IConfiguration));

            _cb = new ConcurrentDictionary<long, bool>();
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
                Console.WriteLine("Starting to copy, please wait...\n");

                // Define unbounded channel for file chunks
                Channel<FileChunk> _transferChannel = Channel.CreateUnbounded<FileChunk>();

                // Read file
                Task producerTask = Task.Run(() => ProduceChunkAsync(fileCopyDto, _transferChannel.Writer));

                // Save file via channel
                Task consumerTask = Task.Run(() => ConsumeChunksAsync(fileCopyDto, _transferChannel.Reader, _transferChannel.Writer));

                // Wait for both producer and consumer to complete
                await Task.WhenAll(producerTask, consumerTask);
                
                Console.WriteLine("\nFile copied successfully.\n");
                
                
                //COMPUTE SHA256 HASHES OF SOURCE AND TARGET FILES TO VERIFY INTEGRITY
                string sourceSHA = Encrypt.ComputeSHA256(fileCopyDto.FullSourcePath);
                Console.WriteLine($"Source SHA256: {sourceSHA}");

                string targetSHA = Encrypt.ComputeSHA256(fileCopyDto.FullTargetPath);
                Console.WriteLine($"Target SHA256: {targetSHA}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                //_transferChannel.Writer.TryComplete(ex);
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
                await using FileStream stream = new FileStream(fileCopyDto.FullSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: _chunkSize, useAsync: true);

                int chunkIndex = 0;
                long offset = 0;
                var buffer = new byte[_chunkSize];

                while (true)
                {
                    // Allocate array
                    int bytesRead = await stream.ReadAsync(buffer, 0, _chunkSize);

                    if (bytesRead == 0) break;

                    // Copy only the bytes that were read
                    byte[] chunkData = new byte[bytesRead];
                    Array.Copy(buffer, 0, chunkData, 0, bytesRead);

                    // Create MD5 hash
                    string md5HashHex = Encrypt.ComputeMD5(chunkData);

                    var chunkMessage = new FileChunk(chunkIndex, offset, chunkData, md5HashHex);

                    Console.WriteLine($"Index: {chunkIndex}, Position: {offset}, Bytes Read: {bytesRead}, Hash: {md5HashHex}");
                    _cb.TryAdd(offset, false);

                    // Write to channel
                    await writer.WriteAsync(chunkMessage);

                    chunkIndex++;
                    offset += bytesRead;
                }

                // Wait for the chuncks to be successfully saved, prior to completing the channel.
                // This handles retries, in case MD5 HASH does not match in the consumer, and the chunk is resubmitted to the channel.
                // TODO Find better alternative to complete the channel, for retries
                do { } while (_cb.Count > 0);

                // Complete the cxhannel to signal the consumer that no more data will be written.
                // This allows the consumer to finish processing and exit gracefully.
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
        private async Task ConsumeChunksAsync(FileCopyDto fileCopyDto, ChannelReader<FileChunk> reader, ChannelWriter<FileChunk> writer)
        {
            try
            {
                await using FileStream outputStream = new FileStream(fileCopyDto.FullTargetPath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: _chunkSize, useAsync: true);
                                
                while (await reader.WaitToReadAsync())
                {
                    if (reader.TryRead(out var chunk))
                    {                   
                        // Create MD5 hash
                        string md5HashHex = Encrypt.ComputeMD5(chunk.Payload);

                        // Save the chunk data
                        if (md5HashHex == chunk.CheckSumMD5)
                        {
                            //Move the stream position to the correct offset and write the chunk data
                            //outputStream.Seek(chunk.Offset, SeekOrigin.Begin);
                            //await outputStream.WriteAsync(chunk.Payload, 0, chunk.Payload.Length);
                            //_cb.TryRemove(chunk.Offset, out bool success);

                            // NOTE - The following block, simulates random corruption of chunks, to test the retry mechanism. 
                            // Unremark this block of code lines 175 - 190 to randomly corrupt chunks.  Remark the previous code - lines 167 - 170.
                            // It is set as 50% failure rate
                            int x = Random.Shared.Next(0, 2);

                            if (x == 0)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"Chunk {chunk.ChunkIndex} corrupted. Resubmitting...");
                                Console.ResetColor();
                                await writer.WriteAsync(chunk);
                            }
                            else
                            {
                                // Move the stream position to the correct offset and write the chunk data
                                outputStream.Seek(chunk.Offset, SeekOrigin.Begin);
                                await outputStream.WriteAsync(chunk.Payload, 0, chunk.Payload.Length);
                                _cb.TryRemove(chunk.Offset, out bool success);
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Chunk {chunk.ChunkIndex} corrupted. Resubmitting...");
                            Console.ResetColor();

                            await writer.WriteAsync(chunk);
                        }
                    }
                }

                writer.TryComplete();
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
