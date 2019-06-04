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

namespace WebApp.Controllers
{
    [RoutePrefix("piapi")]
    public class PiAPIController : Controller
    {
        private readonly IDbHelper _db;

        private JsonResult Jsonify(string content) => Json($"{{ content: '{content}' }}");
        // BadRequest takes a string or JSON object and returns it along with a 500 (BadRequest) status code
        private ActionResult BadRequest(string content) => BadRequest(Jsonify(content));
        private ActionResult BadRequest(JsonResult content) =>
            new HttpStatusCodeResult(HttpStatusCode.BadRequest, content.Data.ToString());

        // Ok takes a string or JSON object and returns it along with a 200 (OK) status code
        private ActionResult Ok(string content) => Ok(Jsonify(content));
        private ActionResult Ok(JsonResult content) =>
              new HttpStatusCodeResult(HttpStatusCode.OK, content.Data.ToString());

        //external pi requirements include requests, os, apscheduler
        // CTOR receives the DbHelper through Dependency Injection
        public PiAPIController(IDbHelper db) => _db = db;

        // GET: PiAPI
        //piapi
        public String Index()
        {
            return "I'm feeling Plant-Tastic!";
        }
        
        //GET: piapi/getuserplants >> Return list of plant ids
        // userEmail
        [HttpGet]
        public JsonResult GetUserPlants(string email)
        {
            //verify and find user
            var user = _db.FindUser(email: email);              
            if (user == null)
            {
                BadRequest("User not found.");
            }

            //get user's plants
            List<Plant> plants = _db.GetPlantsForUser(user.Id);

            //construct list of just PlantId's
            List<string> plantIds = new List<string>();
            foreach (Plant p in plants)
            {
                plantIds.Add(p.Id);
            }

            return Json(plantIds, JsonRequestBehavior.AllowGet);
        }


        //POST: piapi/updateplant
        //ID,Water,Light
        [HttpPost]
        public void updateplant(Plant plant)
        {
            Plant currentPlant = _db.FindPlant(plant.Id);
            Species currentSpecies = _db.FindSpecies(currentPlant.SpeciesId);
            double previousLightVariable = plant.CurrentLight;
            currentPlant.CurrentLight = plant.CurrentLight;
            currentPlant.CurrentWater = plant.CurrentWater;
            currentPlant.UpdateTime = (int)DateTime.Now.TimeOfDay.TotalHours;
                   

            var result = _db.UpdatePlant(currentPlant);
                        
            if (result == true)
            {
                HandleLightTracker(currentPlant, currentSpecies, previousLightVariable);
                HandleData(currentPlant, currentSpecies);
            }

        }
        
        public void HandleData(Plant plant, Species species)
        {
            User currentUser = _db.FindPlantUser(plant.Id);
            Plant currentPlant = plant;
            Species currentSpecies = species;          
            Dictionary<string, bool> userPreferences = _db.GetNotificationPreferences(currentUser.Id);

            bool email = userPreferences["Email"];
            bool sms = userPreferences["Phone"];
            bool waterNotification = false;
            bool lightNotification = false;
            

            if (email == true || sms == true)
            {

                string emailSubject = null;
                string emailBody = null;
                string smsBody = null;

                ResponseTypes waterType = CheckWater(currentPlant.CurrentWater, currentSpecies.WaterMax, currentSpecies.WaterMin);
                ResponseTypes lightType = CheckLight(currentPlant.CurrentLight, currentSpecies.WaterMax, currentSpecies.WaterMin, currentPlant.LightTracker);

                switch (waterType)
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
                if (waterNotification == true && sms == true)
                {
                    SendSMS(currentUser.Phone, smsBody);
                }

                switch (lightType)
                {

                    case ResponseTypes.HighLight:
                        {
                            emailSubject = "High Light";
                            emailBody = ComposeEmailBody(currentUser.FirstName, lightType, currentPlant.Nickname);
                            smsBody = ComposeSMSBody(lightType, currentPlant.Nickname);
                            lightNotification = true;
                            break;
                        }
                    case ResponseTypes.LowLight:
                        {
                            emailSubject = "Low Light";
                            emailBody = ComposeEmailBody(currentUser.FirstName, lightType, currentPlant.Nickname);
                            smsBody = ComposeSMSBody(lightType, currentPlant.Nickname);
                            lightNotification = true;
                            break;
                        }
                    default:
                        break;
                }

                if (lightNotification == true && email == true)
                {
                    SendEmail(currentUser.Email, currentPlant.Nickname, emailSubject, emailBody).Wait();
                }
                if (lightNotification == true && sms == true)
                {
                    SendSMS(currentUser.Phone, smsBody);
                }
            }
        }

        public ResponseTypes CheckWater(double currentWater, double speciesMax, double speciesMin)
        {
            if (currentWater > speciesMax)
            {
                return ResponseTypes.HighWater;
            }
            
            if (currentWater < speciesMin)
            {
                return ResponseTypes.LowWater;
            }
            return ResponseTypes.Okay;
        }

        public ResponseTypes CheckLight(double currentLight, double speciesMax, double speciesMin, int lightTracker)
        {
            if(lightTracker >= 3)
            {
                if (currentLight > speciesMax)
                {
                    return ResponseTypes.HighLight;
                }
                if (currentLight < speciesMin)
                {
                    return ResponseTypes.LowLight;
                }
            }
            return ResponseTypes.Okay;
        }
                
        public void HandleLightTracker(Plant plant, Species species, double previousLight)
        {
            Plant currentPlant = plant;
            Species currentSpecies = species;
            int lightTrackerTemp = currentPlant.LightTracker;

            if (currentPlant.CurrentLight > currentSpecies.LightMax || currentPlant.CurrentLight < currentSpecies.LightMin)
            {
                if (currentPlant.CurrentLight == previousLight && currentPlant.UpdateTime > 7 && currentPlant.UpdateTime < 19)
                {
                    currentPlant.LightTracker = lightTrackerTemp + 1;
                    var result = _db.UpdatePlant(currentPlant);

                    if (result != true)
                    {
                        throw new Exception("Error in HandlelightTracker, PiAPIController");
                    }
                }
            }
        }

        public string ComposeSMSBody(ResponseTypes response, string plantName)
        {
            string message = _db.GetNotificationResponseMessage(response) + " ~" + plantName;
            return message;
        }

        public string ComposeEmailBody(string userName, ResponseTypes response, string plantName)
        {
            string message = "Hello " + userName + "! " + _db.GetNotificationResponseMessage(response) + "\n ~" + plantName;
            return message;
        }

        public void SendSMS(string userPhone, string msgbody)
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
        static public async Task SendEmail(string email, string plantName, string msgSubject, string msgcontent)
        {
            string apiKey = "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";//System.Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
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
        }
    }
}