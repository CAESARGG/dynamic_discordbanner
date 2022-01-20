using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordJsonBanner
{
    class WebHelper
    {
        /// <summary>
        /// Downloads <see cref="string"/> from specified <paramref name="Url"/>
        /// </summary>
        /// <param name="Url">Download from</param>
        /// <returns>An awaitable task, returns <see cref="string"/></returns>
        public static async Task<string> DownloadStringAsync(string Url)
        {
            using WebClient client = new WebClient();
            string response = await client.DownloadStringTaskAsync(new Uri(Url));
            return response;
        }

        public static async Task<string> UploadFileAsync(Stream stream, string Url)
        {
            stream.Seek(0, SeekOrigin.Begin);
            using var streamContent = new StreamContent(stream);
            using var client = new HttpClient();
            using var formData = new MultipartFormDataContent
            {
                { streamContent, "photo", "photo.jpg" }
            };
            var response = await client.PostAsync(Url, formData).ConfigureAwait(false);
            if (formData != null) formData.Dispose();
            if (client != null) client.Dispose();
            if (streamContent != null) streamContent.Dispose();
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

/*
        public static async Task<string> UploadImageAsync(Image image, string Url, ImageFormat imageFormat, EncoderParameters encoderParameters = null)
        {
            using var memoryStream = GetImageStream(image, imageFormat, encoderParameters);
            return await UploadFileAsync(memoryStream, Url).ConfigureAwait(false);
        }
*/

/*
        public static async Task<string> UploadImageAsync(Image image, string Url, ImageCodecInfo imageCodecInfo, EncoderParameters encoderParameters)
        {
            using var memoryStream = GetImageStream(image, imageCodecInfo, encoderParameters);
            return await UploadFileAsync(memoryStream, Url).ConfigureAwait(false);
        }
*/

/*
        public static Stream GetImageStream(Image image, ImageFormat imageFormat, EncoderParameters encoderParameters = null)
        {
            if (encoderParameters != null) return GetImageStream(image, GetEncoder(imageFormat), encoderParameters);
            MemoryStream memoryStream = new MemoryStream();
            image.Save(memoryStream, imageFormat);
            return memoryStream;
        }
*/

        public static Stream GetImageStream(Image image, ImageCodecInfo imageCodecInfo, EncoderParameters encoderParameters)
        {
            MemoryStream memoryStream = new MemoryStream();
            image.Save(memoryStream, imageCodecInfo, encoderParameters);
            return memoryStream;
        }

        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}