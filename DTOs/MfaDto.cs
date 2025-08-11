using System.ComponentModel.DataAnnotations;

namespace MultiDeptReportingTool.DTOs
{
    public class EnableMfaDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        public string? Secret { get; set; } // Optional for manual entry
    }

    public class EnableMfaRequestDto
    {
        [Required]
        public string VerificationCode { get; set; } = string.Empty;
    }

    public class DisableMfaRequestDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required]
        public string MfaCode { get; set; } = string.Empty;
    }

    public class VerifyMfaRequestDto
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        
        public bool IsBackupCode { get; set; } = false;
    }

    public class VerifyMfaDto
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Code { get; set; } = string.Empty;
        
        public bool RememberDevice { get; set; } = false;
    }

    public class MfaSetupResponseDto
    {
        public string Secret { get; set; } = string.Empty;
        public string QrCodeUrl { get; set; } = string.Empty;
        public List<string> BackupCodes { get; set; } = new List<string>();
    }

    public class DisableMfaDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
        
        [Required]
        public string MfaCode { get; set; } = string.Empty;
    }

    public class BackupCodeDto
    {
        [Required]
        public string BackupCode { get; set; } = string.Empty;
        
        [Required]
        public string Username { get; set; } = string.Empty;
    }

    public class MfaStatusDto
    {
        public bool IsEnabled { get; set; }
        public DateTime? SetupAt { get; set; }
        public int BackupCodesRemaining { get; set; }
        public bool IsCurrentlyRequired { get; set; }
        public DateTime? LastUsed { get; set; }
    }
}
