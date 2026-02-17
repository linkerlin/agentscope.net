// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AgentScope.Core.Message;

/// <summary>
/// Message class for communication between agents
/// </summary>
public class Msg
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("role")]
    public string Role { get; set; } = "user";

    [JsonProperty("content")]
    public object? Content { get; set; }

    [JsonProperty("url")]
    public List<string>? Url { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonProperty("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    public Msg()
    {
    }

    public Msg(string? name, object? content, string role = "user")
    {
        Name = name;
        Content = content;
        Role = role;
    }

    public string? GetTextContent()
    {
        if (Content is string text)
        {
            return text;
        }
        
        if (Content is Dictionary<string, object> dict && dict.ContainsKey("text"))
        {
            return dict["text"]?.ToString();
        }

        return Content?.ToString();
    }

    public void SetTextContent(string text)
    {
        Content = text;
    }

    public static MsgBuilder Builder()
    {
        return new MsgBuilder();
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}

/// <summary>
/// Builder for Msg class
/// </summary>
public class MsgBuilder
{
    private readonly Msg _msg = new();

    public MsgBuilder Id(string id)
    {
        _msg.Id = id;
        return this;
    }

    public MsgBuilder Name(string name)
    {
        _msg.Name = name;
        return this;
    }

    public MsgBuilder Role(string role)
    {
        _msg.Role = role;
        return this;
    }

    public MsgBuilder Content(object content)
    {
        _msg.Content = content;
        return this;
    }

    public MsgBuilder TextContent(string text)
    {
        _msg.Content = text;
        return this;
    }

    public MsgBuilder Url(List<string> urls)
    {
        _msg.Url = urls;
        return this;
    }

    public MsgBuilder Timestamp(DateTime timestamp)
    {
        _msg.Timestamp = timestamp;
        return this;
    }

    public MsgBuilder Metadata(Dictionary<string, object> metadata)
    {
        _msg.Metadata = metadata;
        return this;
    }

    public MsgBuilder AddMetadata(string key, object value)
    {
        _msg.Metadata ??= new Dictionary<string, object>();
        _msg.Metadata[key] = value;
        return this;
    }

    public Msg Build()
    {
        return _msg;
    }
}
