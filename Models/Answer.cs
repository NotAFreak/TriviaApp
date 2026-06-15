using System;
using System.Collections.Generic;

namespace TriviaApp.Models;

public partial class Answer
{
    public int AnswerId { get; set; }

    public int QuestionId { get; set; }

    public string Text { get; set; } = null!;

    public int IsCorrect { get; set; }

    public virtual Question Question { get; set; } = null!;
}
