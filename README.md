WebApi.Formatting.JsonMask
==========================

`WebApi.Formatting.JsonMask` provides a [MediaTypeFormatter](http://msdn.microsoft.com/en-us/library/system.net.http.formatting.mediatypeformatter.aspx) implementation for [ASP.NET Web API](http://www.asp.net/web-api) that allows the user to specify specific parts of a JS object, hiding/masking the rest, thereby allowing the user to get a partial response.

To use the `JsonMaskMediaTypeFormatter`, add the following code to your Web API Configuration:

`FormatterConfig.RegisterFormatters(GlobalConfiguration.Configuration.Formatters);`

The `FormatterConfig` class looks this:

    public class FormatterConfig
    {
        public static void RegisterFormatters(MediaTypeFormatterCollection formatters)
        {
            var jsonFormatter = formatters.JsonFormatter;
            jsonFormatter.SerializerSettings = new JsonSerializerSettings {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            // Insert the JSONP formatter in front of the standard JSON formatter.
            var jsonMaskFormatter = new JsonMaskMediaTypeFormatter(formatters.JsonFormatter);
            formatters.Insert(0, jsonMaskFormatter);
        }
    }

If you've used the Google APIs, and provided a `?fields=` querystring to get a
[Partial Response](https://developers.google.com/+/api/#partial-responses), you've
already used this language. The main difference is that this MediaTypeFormatter defaults
to looking for a `?$fields=` querystring field. If you wish to use something else, then
you can specify it when creating the MediaTypeFormatter

    var jsonMaskFormatter = new JsonMaskMediatypeFormatter(formatters.JsonFormatter, "fields");

## Syntax

The syntax is loosely based on XPath:

- ` a,b,c` comma-separated list will select multiple fields
- ` a/b/c` path will select a field from its parent
- `a(b,c)` sub-selection will select many fields from a parent
- ` a/*/c` the star `*` wildcard will select all items in a field

## Examples

Identify the fields you want to keep

    ?$fields=name,address/zip

From this sample object:

    {
       "name": "John Doe",
       "age": 32,
       "position": "programmer",
       "address": {
          "city": "Seattle",
          "state": "WA",
          "zip": "98101"
       }
    }

You will get back:

    {
       "name": "John Doe",
       "address": {
          "zip": "98101"
       }
    }