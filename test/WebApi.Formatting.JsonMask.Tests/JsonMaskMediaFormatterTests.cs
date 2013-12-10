using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using Xunit;

namespace WebApi.Formatting.JsonMask.Tests
{
   public class JsonMaskMediaFormatterTests
   {
      [Fact]
      public void CanReadType_AlwaysReturnsFalse()
      {
         var formatter = CreateFormatter();
         Assert.False(formatter.CanReadType(typeof(object)));
         Assert.False(formatter.CanReadType(typeof(string)));
         Assert.False(formatter.CanReadType(typeof(JToken)));
      }

      [Fact]
      public void IsJsonMaskRequest_ReturnsFalseForNonMaskedRequest()
      {
         var request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/api");
         JsonMask mask;
         Assert.False(JsonMaskMediaTypeFormatter.IsMaskedRequest(request, "$fields", out mask));
      }

      [Fact]
      public void IsJsonMaskRequest_ReturnsTrueForMaskedRequest()
      {
         var request = new HttpRequestMessage(HttpMethod.Get, "http://example.org/api?fields=a");
         JsonMask mask;
         Assert.True(JsonMaskMediaTypeFormatter.IsMaskedRequest(request, "fields", out mask));
      }

      [Fact]
      public void IsJsonMaskRequest_ReturnsTrueForMaskedNonGetRequest()
      {
         var request = new HttpRequestMessage(HttpMethod.Post, "http://example.org/api?fields=a");
         JsonMask mask;
         Assert.True(JsonMaskMediaTypeFormatter.IsMaskedRequest(request, "fields", out mask));
      }

      [Fact]
      public async Task WriteToStreamAsync_MasksTheResponse()
      {
         var config = new HttpConfiguration();
         config.Formatters.Insert(0, CreateFormatter(config.Formatters.JsonFormatter));
         config.Routes.MapHttpRoute("Default", "api/{controller}/{id}", new { id = RouteParameter.Optional });

         using (var server = new HttpServer(config))
         using (var client = new HttpClient(server)) {
            client.BaseAddress = new Uri("http://test.org");

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/values/1?$fields=A");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = response.Content;
            Assert.Equal("application/json", content.Headers.ContentType.MediaType);

            var text = await content.ReadAsStringAsync();
            Assert.Equal(@"{""A"":""the letter a""}", text);
         }
      }

      [Fact]
      public async Task WriteToStreamAsync_GetFullResponseWithoutMask()
      {
         var config = new HttpConfiguration();
         config.Formatters.Insert(0, CreateFormatter(config.Formatters.JsonFormatter));
         config.Routes.MapHttpRoute("Default", "api/{controller}/{id}", new { id = RouteParameter.Optional });

         using (var server = new HttpServer(config))
         using (var client = new HttpClient(server)) {
            client.BaseAddress = new Uri("http://test.org");

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/values/1");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = response.Content;
            Assert.Equal("application/json", content.Headers.ContentType.MediaType);

            var text = await content.ReadAsStringAsync();
            Assert.Equal(@"{""A"":""the letter a"",""B"":""the letter b""}", text);
         }
      }

      static JsonMaskMediaTypeFormatter CreateFormatter(JsonMediaTypeFormatter formatter = null)
      {
         var jsonFormatter = formatter ?? new JsonMediaTypeFormatter();
         return new JsonMaskMediaTypeFormatter(jsonFormatter);
      }
   }
}
