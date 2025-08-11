using Microsoft.EntityFrameworkCore;
using OtpNet;
using QRCoder;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MultiDeptReportingTool.Data;
using MultiDeptReportingTool.DTOs;
using MultiDeptReportingTool.Models;
using MultiDeptReportingTool.Services.Interfaces;

namespace MultiDeptReportingTool.Services
{
    public class MfaService : IMfaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly IAuditService _auditService;
        private readonly ILogger<MfaService> _logger;
        private readonly IConfiguration _configuration;
        private const int BackupCodeCount = 10;
        private const int BackupCodeLength = 8;

        public MfaService(
            ApplicationDbContext context,
            IEncryptionService encryptionService,
            IAuditService auditService,
            ILogger<MfaService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _encryptionService = encryptionService;
            _auditService = auditService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<MfaSetupResponseDto> GenerateMfaSetupAsync(int userId, string? ipAddress = null)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new ArgumentException("User not found");

                if (user.IsMfaEnabled)
                    throw new InvalidOperationException("MFA is already enabled for this user");

                // Generate a new secret
                var secretBytes = new byte[20];
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(secretBytes);
                
                var base32Secret = Base32Encoding.ToString(secretBytes);
                
                // Generate backup codes
                var backupCodes = GenerateBackupCodes();
                
                // Encrypt sensitive data
                var encryptedSecret = await _encryptionService.EncryptAsync(base32Secret);
                var encryptedBackupCodes = await _encryptionService.EncryptAsync(
                    JsonSerializer.Serialize(backupCodes)
                );

                // Update user with setup data (not enabled yet)
                user.MfaSecret = encryptedSecret;
                user.BackupCodes = encryptedBackupCodes;
                await _context.SaveChangesAsync();

                await _auditService.LogSecurityEventAsync(
                    action: "MFA_SETUP_INITIATED",
                    resource: "User",
                    userId: userId,
                    username: user.Username,
                    details: $"MFA setup initiated for user {user.Username}",
                    ipAddress: ipAddress ?? "Unknown"
                );

                return new MfaSetupResponseDto
                {
                    Secret = base32Secret,
                    QrCodeUrl = await GenerateQrCodeAsync(base32Secret, user.Username),
                    BackupCodes = backupCodes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating MFA setup for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> EnableMfaAsync(int userId, string verificationCode)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                if (user.IsMfaEnabled)
                    return false; // Already enabled

                if (string.IsNullOrEmpty(user.MfaSecret))
                    return false; // No setup done

                // Verify the TOTP code
                var decryptedSecret = await _encryptionService.DecryptAsync(user.MfaSecret);
                var secretBytes = Base32Encoding.ToBytes(decryptedSecret);
                var totp = new Totp(secretBytes);

                if (!totp.VerifyTotp(verificationCode, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay))
                {
                    await _auditService.LogSecurityEventAsync(
                        action: "MFA_ENABLE_FAILED",
                        resource: "User",
                        userId: userId,
                        username: user.Username,
                        isSuccess: false,
                        failureReason: "Invalid verification code",
                        details: $"Invalid verification code during MFA enable for user {user.Username}",
                        ipAddress: "Unknown"
                    );
                    return false;
                }

                // Enable MFA
                user.IsMfaEnabled = true;
                user.MfaEnabledAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _auditService.LogSecurityEventAsync(
                    action: "MFA_ENABLED",
                    resource: "User",
                    userId: userId,
                    username: user.Username,
                    details: $"MFA successfully enabled for user {user.Username}",
                    ipAddress: "Unknown"
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling MFA for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DisableMfaAsync(int userId, string currentPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return false;

                if (!user.IsMfaEnabled)
                    return false; // Already disabled

                // Verify current password (add password verification logic here)
                // This would typically involve checking the provided password against the stored hash

                // Disable MFA
                user.IsMfaEnabled = false;
                user.MfaSecret = null;
                user.BackupCodes = null;
                user.MfaEnabledAt = null;
                user.FailedMfaAttempts = 0;
                user.MfaLockedUntil = null;

                await _context.SaveChangesAsync();

                await _auditService.LogSecurityEventAsync(
                    action: "MFA_DISABLED",
                    resource: "User",
                    userId: userId,
                    username: user.Username,
                    details: $"MFA disabled for user {user.Username}",
                    ipAddress: "Unknown"
                );

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling MFA for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> VerifyTotpCodeAsync(int userId, string code)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsMfaEnabled)
                    return false;

                // Check if user is locked out
                if (user.MfaLockedUntil.HasValue && user.MfaLockedUntil > DateTime.UtcNow)
                {
                    await _auditService.LogSecurityEventAsync(
                        action: "MFA_VERIFY_BLOCKED",
                        resource: "User",
                        userId: userId,
                        username: user.Username,
                        isSuccess: false,
                        failureReason: "User locked out",
                        details: $"MFA verification blocked due to lockout for user {user.Username}",
                        ipAddress: "Unknown"
                    );
                    return false;
                }

                var decryptedSecret = await _encryptionService.DecryptAsync(user.MfaSecret);
                var secretBytes = Base32Encoding.ToBytes(decryptedSecret);
                var totp = new Totp(secretBytes);

                var isValid = totp.VerifyTotp(code, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay);

                if (isValid)
                {
                    // Reset failed attempts on successful verification
                    user.FailedMfaAttempts = 0;
                    user.MfaLockedUntil = null;
                    await _context.SaveChangesAsync();

                    await _auditService.LogSecurityEventAsync(
                        action: "MFA_VERIFY_SUCCESS",
                        resource: "User",
                        userId: userId,
                        username: user.Username,
                        details: $"MFA verification successful for user {user.Username}",
                        ipAddress: "Unknown"
                    );
                }
                else
                {
                    // Increment failed attempts
                    user.FailedMfaAttempts++;
                    
                    // Lock account after 5 failed attempts for 15 minutes
                    if (user.FailedMfaAttempts >= 5)
                    {
                        user.MfaLockedUntil = DateTime.UtcNow.AddMinutes(15);
                    }
                    
                    await _context.SaveChangesAsync();

                    await _auditService.LogSecurityEventAsync(
                        action: "MFA_VERIFY_FAILED",
                        resource: "User",
                        userId: userId,
                        username: user.Username,
                        isSuccess: false,
                        failureReason: "Invalid TOTP code",
                        details: $"MFA verification failed for user {user.Username}. Failed attempts: {user.FailedMfaAttempts}",
                        ipAddress: "Unknown"
                    );
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying TOTP code for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> VerifyBackupCodeAsync(int userId, string backupCode)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsMfaEnabled || string.IsNullOrEmpty(user.BackupCodes))
                    return false;

                var decryptedBackupCodes = await _encryptionService.DecryptAsync(user.BackupCodes);
                var backupCodes = JsonSerializer.Deserialize<List<string>>(decryptedBackupCodes);

                if (backupCodes?.Contains(backupCode) == true)
                {
                    // Remove the used backup code
                    backupCodes.Remove(backupCode);
                    
                    // Update the encrypted backup codes
                    user.BackupCodes = await _encryptionService.EncryptAsync(
                        JsonSerializer.Serialize(backupCodes)
                    );
                    
                    // Reset failed attempts
                    user.FailedMfaAttempts = 0;
                    user.MfaLockedUntil = null;
                    
                    await _context.SaveChangesAsync();

                    await _auditService.LogSecurityEventAsync(
                        action: "MFA_BACKUP_CODE_USED",
                        resource: "User",
                        userId: userId,
                        username: user.Username,
                        details: $"Backup code successfully used for user {user.Username}. Remaining codes: {backupCodes.Count}",
                        ipAddress: "Unknown"
                    );

                    return true;
                }

                await _auditService.LogSecurityEventAsync(
                    action: "MFA_BACKUP_CODE_FAILED",
                    resource: "User",
                    userId: userId,
                    username: user.Username,
                    isSuccess: false,
                    failureReason: "Invalid backup code",
                    details: $"Invalid backup code attempted for user {user.Username}",
                    ipAddress: "Unknown"
                );

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying backup code for user {UserId}", userId);
                return false;
            }
        }

        public async Task<List<string>> GenerateNewBackupCodesAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsMfaEnabled)
                    throw new InvalidOperationException("MFA is not enabled for this user");

                var newBackupCodes = GenerateBackupCodes();
                var encryptedBackupCodes = await _encryptionService.EncryptAsync(
                    JsonSerializer.Serialize(newBackupCodes)
                );

                user.BackupCodes = encryptedBackupCodes;
                await _context.SaveChangesAsync();

                await _auditService.LogSecurityEventAsync(
                    action: "MFA_BACKUP_CODES_REGENERATED",
                    resource: "User",
                    userId: userId,
                    username: user.Username,
                    details: $"New backup codes generated for user {user.Username}",
                    ipAddress: "Unknown"
                );

                return newBackupCodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating new backup codes for user {UserId}", userId);
                throw;
            }
        }

        public async Task<string> GenerateQrCodeAsync(string secret, string userEmail, string issuer = "MultiDeptReportingTool")
        {
            return await Task.Run(() =>
            {
                var totpUrl = $"otpauth://totp/{issuer}:{userEmail}?secret={secret}&issuer={issuer}";
                
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(totpUrl, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new Base64QRCode(qrCodeData);
                
                return $"data:image/png;base64,{qrCode.GetGraphic(20)}";
            });
        }

        private List<string> GenerateBackupCodes()
        {
            var codes = new List<string>();
            using var rng = RandomNumberGenerator.Create();
            
            for (int i = 0; i < BackupCodeCount; i++)
            {
                var codeBytes = new byte[BackupCodeLength / 2];
                rng.GetBytes(codeBytes);
                codes.Add(Convert.ToHexString(codeBytes).ToLower());
            }
            
            return codes;
        }
    }
}
