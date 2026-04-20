namespace HOST.Services;

public class TwilioVoiceSettings
{
    public const string SectionName = "TwilioVoice";

    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromPhoneNumber { get; set; } = string.Empty;
    public string TableReadyMessage { get; set; } = "Thank you for waiting. Please aggressively push your way, to the host stand, and wave your arms about. Your table is ready!";
}
