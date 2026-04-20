using System.Net.Http.Headers;
using System.Security;
using System.Text;
using Microsoft.Extensions.Options;

namespace HOST.Services;

public class TwilioVoiceCallService
{
    private readonly HttpClient _httpClient;
    private readonly TwilioVoiceSettings _settings;
    private readonly ILogger<TwilioVoiceCallService> _logger;

    public TwilioVoiceCallService(
        HttpClient httpClient,
        IOptions<TwilioVoiceSettings> settings,
        ILogger<TwilioVoiceCallService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<TwilioVoiceCallResult> PlaceTableReadyCallAsync(
        string? rawPhoneNumber,
        string? partyName,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            return TwilioVoiceCallResult.Failure("Twilio voice settings are missing.");
        }

        var toPhoneNumber = NormalizeUsPhoneNumber(rawPhoneNumber);
        if (toPhoneNumber is null)
        {
            return TwilioVoiceCallResult.Failure("Party phone number is not a valid US phone number.");
        }

        var fromPhoneNumber = NormalizeUsPhoneNumber(_settings.FromPhoneNumber);
        if (fromPhoneNumber is null)
        {
            return TwilioVoiceCallResult.Failure("Configured Twilio voice number is invalid.");
        }

        var endpoint = $"https://api.twilio.com/2010-04-01/Accounts/{_settings.AccountSid}/Calls.json";
        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_settings.AccountSid}:{_settings.AuthToken}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = toPhoneNumber,
            ["From"] = fromPhoneNumber,
            ["Twiml"] = BuildTwiml(BuildMessage(partyName))
        });

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Placed Twilio table-ready call to {PhoneNumber}.", toPhoneNumber);
                return TwilioVoiceCallResult.Success();
            }

            _logger.LogWarning(
                "Twilio voice call failed with status {StatusCode}: {ResponseBody}",
                (int)response.StatusCode,
                responseBody);

            return TwilioVoiceCallResult.Failure("Twilio rejected the outbound call request.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place Twilio voice call.");
            return TwilioVoiceCallResult.Failure("An error occurred while contacting Twilio.");
        }
    }

    private bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_settings.AccountSid)
            && !string.IsNullOrWhiteSpace(_settings.AuthToken)
            && !string.IsNullOrWhiteSpace(_settings.FromPhoneNumber);
    }

    private string BuildMessage(string? partyName)
    {
        var cleanedName = string.IsNullOrWhiteSpace(partyName)
            ? "guest"
            : partyName.Trim();

        return $"Hello {cleanedName}, {_settings.TableReadyMessage}";
    }

    private static string BuildTwiml(string message)
    {
        var safeMessage = SecurityElement.Escape(message) ?? "Hello guest, your seat is ready";
        return $"<Response><Pause length=\"2\" /><Say>{safeMessage}</Say></Response>";
    }

    private static string? NormalizeUsPhoneNumber(string? rawPhoneNumber)
    {
        if (string.IsNullOrWhiteSpace(rawPhoneNumber))
        {
            return null;
        }

        var digits = new string(rawPhoneNumber.Where(char.IsDigit).ToArray());

        if (digits.Length == 10)
        {
            return $"+1{digits}";
        }

        if (digits.Length == 11 && digits.StartsWith('1'))
        {
            return $"+{digits}";
        }

        if (rawPhoneNumber.StartsWith('+') && digits.Length >= 11)
        {
            return $"+{digits}";
        }

        return null;
    }
}

public record TwilioVoiceCallResult(bool Succeeded, string? ErrorMessage)
{
    public static TwilioVoiceCallResult Success() => new(true, null);

    public static TwilioVoiceCallResult Failure(string errorMessage) => new(false, errorMessage);
}
