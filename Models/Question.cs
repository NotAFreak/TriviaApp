using System;
using System.Collections.Generic;

namespace TriviaApp.Models;

public partial class Question
{
    public int QuestionId { get; set; }

    public int CategoryId { get; set; }

    public int DifficultyId { get; set; }

    public string Text { get; set; } = null!;

    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    public virtual Category Category { get; set; } = null!;

    public virtual Difficulty Difficulty { get; set; } = null!;

    public virtual ICollection<UserQuestion> UserQuestions { get; set; } = new List<UserQuestion>();
}
