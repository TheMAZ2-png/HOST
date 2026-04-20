namespace HOST.Services;

public class TwilioVoiceSettings
{
    public const string SectionName = "TwilioVoice";

    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromPhoneNumber { get; set; } = string.Empty;
    public string TableReadyMessage { get; set; } = "thank you for waiting, your table is available. Enjoy your food!";
}
