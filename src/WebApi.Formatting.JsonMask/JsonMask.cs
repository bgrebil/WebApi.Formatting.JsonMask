using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebApi.Formatting.JsonMask
{
   public class JsonMask
   {
      private readonly IEnumerable<PropertyMask> _propertiesToMask;
      private readonly StringComparison _defaultComparison;

      public JsonMask(string mask) 
         : this(mask, StringComparison.InvariantCulture) 
      { }

      public JsonMask(string mask, bool ignoreCaseOnProperties)
         : this(mask, ignoreCaseOnProperties ? StringComparison.OrdinalIgnoreCase : StringComparison.InvariantCulture)
      { }

      public JsonMask(string mask, StringComparison comparisonType)
      {
         _propertiesToMask = BuildMask(Tokenize(mask));
         _defaultComparison = comparisonType;
      }

      public string Filter(string json)
      {
         var sb = new StringBuilder(json.Length);
         using (var sw = new StringWriter(sb))
         using (var writer = new JsonTextWriter(sw)) {
            Filter(writer, json);
         }

         return sb.ToString();
      }

      public void Filter(JsonTextWriter writer, string json)
      {
         var jObject = JToken.Parse(json);
         ParseJson(writer, jObject, _propertiesToMask);
      }

      #region Tokenize Mask
      private IEnumerable<Token> Tokenize(string text)
      {
         var tokens = new List<Token>();

         string token = "";
         foreach (var ch in text) {
            if (__TERMINALS.Contains(ch)) {
               if (!String.IsNullOrWhiteSpace(token)) {
                  tokens.Add(new Token { Type = TokenType.Name, Value = token });
                  token = "";
               }
               tokens.Add(new Token { Type = TokenType.Terminal, Value = ch.ToString() });
            }
            else {
               token += ch;
            }
         }

         if (!String.IsNullOrWhiteSpace(token)) {
            tokens.Add(new Token { Type = TokenType.Name, Value = token });
         }

         return tokens.ToArray();
      }

      private IEnumerable<PropertyMask> BuildMask(IEnumerable<Token> tokens)
      {
         return BuildMask(new Queue<Token>(tokens), new Stack<Token>());
      }

      private List<PropertyMask> BuildMask(Queue<Token> tokens, Stack<Token> stack)
      {
         var properties = new List<PropertyMask>();

         while (tokens.Count > 0) {
            var token = tokens.Dequeue();
            if (token.Type == TokenType.Name) {
               var propToken = new PropertyMask {
                  Name = token.Value,
                  Mask = BuildMask(tokens, stack)
               };
               properties.Add(propToken);

               if (stack.Count > 0 && stack.Peek().Value == "/") {
                  stack.Pop();
                  return properties;
               }
            }
            else if (token.Type == TokenType.Terminal) {
               if (token.Value == ",") {
                  return properties;
               }
               else if (token.Value == "(") {
                  stack.Push(token);
               }
               else if (token.Value == ")") {
                  stack.Pop();
                  return properties;
               }
               else if (token.Value == "/") {
                  stack.Push(token);
               }
            }
         }

         return properties;
      }

      private static readonly char[] __TERMINALS = { ',', '/', '(', ')' };

      private enum TokenType { Name, Terminal }

      private class Token
      {
         public TokenType Type { get; set; }
         public string Value { get; set; }
      }

      private class PropertyMask
      {
         public string Name { get; set; }
         public List<PropertyMask> Mask { get; set; }
      }
      #endregion

      #region Filter
      private void ParseJson(JsonTextWriter writer, JToken obj, IEnumerable<PropertyMask> properties)
      {
         if (obj.Type == JTokenType.Object) {
            ParseJsonObject(writer, obj, properties);
         }

         if (obj.Type == JTokenType.Array) {
            ParseJsonArray(writer, (JArray)obj, properties);
         }
      }

      private void ParseJsonObject(JsonTextWriter writer, JToken obj, IEnumerable<PropertyMask> properties)
      {
         writer.WriteStartObject();

         foreach (JProperty x in obj) {
            if (properties.Any() && !properties.All(p => p.Name == "*")) {
               var propMask = properties.FirstOrDefault(m => m.Name.Equals(x.Name, _defaultComparison));
               if (propMask != null) {
                  writer.WritePropertyName(x.Name);
                  if (x.Value.Type == JTokenType.Object || x.Value.Type == JTokenType.Array) {
                     ParseJson(writer, x.Value, propMask.Mask);
                  }
                  else {
                     writer.WriteValue(((JValue)x.Value).Value);
                  }
               }
            }
            else {
               writer.WritePropertyName(x.Name);
               if (x.Value.Type == JTokenType.Object || x.Value.Type == JTokenType.Array) {
                  ParseJson(writer, x.Value, properties);
               }
               else {
                  writer.WriteValue(((JValue)x.Value).Value);
               }
            }
         }

         writer.WriteEndObject();
      }

      private void ParseJsonArray(JsonTextWriter writer, JArray array, IEnumerable<PropertyMask> properties)
      {
         writer.WriteStartArray();

         foreach (var x in array) {
            ParseJson(writer, x, properties);
         }

         writer.WriteEndArray();
      }
      #endregion
   }
}
