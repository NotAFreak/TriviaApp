using System;
using System.Collections.Generic;

namespace TriviaApp.Models;

public partial class UserQuestion
{
    public int UserId { get; set; }

    public int QuestionId { get; set; }

    public int CompletionState { get; set; }

    public double CompletionTime { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
