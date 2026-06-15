#nullable enable
using Newtonsoft.Json;

namespace TriviaApp.TriviaAPI;
[JsonObject]
public class APIResponse {
	public int responseCode;
	public List<APIQuestion> results = new();
}