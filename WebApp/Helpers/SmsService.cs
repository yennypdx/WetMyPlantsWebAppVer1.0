using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace WebApp.Helpers
{
    public static class SmsService
    {
        public static void SendSms(string destinationNumber, string message)
        {
            var accountId = Constants.TwilioAccountId;
            var authenticationToken = Constants.TwilioAuthenticationToken;

            TwilioClient.Init(accountId, authenticationToken);

            var smsMessage = MessageResource.Create(
            body: message,
            from: new Twilio.Types.PhoneNumber(Constants.TwilioPhoneNumber),
            to: new Twilio.Types.PhoneNumber($"+1{destinationNumber}")
            );
        }
    }
}