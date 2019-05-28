using System;
using System.Net;
using DbHelper;
using Models;
using System.Web.Mvc;
using Twilio.Rest.Api.V2010.Account;
using Twilio;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Http;
using System.Collections.Generic;
using WebApp.Helpers;

namespace WebApp.Controllers
{
    [RoutePrefix("piapi")]
    public class PiAPIController : Controller
    {
        private readonly IDbHelper _db;

        //private JsonResult Jsonify(string content) => Json($"{{ content: '{content}' }}");
        // BadRequest takes a string or JSON object and returns it along with a 500 (BadRequest) status code
        //private ActionResult BadRequest(string content) => BadRequest(Jsonify(content));
        //private ActionResult BadRequest(JsonResult content) =>
          //  new HttpStatusCodeResult(HttpStatusCode.BadRequest, content.Data.ToString());

        // Ok takes a string or JSON object and returns it along with a 200 (OK) status code
        //private ActionResult Ok(string content) => Ok(Jsonify(content));
        //private ActionResult Ok(JsonResult content) =>
          //    new HttpStatusCodeResult(HttpStatusCode.OK, content.Data.ToString());

        //external pi requirements include requests, os, apscheduler
        // CTOR receives the DbHelper through Dependency Injection
        public PiAPIController(IDbHelper db) => _db = db;

        // GET: PiAPI
        //piapi
        public String Index()
        {
            return "I'm feeling Plant-Tastic!";
        }
        
        //POST: piapi/updateplant
        //ID,Water,Light
        [HttpPost]
        public void updateplant(Plant plant)
        {
            Plant currentPlant = _db.FindPlant(plant.Id);
            double previousLightVariable = plant.CurrentLight;
            currentPlant.CurrentLight = plant.CurrentLight;
            currentPlant.CurrentWater = plant.CurrentWater;
            currentPlant.UpdateTime = (int)DateTime.Now.TimeOfDay.TotalHours;
                   

            var result = _db.UpdatePlant(currentPlant);
                        
            if (result == true)
                HandleData(currentPlant, previousLightVariable);
           
        }
        
        public void HandleData(Plant plant, double previousLightVariable)
        {
            User currentUser = _db.FindPlantUser(plant.Id);
            Plant currentPlant = _db.FindPlant(plant.Id);
            Species currentSpecies = _db.FindSpecies(plant.SpeciesId);            
            Dictionary<string, bool> userPreferences = _db.GetNotificationPreferences(currentUser.Id);
            int lightTrackerTemp = currentPlant.LightTracker;

            if (currentPlant.CurrentLight > currentSpecies.LightMax || currentPlant.CurrentLight < currentSpecies.LightMax)
            {
                if (currentPlant.CurrentLight == previousLightVariable && currentPlant.UpdateTime > 7 && currentPlant.UpdateTime < 20)
                {
                    currentPlant.LightTracker = lightTrackerTemp + 1;
                    var result = _db.UpdatePlant(currentPlant);

                    if (result != true)
                    {
                        throw new Exception("Error in updating lightTracker, PiAPIController, Line 78");
                    }
                }
            }
                
            if (currentPlant.CurrentWater > currentSpecies.WaterMax)
            {
                if (userPreferences["Email"].Equals(true))
                {
                    
                    string subject = "High Water";
                    string message = "Hello " + currentUser.FirstName + "! " + _db.GetNotificationResponseMessage(ResponseTypes.HighWater) + "\n ~" + currentPlant.Nickname;
                    SendEmail(currentUser.Email, currentPlant.Nickname, subject, message).Wait();
                }
                if (userPreferences["Phone"].Equals(true))
                {
                   
                    string message = _db.GetNotificationResponseMessage(ResponseTypes.HighWater) + " ~" + currentPlant.Nickname;
                    SendSMS(currentUser.Phone, message);
                }
            }
            if (currentPlant.CurrentWater < currentSpecies.WaterMin)
            {
                if (userPreferences["Email"].Equals(true))
                {
                    
                    string subject = "Low Water";
                    string message = "Hello " + currentUser.FirstName + "! " + _db.GetNotificationResponseMessage(ResponseTypes.LowWater) + "\n ~" + currentPlant.Nickname;
                    SendEmail(currentUser.Email, currentPlant.Nickname, subject, message).Wait();
                }
                if (userPreferences["Phone"].Equals(true))
                {
                    
                    string message = _db.GetNotificationResponseMessage(ResponseTypes.LowWater) + " ~" + currentPlant.Nickname;
                    SendSMS(currentUser.Phone, message);
                }
            }

            if (currentPlant.CurrentLight > currentSpecies.LightMax)
            {
                if (currentPlant.LightTracker >= 3)
                {
                    if (userPreferences["Email"].Equals(true))
                    {

                        string subject = "High Light";
                        string message = "Hello " + currentUser.FirstName + "! " + _db.GetNotificationResponseMessage(ResponseTypes.HighLight) + "\n ~" + currentPlant.Nickname;
                        SendEmail(currentUser.Email, currentPlant.Nickname, subject, message).Wait();
                    }
                    if (userPreferences["Phone"].Equals(true))
                    {

                        string message = _db.GetNotificationResponseMessage(ResponseTypes.HighLight) + " ~" + currentPlant.Nickname;
                        SendSMS(currentUser.Phone, message);
                    }
                }
            }

            if (currentPlant.CurrentLight < currentSpecies.LightMin)
            {
                if (currentPlant.LightTracker >= 3)
                {
                    if (userPreferences["Email"].Equals(true))
                    {

                        string subject = "Low Light";
                        string message = "Hello " + currentUser.FirstName + "! " + _db.GetNotificationResponseMessage(ResponseTypes.LowLight) + "\n ~" + currentPlant.Nickname;
                        SendEmail(currentUser.Email, currentPlant.Nickname, subject, message).Wait();
                    }
                    if (userPreferences["Phone"].Equals(true))
                    {

                        string message = _db.GetNotificationResponseMessage(ResponseTypes.LowLight) + " ~" + currentPlant.Nickname;
                        SendSMS(currentUser.Phone, message);
                    }
                }
            }
        }

        public void SendSMS(string userPhone, string msgbody)
        {
            /*
            //const string accountSid = "AC3dfa39c6c58dba42c4867c99fb626324";
            var accountSid = Constants.TwilioAccountId;
            var authToken = Constants.TwilioAuthenticationToken;
            //const string authToken = "cab21f1579fd511e71c56bc45fcc2dbc";
            */
            //string completeNumber = "+1" + userPhone;

            /*
            TwilioClient.Init(accountSid, authToken);

            var message = MessageResource.Create(
            body: msgbody,
            //from: new Twilio.Types.PhoneNumber("+19713184244"),
            from: new Twilio.Types.PhoneNumber(Constants.TwilioPhoneNumber),
            to: new Twilio.Types.PhoneNumber(completeNumber)
            );
            */

            SmsService.SendSms($"+1{userPhone}", msgbody);
        }
        static public async Task SendEmail(string email, string plantName, string msgSubject, string msgcontent)
        {
            /*
            //string apiKey = "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";//System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
            var apiKey = Constants.SendGridApiKey;
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress( plantName + "@wetmyplants.com", plantName),
                Subject = msgSubject,
                PlainTextContent = msgcontent//,
               // HtmlContent = "<strong>Please click on this link to reset your password: </strong><a href=\"" + urlString + "\" > wetmyplants.azurewebsites.net/Account/ResetPassword</a>"
            };
            msg.AddTo(new EmailAddress(email, "user"));
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
            */

            var emailMessage = new EmailService
            {
                Destination = email,
                Subject = msgSubject,
                PlainTextContent = msgcontent,
                HtmlContent = $"<strong>{msgcontent}</strong>"
            };

            emailMessage.OverrideFrom($"{plantName}@wetmyplants.com", plantName);

            emailMessage.Send();
        }
    }
}