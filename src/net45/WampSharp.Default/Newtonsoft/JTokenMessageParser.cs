﻿using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WampSharp.Core.Message;
using WampSharp.Core.Serialization;
using WampSharp.V2.Binding.Parsers;

namespace WampSharp.Newtonsoft
{
    public class JTokenMessageParser : IWampTextMessageParser<JToken>
    {
        private readonly IWampMessageFormatter<JToken> mMessageFormatter;

        public JTokenMessageParser()
        {
            mMessageFormatter = new JsonWampMessageFormatter();
        }

        public WampMessage<JToken> Parse(string text)
        {
            return mMessageFormatter.Parse(JToken.Parse(text));
        }

        public string Format(WampMessage<JToken> message)
        {
            StringWriter writer = new StringWriter();
            JsonTextWriter jsonWriter = new JsonTextWriter(writer);
            jsonWriter.Formatting = Formatting.None;
            JToken raw = mMessageFormatter.Format(message);
            raw.WriteTo(jsonWriter);
            return writer.ToString();
        }
    }
}