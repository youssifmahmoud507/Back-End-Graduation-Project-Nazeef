using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServies.Common
{

    /// <summary>
    /// بترفع الصور على Cloudinary
    /// لو بعدين عايز تنقل لـ Azure
    /// بس تعمل AzureBlobService بنفس الـ Interface
    /// </summary>
    public class CloudinaryService : IFileService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudName =
                configuration["Cloudinary:CloudName"] ??
                configuration["Authentication:Cloudinary:CloudName"];
            var apiKey =
                configuration["Cloudinary:ApiKey"] ??
                configuration["Authentication:Cloudinary:ApiKey"];
            var apiSecret =
                configuration["Cloudinary:ApiSecret"] ??
                configuration["Authentication:Cloudinary:ApiSecret"];

            if (string.IsNullOrWhiteSpace(cloudName) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(apiSecret))
            {
                throw new InvalidOperationException(
                    "Cloudinary configuration is missing. Set Cloudinary:CloudName, Cloudinary:ApiKey, and Cloudinary:ApiSecret.");
            }

            // بنجيب الـ Credentials من الـ appsettings.json
            var account = new Account(
                cloudName,
                apiKey,
                apiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            // بنتأكد إن الملف موجود ومش فاضي
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                // بنحدد الفولدر على Cloudinary
                Folder = $"nadeef/{folder}",

                // بنحول الصورة تلقائي لـ WebP عشان تبقى أخف
                Format = "webp",

                // بنحدد أقصى حجم للصورة
                Transformation = new Transformation()
                    .Width(800)
                    .Height(600)
                    .Crop("limit")
                    .Quality("auto"),

                File = new FileDescription(file.FileName, stream)
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            // لو في Error في الرفع
            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");

            // بنرجع الـ URL بتاع الصورة
            return uploadResult.SecureUrl.ToString();
        }

        public async Task<string> UploadPdfAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                Folder = $"nadeef/{folder}",
                File = new FileDescription(file.FileName, stream),
                PublicId = $"{folder}/{Guid.NewGuid()}"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");

            return uploadResult.SecureUrl.ToString();
        }

        public async Task DeleteImageAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deleteParams);
        }
    }
}
