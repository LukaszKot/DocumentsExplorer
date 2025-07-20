using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Blobs;

namespace DocumentExplorer.Infrastructure.BlobStorage
{
    public class BlobStorageContext
    {
        private readonly BlobStorageSettings _blobStorageSettings;

        public BlobStorageContext(BlobStorageSettings blobStorageSettings)
        {
            _blobStorageSettings = blobStorageSettings;
        }

        public async Task UploadAsync(string blobName, string filePath)
        {
            var blockBlob = await GetBlockBlobAsync(blobName);

            await using (var fileStream = File.Open(filePath, FileMode.Open))
            {
                fileStream.Position = 0;
                await blockBlob.UploadAsync(fileStream);
            }
            File.Delete(filePath);
        }

        public async Task<MemoryStream> DownloadAsync(string blobName)
        {
            var blockBlob = await GetBlockBlobAsync(blobName);

            var stream = new MemoryStream();
            var result = await blockBlob.DownloadStreamingAsync();
            await result.Value.Content.CopyToAsync(stream);
            return stream;
        }

        public async Task DeleteAsync(string blobName)
        {
            var blockBlob = await GetBlockBlobAsync(blobName);
            await blockBlob.DeleteIfExistsAsync();
        }


        public async Task UpdateFileNames(IEnumerable<string> from, IEnumerable<string> to)
        {
            using(var e1 = from.GetEnumerator())
            using(var e2 = to.GetEnumerator())
            {
                while(e1.MoveNext() && e2.MoveNext())
                {
                    await UpdateBlobName(e2.Current,e1.Current);
                }
            }
        }

        private async Task UpdateBlobName(string newBlobName, string oldBlobName)
        {
            var blockCopy = await GetBlockBlobAsync(newBlobName);
            if (!await blockCopy.ExistsAsync())  
            {  
                var blob = await GetBlockBlobAsync(oldBlobName);

                if (await blob.ExistsAsync())  
                {  
                    await blockCopy.StartCopyFromUriAsync(blob.Uri);  
                    await blob.DeleteIfExistsAsync();  
                } 
            } 

        }

        private async Task<BlobContainerClient> GetContainerAsync()
        {
            var blobServiceClient =
                new BlobServiceClient(new Uri($"https://{_blobStorageSettings.StorageAccount}.blob.core.windows.net"),
                    new DefaultAzureCredential());

            var blobContainer = blobServiceClient.GetBlobContainerClient(_blobStorageSettings.ContainerName);
            await blobContainer.CreateIfNotExistsAsync();

            return blobContainer;
        }

        private async Task<BlobClient> GetBlockBlobAsync(string blobName)
        {
            var blobContainer = await GetContainerAsync();

            var blockBlob = blobContainer.GetBlobClient(blobName);

            return blockBlob;
        }
    }
}
