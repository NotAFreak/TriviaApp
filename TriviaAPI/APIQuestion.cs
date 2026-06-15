#nullable enable
using Newtonsoft.Json;

namespace TriviaApp.TriviaAPI;
[JsonObject]
public class APIQuestion {
	public string type = "";
	public string difficulty = "";
	public string category = "";
	public string question = "";
	public string correct_answer = "";
	public List<string> incorrect_answers = new();
}