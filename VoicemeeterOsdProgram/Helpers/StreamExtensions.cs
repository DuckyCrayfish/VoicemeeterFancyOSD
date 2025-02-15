﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AtgDev.Utils.StreamExtensions
{
    public static class StreamExtension
    {
        public static async Task CopyToAsync(this Stream input, Stream output,
            int bufferSize, IProgress<long> progress = null,
            CancellationToken cancellationToken = default)
        {
            var buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await input.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }
        }

        public static async Task CopyToAsync(this Stream input, Stream output,
            IProgress<long> progress = null,
            CancellationToken cancellationToken = default)
        {
            await CopyToAsync(input, output, 4096, progress, cancellationToken);
        }
    }
}
