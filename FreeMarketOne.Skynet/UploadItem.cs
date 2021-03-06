using Microsoft.Extensions.FileProviders;
using System;
using System.Net.Http.Headers;

namespace FreeMarketOne.Skynet
{
    /// <summary>
    /// Represents an item that is to be uploaded to Skynet
    /// </summary>
    public class UploadItem
    {
        /// <summary>
        /// File to upload
        /// </summary>
        public IFileInfo FileInfo { get; }

        /// <summary>
        /// The desired Skynet file path, which if unspecified will be set to the file name
        /// </summary>
        public string SkynetPath { get; }

        /// <summary>
        /// MIME type of the file, which if not specified will be automatically mapped
        /// </summary>
        public MediaTypeHeaderValue ContentType { get; }

        /// <summary>
        /// Creates an upload item, ready to upload to Skynet
        /// </summary>
        /// <param name="fileInfo">File to upload</param>
        /// <param name="skynetPath">The desired Skynet file path, which if unspecified will be set to the file name</param>
        /// <param name="contentType">MIME type of the file, which if not specified will be automatically mapped</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileInfo"/> is a null reference</exception>
        /// <exception cref="ArgumentException"><paramref name="fileInfo"/> represents a directory or is of type <see cref="NotFoundFileInfo"/></exception>
        public UploadItem(IFileInfo fileInfo, string skynetPath = null, MediaTypeHeaderValue contentType = null)
        {
            if (fileInfo is null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }

            if (fileInfo.IsDirectory)
            {
                throw new ArgumentException("FileInfo must not represent a directory", nameof(fileInfo));
            }

            if (fileInfo is NotFoundFileInfo)
            {
                throw new ArgumentException("Can not upload non-existant file", nameof(fileInfo.Name));
            }

            FileInfo = fileInfo;
            SkynetPath = skynetPath ?? fileInfo.Name;
            ContentType = contentType ?? MediaTypeHeaderValue.Parse(MimeTypes.GetMimeType(fileInfo.Name));
        }
    }
}
