using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
            //const string authToken = "cab21f1579fd511e71c56bc45fcc2dbc";
            string completeNumber = "+1" + destinationNumber;

            TwilioClient.Init(accountId, authenticationToken);

            var smsMessage = MessageResource.Create(
            body: message,
            //from: new Twilio.Types.PhoneNumber("+19713184244"),
            from: new Twilio.Types.PhoneNumber(Constants.TwilioPhoneNumber),
            to: new Twilio.Types.PhoneNumber(completeNumber)
            );
        }
    }
}