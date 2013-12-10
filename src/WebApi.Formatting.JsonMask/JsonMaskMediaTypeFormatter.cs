using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebApi.Formatting.JsonMask
{
   public class JsonMaskMediaTypeFormatter : MediaTypeFormatter
   {
      private readonly MediaTypeFormatter _jsonMediaTypeFormatter;
      private readonly string _fieldsQueryParameterName;

      private readonly JsonMask _jsonMask;

      public JsonMaskMediaTypeFormatter(MediaTypeFormatter jsonMediaTypeFormatter, string fieldsQueryParameterName = "$fields")
      {
         if (jsonMediaTypeFormatter == null) {
            throw new ArgumentNullException("jsonMediaTypeFormatter");
         }

         if (String.IsNullOrWhiteSpace(fieldsQueryParameterName)) {
            fieldsQueryParameterName = "$fields";
         }

         _jsonMediaTypeFormatter = jsonMediaTypeFormatter;
         _fieldsQueryParameterName = fieldsQueryParameterName;

         foreach (var encoding in _jsonMediaTypeFormatter.SupportedEncodings) {
            SupportedEncodings.Add(encoding);
         }

         foreach (var mediaType in _jsonMediaTypeFormatter.SupportedMediaTypes) {
            SupportedMediaTypes.Add(mediaType);
         }
      }

      private JsonMaskMediaTypeFormatter(JsonMask mask, MediaTypeFormatter jsonMediaTypeFormatter, string fieldsQueryParameterName)
         : this(jsonMediaTypeFormatter, fieldsQueryParameterName)
      {
         if (mask == null) {
            throw new ArgumentNullException("mask");
         }

         _jsonMask = mask;
      }

      public override bool CanReadType(Type type)
      {
         return false;
      }

      public override bool CanWriteType(Type type)
      {
         if (type == null) {
            throw new ArgumentNullException("type");
         }

         return _jsonMediaTypeFormatter.CanWriteType(type);
      }

      public override Task WriteToStreamAsync(Type type, object value, Stream writeStream, HttpContent content, TransportContext transportContext)
      {
         if (type == null) {
            throw new ArgumentNullException("type");
         }

         if (writeStream == null) {
            throw new ArgumentNullException("writeStream");
         }

         var encoding = SelectCharacterEncoding(content == null ? null : content.Headers);

         var memStream = new MemoryStream();
         return _jsonMediaTypeFormatter.WriteToStreamAsync(type, value, memStream, content, transportContext)
            .Then(() => {
               var json = encoding.GetString(memStream.ToArray());
               using (var jsonWriter = new JsonTextWriter(new StreamWriter(writeStream, encoding)) { CloseOutput = false }) {
                  _jsonMask.Filter(jsonWriter, json);
                  jsonWriter.Flush();
               }
               memStream.Close();
            });
      }

      public override MediaTypeFormatter GetPerRequestFormatterInstance(Type type, HttpRequestMessage request, MediaTypeHeaderValue mediaType)
      {
         if (type == null) {
            throw new ArgumentNullException("type");
         }

         if (request == null) {
            throw new ArgumentNullException("request");
         }

         JsonMask mask;
         if (IsMaskedRequest(request, _fieldsQueryParameterName, out mask)) {
            return new JsonMaskMediaTypeFormatter(mask, _jsonMediaTypeFormatter, _fieldsQueryParameterName);
         }

         return _jsonMediaTypeFormatter.GetPerRequestFormatterInstance(type, request, mediaType);
      }

      public static bool IsMaskedRequest(HttpRequestMessage request, string fieldsQueryParameterName, out JsonMask jsonMask)
      {
         jsonMask = null;
         if (request == null) {
            return false;
         }

         jsonMask = request.GetQueryNameValuePairs()
            .Where(kvp => kvp.Key.Equals(fieldsQueryParameterName, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new JsonMask(kvp.Value))
            .FirstOrDefault();

         return jsonMask != null;
      }
   }
}
