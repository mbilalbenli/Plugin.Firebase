using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.Firebase.Storage;
using Xunit;

namespace Plugin.Firebase.IntegrationTests
{
    public sealed class StorageFixture : IDisposable
    {
        private const string DownloadUrlPrefix = "https://firebasestorage.googleapis.com/v0/b/pluginfirebase-integrationtest.appspot.com/o/";
        private const string DownloadUrlSuffix = "?alt=media&token=";
        
        [Fact]
        public void gets_root_reference()
        {
            var reference = CrossFirebaseStorage.Current.GetRootReference();
            
            Assert.NotNull(reference);
            Assert.Null(reference.Parent);
            Assert.Equal("/", reference.FullPath);
            Assert.Equal("", reference.Name);
            Assert.Equal("pluginfirebase-integrationtest.appspot.com", reference.Bucket);
        }
        
        [Fact]
        public void gets_reference_from_url()
        {
            var reference = CrossFirebaseStorage
                .Current
                .GetReferenceFromUrl("gs://pluginfirebase-integrationtest.appspot.com/files_to_keep/text_1.txt");
            
            Assert.NotNull(reference.Root);
            Assert.NotNull(reference.Parent);
            Assert.Equal("/files_to_keep/text_1.txt", reference.FullPath);
            Assert.Equal("text_1.txt", reference.Name);
            Assert.Equal("pluginfirebase-integrationtest.appspot.com", reference.Bucket);
        }
        
        [Fact]
        public void gets_reference_from_path()
        {
            var reference = CrossFirebaseStorage
                .Current
                .GetReferenceFromPath("files_to_keep/text_1.txt");
            
            Assert.NotNull(reference.Root);
            Assert.NotNull(reference.Parent);
            Assert.Equal("/files_to_keep/text_1.txt", reference.FullPath);
            Assert.Equal("text_1.txt", reference.Name);
            Assert.Equal("pluginfirebase-integrationtest.appspot.com", reference.Bucket);
        }
        
        [Fact]
        public void gets_child_reference()
        {
            var reference = CrossFirebaseStorage
                .Current
                .GetRootReference().GetChild("files_to_keep/text_1.txt");
            
            Assert.NotNull(reference.Root);
            Assert.NotNull(reference.Parent);
            Assert.Equal("/files_to_keep/text_1.txt", reference.FullPath);
            Assert.Equal("text_1.txt", reference.Name);
            Assert.Equal("pluginfirebase-integrationtest.appspot.com", reference.Bucket);
        }

        [Fact]
        public async Task gets_download_url()
        {
            var reference = CrossFirebaseStorage
                .Current
                .GetReferenceFromPath("files_to_keep/text_1.txt");

            var downloadUrl = await reference.GetDownloadUrlAsync();
            Assert.StartsWith(CreateDownloadUrl("files_to_keep/text_1.txt"), downloadUrl);
        }

        private static string CreateDownloadUrl(string pathToFile)
        {
            return $"{DownloadUrlPrefix}{pathToFile}{DownloadUrlSuffix}";
        }

        [Fact]
        public async Task uploads_via_byte_array()
        {
            var path = $"texts/via_bytes.txt";
            var reference = CrossFirebaseStorage
                .Current
                .GetReferenceFromPath(path);

            await reference.PutBytes(Encoding.UTF8.GetBytes("Some test text")).AwaitAsync();
            var downloadUrl = await reference.GetDownloadUrlAsync();
            Assert.StartsWith(CreateDownloadUrl(path), downloadUrl);
        }

        [Fact]
        public async Task uploads_via_stream()
        {
            var path = $"texts/via_stream.txt";
            var reference = CrossFirebaseStorage
                .Current
                .GetReferenceFromPath(path);

            using(var stream = new MemoryStream()) {
                var writer = new StreamWriter(stream);
                await writer.WriteAsync("Some test Text");
                await writer.FlushAsync();
                await reference.PutStream(stream).AwaitAsync();
                var downloadUrl = await reference.GetDownloadUrlAsync();
                Assert.StartsWith(CreateDownloadUrl(path), downloadUrl);
            }
        }

        [Fact]
        public async Task lists_files_with_limit()
        {
            var reference = CrossFirebaseStorage
                .Current
                .GetReferenceFromPath("files_to_keep");

            var result = await reference.ListAsync(2);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task lists_all_files()
        {
            var reference = CrossFirebaseStorage
                .Current
                .GetReferenceFromPath("files_to_keep");

            var result = await reference.ListAllAsync();
            Assert.Equal(3, result.Items.Count());
        }

        [Fact]
        public async Task deletes_file()
        {
            var reference = CrossFirebaseStorage
                .Current
                .GetReferenceFromPath("files_to_delete");
            
            Assert.Empty((await reference.ListAllAsync()).Items);
            await reference.GetChild("text.txt").PutBytes(Encoding.UTF8.GetBytes("This file should get deleted")).AwaitAsync();
            Assert.Single((await reference.ListAllAsync()).Items);

            await reference.GetChild("text.txt").DeleteAsync();
            Assert.Empty((await reference.ListAllAsync()).Items);
        }

        public async void Dispose()
        {
            var rootReference = CrossFirebaseStorage.Current.GetRootReference();
            var filesToDelete = (await rootReference.GetChild("files_to_delete").ListAllAsync()).Items;
            var texts = (await rootReference.GetChild("texts").ListAllAsync()).Items;
            await Task.WhenAll(filesToDelete.Select(TryDeleteAsync).Concat(texts.Select(TryDeleteAsync)));
        }

        private static async Task TryDeleteAsync(IStorageReference reference)
        {
            try {
                await reference.DeleteAsync();
            } catch(Exception e) {
                Console.WriteLine(e);
            }
        }
    }
}