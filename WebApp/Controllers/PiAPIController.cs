using DbHelper;
using Models;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using WebApp.Helpers;

namespace WebApp.Controllers
{
    [RoutePrefix("piapi")]
    public class PiAPIController : Controller
    {
        private readonly IDbHelper _db;

        //external pi requirements include requests, os, apscheduler
        // CTOR receives the DbHelper through Dependency Injection
        public PiAPIController(IDbHelper db) => _db = db;

        // GET: PiAPI
        //piapi
        public string Index()
        {
            return "I'm feeling Plant-Tastic!";
        }

        //POST: piapi/checkHubRegStatus
        //email, id
        [HttpPost]
        public void checkHubRegStatus(string email, string address)
        {
            var user = _db.FindUser(email: email);
            if(user == null)
            {
                return;
            }
            //verify or register Hub
            var hub = _db.GetHub(address);

            if(hub == null)
            {
                var result = _db.CreateHub(new Hub { Address = address, UserId = user.Id, CurrentPower = 98 });
            }

        }
        //GET: piapi/getuserplants >> Return list of plant ids
        // userEmail, piMAC Address
        [HttpGet]
        public ActionResult GetUserPlants(string email, string hubAddress)
        {
            //verify and find user
            var user = _db.FindUser(email: email);
            if(user == null)
            {
                return ApiResponseService.BadRequest("User not found.");
            }
            //get user's plants
            List<Plant> plants = _db.GetPlantsForUser(user.Id);

            //construct list of just PlantId's
            List<string> plantIds = new List<string>();
            if(plants != null)
            {
                foreach(Plant p in plants)
                {
                    plantIds.Add(p.Id);
                }
            }


            return Json(plantIds, JsonRequestBehavior.AllowGet);
        }


        //POST: piapi/updateplant
        //ID,Water,Light
        [HttpPost]
        public void updateplant(Plant plant)
        {
            Plant currentPlant = _db.FindPlant(plant.Id);

            if (currentPlant == null)
            {
                return;
            }

            Species currentSpecies = _db.FindSpecies(currentPlant.SpeciesId);
            double previousLightVariable = plant.CurrentLight;
            currentPlant.CurrentLight = plant.CurrentLight;
            currentPlant.CurrentWater = plant.CurrentWater;
            currentPlant.UpdateTime = (int)DateTime.Now.TimeOfDay.TotalHours;


            var result = _db.UpdatePlant(currentPlant);

            if(result == true)
            {
                HandleLightTracker(currentPlant, currentSpecies, previousLightVariable);
                HandleData(currentPlant, currentSpecies);
            }

        }

        private void HandleData(Plant plant, Species species)
        {
            User currentUser = _db.FindPlantUser(plant.Id);

            if (currentUser == null)
            {
                return;
            }

            Plant currentPlant = plant;
            Species currentSpecies = species;
            Dictionary<string, bool> userPreferences = _db.GetNotificationPreferences(currentUser.Id);

            bool email = userPreferences["Email"];
            bool sms = userPreferences["Phone"];
            bool waterNotification = false;
            bool lightNotification = false;


            if(email == true || sms == true)
            {

                string emailSubject = null;
                string emailBody = null;
                string smsBody = null;

                ResponseTypes waterType = CheckWater(currentPlant.CurrentWater, currentSpecies.WaterMax, currentSpecies.WaterMin);
                ResponseTypes lightType = CheckLight(currentPlant.CurrentLight, currentSpecies.WaterMax, currentSpecies.WaterMin, currentPlant.LightTracker);

                switch(waterType)
                {
                    case ResponseTypes.HighWater:
                    {
                        emailSubject = "High Water";
                        emailBody = ComposeEmailBody(currentUser.FirstName, waterType, currentPlant.Nickname);
                        smsBody = ComposeSMSBody(waterType, currentPlant.Nickname);
                        waterNotification = true;
                        break;
                    }
                    case ResponseTypes.LowWater:
                    {
                        emailSubject = "Low Water";
                        emailBody = ComposeEmailBody(currentUser.FirstName, waterType, currentPlant.Nickname);
                        smsBody = ComposeSMSBody(waterType, currentPlant.Nickname);
                        waterNotification = true;
                        break;
                    }
                    default:
                        break;

                }
                if(waterNotification == true && email == true)
                {
                    SendEmail(currentUser.Email, currentPlant.Nickname, emailSubject, emailBody).Wait();
                }
                if(waterNotification == true && sms == true)
                {
                    SendSMS(currentUser.Phone, smsBody);
                }

                switch(lightType)
                {

                    case ResponseTypes.HighLight:
                    {
                        emailSubject = "High Light";
                        emailBody = ComposeEmailBody(currentUser.FirstName, lightType, currentPlant.Nickname);
                        smsBody = ComposeSMSBody(lightType, currentPlant.Nickname);
                        lightNotification = true;
                        currentPlant.LightTracker = 0;
                        _db.UpdatePlant(currentPlant);
                        break;
                    }
                    case ResponseTypes.LowLight:
                    {
                        emailSubject = "Low Light";
                        emailBody = ComposeEmailBody(currentUser.FirstName, lightType, currentPlant.Nickname);
                        smsBody = ComposeSMSBody(lightType, currentPlant.Nickname);
                        lightNotification = true;
                        currentPlant.LightTracker = 0;
                        _db.UpdatePlant(currentPlant);
                        break;
                    }
                    default:
                        break;
                }

                if(lightNotification == true && email == true)
                {
                    SendEmail(currentUser.Email, currentPlant.Nickname, emailSubject, emailBody).Wait();
                }
                if(lightNotification == true && sms == true)
                {
                    SendSMS(currentUser.Phone, smsBody);
                }
            }
        }

        private ResponseTypes CheckWater(double currentWater, double speciesMax, double speciesMin)
        {
            if(currentWater > speciesMax)
            {
                return ResponseTypes.HighWater;
            }

            if(currentWater < speciesMin)
            {
                return ResponseTypes.LowWater;
            }
            return ResponseTypes.Okay;
        }

        private ResponseTypes CheckLight(double currentLight, double speciesMax, double speciesMin, int lightTracker)
        {
            if(lightTracker >= 3)
            {
                if(currentLight > speciesMax)
                {
                    return ResponseTypes.HighLight;
                }
                if(currentLight < speciesMin)
                {
                    return ResponseTypes.LowLight;
                }
            }
            return ResponseTypes.Okay;
        }

        private void HandleLightTracker(Plant plant, Species species, double previousLight)
        {
            Plant currentPlant = plant;
            Species currentSpecies = species;
            int lightTrackerTemp = currentPlant.LightTracker;

            if(currentPlant.CurrentLight > currentSpecies.LightMax || currentPlant.CurrentLight < currentSpecies.LightMin)
            {
                if(currentPlant.CurrentLight == previousLight && currentPlant.UpdateTime > 7 && currentPlant.UpdateTime < 19)
                {
                    currentPlant.LightTracker = lightTrackerTemp + 1;
                    var result = _db.UpdatePlant(currentPlant);

                    if(result != true)
                    {
                        throw new Exception("Error in HandlelightTracker, PiAPIController");
                    }
                }
            }
        }

        private string ComposeSMSBody(ResponseTypes response, string plantName)
        {
            string message = _db.GetNotificationResponseMessage(response) + " ~" + plantName;
            return message;
        }

        private string ComposeEmailBody(string userName, ResponseTypes response, string plantName)
        {
            string message = "Hello " + userName + "! " + _db.GetNotificationResponseMessage(response) + "\n ~" + plantName;
            return message;
        }

        private void SendSMS(string userPhone, string msgbody)
        {
            const string accountSid = "AC3dfa39c6c58dba42c4867c99fb626324";
            const string authToken = "cab21f1579fd511e71c56bc45fcc2dbc";
            string completeNumber = "+1" + userPhone;

            TwilioClient.Init(accountSid, authToken);

            var message = MessageResource.Create(
            body: msgbody,
            from: new Twilio.Types.PhoneNumber("+19713184244"),
            to: new Twilio.Types.PhoneNumber(completeNumber)
            );
        }
        private static async Task SendEmail(string email, string plantName, string msgSubject, string msgcontent)
        {
            string apiKey = "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";//System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(plantName + "@wetmyplants.com", plantName),
                Subject = msgSubject,
                PlainTextContent = msgcontent//,
                                             // HtmlContent = "<strong>Please click on this link to reset your password: </strong><a href=\"" + urlString + "\" > wetmyplants.azurewebsites.net/Account/ResetPassword</a>"
            };
            msg.AddTo(new EmailAddress(email, "user"));
            var response = await client.SendEmailAsync(msg).ConfigureAwait(false);
        }
    }
}