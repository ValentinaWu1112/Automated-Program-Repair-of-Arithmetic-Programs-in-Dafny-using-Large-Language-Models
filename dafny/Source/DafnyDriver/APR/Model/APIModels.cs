using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using OpenAI.Chat;
using OpenAI;
using DotNetEnv;

/*
 * APIModels
 * <summary>
 *  Initialize the LLM by providing the model and API key, then pass the prompt to it.
 * </summary>
 * 
 * <fields>
 *  <field> _client: the model
 * </fields>
 */

namespace DafnyDriver.APR;
public class APIModels {
  private readonly ChatClient _client;

  public APIModels(string model, string apiKey, string endpoint)
  {
    var credential = new ApiKeyCredential(apiKey);
    var options = new OpenAIClientOptions
    {
      Endpoint = new Uri(endpoint)
    };
    _client = new ChatClient(model, credential, options);
  }

  public APIModels(string model) {
    string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (string.IsNullOrEmpty(apiKey))
    {
      throw new InvalidOperationException("OpenAI API key is not set in the environment variables.");
    }

    _client = new ChatClient(model, new ApiKeyCredential(apiKey));
  }

  public async Task<string> FixCandidateAsync(string code, string model) {
    var chatRequest = new { 
      model= model.ToString(),
      messages = new[] {
        new {
          role = "system",
          content = "You are a software expert specializing in formal methods using the Dafny programming language. You receive the following program where a verifier error message indicates an issue. The error is due to a buggy line, which is marked with the comment \"//buggy line\". Your task is to correct the buggy line to ensure the program verifies successfully.\n Do not include explanations.\n Return only fixed line.\n Here is the code: \n"
        },
        new {
          role = "user",
          content = string.Concat(code, "\nfixed line: \n")
        }
      },
      temperature= 0.7,
      max_tokens= 30
    };

    BinaryData json = BinaryData.FromObjectAsJson(chatRequest);
    ClientResult chatResult = await _client.CompleteChatAsync(BinaryContent.Create(json));

    var response = chatResult.GetRawResponse();
    var content = response.Content.ToString();
    
    using (JsonDocument doc = JsonDocument.Parse(content))
    {
      JsonElement root = doc.RootElement;
      return root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }

    // Return an empty string or handle the case where there is no result
    return string.Empty;
  }
}