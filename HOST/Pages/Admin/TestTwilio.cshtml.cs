using HOST.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace HOST.Pages.Admin
{
    [Authorize(Roles = "Manager")]
    public class TestTwilioModel : PageModel
    {
        private readonly TwilioVoiceCallService _twilioService;
        private readonly ILogger<TestTwilioModel> _logger;
        private readonly TwilioVoiceSettings _settings;

        public string ResultMessage { get; set; }
        public bool IsSuccess { get; set; }

        public ConfigStatusDto ConfigStatus { get; set; } = new();

        public TestTwilioModel(
            TwilioVoiceCallService twilioService,
            ILogger<TestTwilioModel> logger,
            IOptions<TwilioVoiceSettings> settings)
        {
            _twilioService = twilioService;
            _logger = logger;
            _settings = settings.Value;
        }

        public void OnGet()
        {
            LoadConfigStatus();
        }

        public async Task OnPostAsync(string phoneNumber, string partyName)
        {
            LoadConfigStatus();

            _logger.LogInformation("🧪 TEST: Calling Twilio with Phone={Phone}, Party={Party}", 
                phoneNumber, partyName);

            var result = await _twilioService.PlaceTableReadyCallAsync(
                phoneNumber,
                partyName,
                CancellationToken.None);

            IsSuccess = result.Succeeded;
            ResultMessage = result.Succeeded 
                ? $"✅ Call successfully placed to {phoneNumber}!" 
                : $"❌ {result.ErrorMessage}";

            _logger.LogInformation("🧪 TEST RESULT: Success={Success}, Message={Message}", 
                IsSuccess, ResultMessage);
        }

        private void LoadConfigStatus()
        {
            ConfigStatus = new ConfigStatusDto
            {
                AccountSid = string.IsNullOrEmpty(_settings.AccountSid) ? null : "SET",
                AuthToken = string.IsNullOrEmpty(_settings.AuthToken) ? null : "SET",
                FromPhone = _settings.FromPhoneNumber
            };
        }

        public class ConfigStatusDto
        {
            public string AccountSid { get; set; }
            public string AuthToken { get; set; }
            public string FromPhone { get; set; }
        }
    }
}
