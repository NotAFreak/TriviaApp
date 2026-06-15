#nullable enable
using TriviaApp.Models;

namespace TriviaApp.ViewModels;

public class HomeViewModel : BaseViewModel {
	//	Leaderboards
	public int TotalAnswered = 0;
	public int TotalCorrect = 0;
	public int TotalIncorrect = 0;
	
	// In Seconds
	public float AverageAnswerTime = 0.0f;
	public float MinimumAnswerTime = 0.0f;
	public float MaximumAnswerTime = 0.0f;
	
	// Nice info
	private Dictionary<int, Category> categories = new();
	private Dictionary<int, Difficulty> difficulties = new();
}