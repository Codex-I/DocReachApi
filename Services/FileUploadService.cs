using System.Security.Cryptography;
using System.Text;

namespace DocReachApi.Services
{
    public interface IFileUploadService
    {
        Task<string> UploadDocumentAsync(string base64Content, string fileName, string contentType, string userId, string documentType);
        Task<bool> ValidateDocumentAsync(string filePath, string contentType);
        Task<bool> DeleteDocumentAsync(string filePath);
        Task<byte[]> GetDocumentAsync(string filePath);
        bool IsValidFileType(string contentType, string fileName);
        bool IsValidFileSize(long fileSize);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileUploadService> _logger;
        private readonly string _uploadPath;

        // Allowed file types for healthcare documents
        private readonly string[] _allowedImageTypes = { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        private readonly string[] _allowedDocumentTypes = { "application/pdf", "image/jpeg", "image/jpg", "image/png" };
        private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public FileUploadService(IConfiguration configuration, ILogger<FileUploadService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Documents");
            
            // Ensure upload directory exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<string> UploadDocumentAsync(string base64Content, string fileName, string contentType, string userId, string documentType)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(base64Content) || string.IsNullOrEmpty(fileName))
                {
                    throw new ArgumentException("Invalid file content or name");
                }

                // Validate file type
                if (!IsValidFileType(contentType, fileName))
                {
                    throw new ArgumentException("Invalid file type");
                }

                // Decode base64 content
                var fileBytes = Convert.FromBase64String(base64Content);

                // Validate file size
                if (!IsValidFileSize(fileBytes.Length))
                {
                    throw new ArgumentException("File size exceeds maximum allowed size");
                }

                // Generate secure filename
                var secureFileName = GenerateSecureFileName(fileName, userId, documentType);
                var filePath = Path.Combine(_uploadPath, secureFileName);

                // Write file to disk
                await File.WriteAllBytesAsync(filePath, fileBytes);

                // Validate document integrity
                if (!await ValidateDocumentAsync(filePath, contentType))
                {
                    // Clean up invalid file
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                    throw new ArgumentException("Document validation failed");
                }

                _logger.LogInformation($"Document uploaded successfully: {secureFileName} for user: {userId}");
                return secureFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ValidateDocumentAsync(string filePath, string contentType)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                // Check file size
                var fileInfo = new FileInfo(filePath);
                if (!IsValidFileSize(fileInfo.Length))
                {
                    return false;
                }

                // Check file header (basic validation)
                using var stream = File.OpenRead(filePath);
                var buffer = new byte[8];
                await stream.ReadAsync(buffer, 0, 8);

                // Validate based on content type
                return contentType.ToLower() switch
                {
                    "application/pdf" => ValidatePdfHeader(buffer),
                    "image/jpeg" or "image/jpg" => ValidateJpegHeader(buffer),
                    "image/png" => ValidatePngHeader(buffer),
                    "image/webp" => ValidateWebpHeader(buffer),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating document: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<bool> DeleteDocumentAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_uploadPath, filePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation($"Document deleted: {filePath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document: {FilePath}", filePath);
                return false;
            }
        }

        public async Task<byte[]> GetDocumentAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_uploadPath, filePath);
                if (File.Exists(fullPath))
                {
                    return await File.ReadAllBytesAsync(fullPath);
                }
                throw new FileNotFoundException($"Document not found: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading document: {FilePath}", filePath);
                throw;
            }
        }

        public bool IsValidFileType(string contentType, string fileName)
        {
            // Check content type
            var isValidContentType = _allowedDocumentTypes.Contains(contentType.ToLower());

            // Check file extension
            var extension = Path.GetExtension(fileName).ToLower();
            var isValidExtension = _allowedExtensions.Contains(extension);

            return isValidContentType && isValidExtension;
        }

        public bool IsValidFileSize(long fileSize)
        {
            return fileSize > 0 && fileSize <= MaxFileSize;
        }

        private string GenerateSecureFileName(string originalFileName, string userId, string documentType)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var randomBytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var randomString = Convert.ToHexString(randomBytes).ToLower();
            var extension = Path.GetExtension(originalFileName);
            
            return $"{userId}_{documentType}_{timestamp}_{randomString}{extension}";
        }

        private bool ValidatePdfHeader(byte[] buffer)
        {
            // PDF files start with "%PDF"
            return buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46;
        }

        private bool ValidateJpegHeader(byte[] buffer)
        {
            // JPEG files start with 0xFF 0xD8
            return buffer[0] == 0xFF && buffer[1] == 0xD8;
        }

        private bool ValidatePngHeader(byte[] buffer)
        {
            // PNG files start with 0x89 0x50 0x4E 0x47
            return buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47;
        }

        private bool ValidateWebpHeader(byte[] buffer)
        {
            // WebP files start with "RIFF" followed by "WEBP"
            return buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
                   buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50;
        }
    }
} 