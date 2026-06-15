#nullable enable
using Newtonsoft.Json;

namespace TriviaApp.TriviaAPI;
[JsonObject]
public class APICategory {
	public uint id;
	public string name;
}