using System;
using Xunit;

namespace WebApi.Formatting.JsonMask.Tests
{
   public class JsonMaskTests
   {
      [Fact]
      public void Mask_WillReturnOriginalJson()
      {
         string json = @"{""a"":""value""}";
         var mask = new JsonMask("a");
         Assert.Equal(json, mask.Filter(json));
      }

      [Fact]
      public void Mask_OnSimpleProperty()
      {
         string json = @"{""a"":""value a"",""b"":""value b""}";
         var mask = new JsonMask("a");
         Assert.Equal(@"{""a"":""value a""}", mask.Filter(json));
      }

      [Fact]
      public void Mask_OnMultipleProperties()
      {
         string json = @"{""a"":""value a"",""b"":""value b"",""c"":""value c""}";
         var mask = new JsonMask("a,c");
         Assert.Equal(@"{""a"":""value a"",""c"":""value c""}", mask.Filter(json));
      }

      [Fact]
      public void Mask_SubProperties()
      {
         string json = @"{""a"":{""b"":""value b"",""c"":""value c"",""d"":""value d""}}";
         var mask = new JsonMask("a(c)");
         Assert.Equal(@"{""a"":{""c"":""value c""}}", mask.Filter(json));
      }

      [Fact]
      public void Mask_IgnorePropertyCaseInMask()
      {
         string json = @"{""a"":""value a"",""b"":""value b"",""c"":""value c""}";
         var mask = new JsonMask("A", true);
         Assert.Equal(@"{""a"":""value a""}", mask.Filter(json));
      }

      [Fact]
      public void Mask_WhenSpecifyingComparisonType_ReturnFilteredResult()
      {
         string json = @"{""a"":""value a"",""b"":""value b"",""c"":""value c""}";
         var mask = new JsonMask("A", StringComparison.OrdinalIgnoreCase);
         Assert.Equal(@"{""a"":""value a""}", mask.Filter(json));
      }
   }
}
