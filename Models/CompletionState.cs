namespace TriviaApp.Models;

public enum CompletionState : byte {
	Unknown = 0,
	Correct = 1,
	Incorrect = 2,
	OutOfTime = 3
}