using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaundryServies.Common
{

    /// <summary>
    /// عقد الخدمة بتاعة رفع الملفات
    /// ممكن تتطبق على Cloudinary أو Azure Blob Storage
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// بترفع صورة وبترجع الـ URL بتاعها
        /// </summary>
        Task<string> UploadImageAsync(IFormFile file, string folder);

        /// <summary>
        /// بترفع ملف PDF وبترجع الـ URL بتاعه
        /// </summary>
        Task<string> UploadPdfAsync(IFormFile file, string folder);

        /// <summary>
        /// بتمسح صورة من الـ Cloud بالـ PublicId بتاعها
        /// </summary>
        Task DeleteImageAsync(string publicId);
    }
}
