using System;
using System.Web.Http;

namespace WebApi.Formatting.JsonMask.Tests
{
   public class ValuesController : ApiController
   {
      public object Get(int id)
      {
         return new {
            A = "the letter a",
            B = "the letter b"
         };
      }
   }
}
