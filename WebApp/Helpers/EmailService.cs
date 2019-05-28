using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace WebApp.Helpers
{
    public class EmailService
    {
        public string Destination { get; set; }
        public string PlainTextContent { get; set; }
        public string HtmlContent { get; set; }
        public string Subject { get; set; }

        private static string _apiKey = Constants.SendGridApiKey;
        private SendGridClient _client = new SendGridClient(_apiKey);
        private SendGridMessage _message = new SendGridMessage();
        private EmailAddress _from = new EmailAddress(Constants.DoNotReply, Constants.WetMyPlantsTeamName); // default is DoNotReply@wetmyplants.com

        public EmailService() {}
        public EmailService(string destination, string plainTextMessage, string subject = null, string htmlMessage = null)
        {
            Destination = destination;
            PlainTextContent = plainTextMessage;
            HtmlContent = htmlMessage;
            Subject = subject;
        }

        // default email address is DoNotReply@wetmyplants.com, but this can be overridden explicitly
        public void OverrideFrom(string email, string name = null)
        {
            if(!IsValidEmail(email))
                throw new ArgumentException("Email address is invalid");

            _from = new EmailAddress(email, name);
        }

        public async Task<Response> Send()
        {
            // check if destination email address is valid
            if (!IsValidEmail(Destination))
            {
                throw new ArgumentException("Destination email is invalid");
            }

            // check that there is message content
            if(string.IsNullOrWhiteSpace(PlainTextContent) && string.IsNullOrWhiteSpace(HtmlContent))
                throw new ArgumentException("Must contain either plaintext or HTML content");

            // if everything passes, create and send the message
            _message.From = _from;
            _message.Subject = Subject;
            _message.PlainTextContent = PlainTextContent;
            _message.HtmlContent = HtmlContent;
            _message.AddTo(Destination);

            var response = await _client.SendEmailAsync(_message);
            return response;
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                new MailAddress(email);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}