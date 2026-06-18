#nullable enable
using TriviaApp.Models;
using TriviaApp.Runtime;
using TriviaApp.ViewModels;

// Strip out extra information so the player cant cheat
public class ClientAnswer {
	public int AnswerID;
	public string Text;
	public ClientAnswer(Answer answer) {
		this.AnswerID = answer.AnswerId;
		this.Text = answer.Text;
	}
}
public class ClientQuestion {
	public int QuestionId;
	public string Text = "";
	public List<ClientAnswer> Answers = new();
	
	public ClientQuestion() { }
	public ClientQuestion(Question question) {
		this.Text = question.Text;
		foreach (Answer answer in question.Answers) {
			this.Answers.Add(new(answer));
		}
	}
}

public class DailyQuestionRequestViewModel : BaseViewModel {
	public bool HasCompleted = false;
	public ClientQuestion? question = null;
}

public class DailyQuestionSubmitViewModel : BaseViewModel {
	public CompletionState state;
	public UserTiming timing;
}