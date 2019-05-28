using System.Collections.Generic;
using System.Net;
using System.Web.Helpers;
using System.Web.Mvc;

namespace WebApp.Helpers
{
    public static class ApiResponseService
    {
        /* HELPER FUNCTIONS */
        /* Jsonify takes a string and packages it as a JSON object under the "content" key */
        //public static JsonResult Jsonify(string content) => Controller.Json($"{{ content: '{content}' }}");
        public static JsonResult Jsonify(string content)
        {
            var data = new Dictionary<string, string>
            {
                { "content", content }
            };

            var json = Json.Encode(data);

            var result = new JsonResult
            {
                Data = json
            };

            return result;
        }

        /* BadRequest takes a string or JSON object and returns it along with a 500 (BadRequest) status code */
        public static ActionResult BadRequest(string content) => BadRequest(Jsonify(content));

        public static ActionResult BadRequest(JsonResult content) =>
            new HttpStatusCodeResult(HttpStatusCode.BadRequest, content.Data.ToString());

        /* Ok takes a string or JSON object and returns it along with a 200 (OK) status code */
        public static ActionResult Ok(string content) => Ok(Jsonify(content));

        public static ActionResult Ok(JsonResult content) =>
            new HttpStatusCodeResult(HttpStatusCode.OK, content.Data.ToString());
    }
}