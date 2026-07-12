using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TriviaApp.Models;
using TriviaApp.Runtime;
using TriviaApp.ViewModels;
using Question = TriviaApp.Models.Question;
using Leaderboard = System.Collections.Generic.List<TriviaApp.ViewModels.LeaderboardEntry>;

namespace TriviaApp.Controllers;

public class HomeController : Controller {
    public const uint maxLeaderboardSize = 10;
    private TriviaContext db;

    public HomeController(TriviaContext db) {
        this.db = db;
    }

    public async Task TryAddNewQuestion(Question question) {
        
    }

    public bool IsUser() {
        return HttpContext.User.Identity is not null && HttpContext.User.Identity.IsAuthenticated;
    }
    
    /// <returns>User ID on success, -1 on fail</returns>
    public async Task<int?> GetUserId() {
        Claim? userIdClaim = HttpContext.User.FindFirst("userid");
        if (userIdClaim is null) {
            TempData["Error"] = "User not correctly logged in!";
            await HttpContext.SignOutAsync();
            return null;
        }

        User? user = db.Users.Include(o=>o.UserQuestions).FirstOrDefault(o => o.UserId == Convert.ToInt32(userIdClaim.Value));
        if (user is null) {
            TempData["Error"] = "User does not exist!";
            await HttpContext.SignOutAsync();
            return null;
        }

        return user.UserId;
    }
    
    // AJAX
    [HttpGet]
    public async Task<IActionResult> GetDailyQuestion() {
        DailyQuestionRequestViewModel vm = new();
        bool isUser = IsUser();
        if (isUser) {
            int userId = (int)await GetUserId();
            User? user = await db.Users.Include(o => o.UserQuestions).FirstOrDefaultAsync(o=>o.UserId == (int)userId);
            
            Question originalQuestion = await TriviaAPI.Client.CurrentDailyQuestion(db);
            await TryAddNewQuestion(originalQuestion);
            vm.question = new ClientQuestion(originalQuestion);
            
            UserQuestion? answer = user.UserQuestions.FirstOrDefault(o => o.QuestionId == vm.question.QuestionId);
            vm.HasCompleted = answer is not null;
            if (answer is not null) {
                Assert.IsTrue(answer.UserId == userId);
                vm.question = null;
            }
        } else {
            Claim? claim = HttpContext.User.FindFirst("completed_Daily");
            if (claim is null) {
                vm.HasCompleted = false;
                Question originalQuestion = await TriviaAPI.Client.CurrentDailyQuestion(db);
                await TryAddNewQuestion(originalQuestion);
                vm.question = new ClientQuestion(originalQuestion);
            } else {
                vm.HasCompleted = Convert.ToBoolean(claim.Value);
            }
        }

        if (isUser) {
            if (vm.HasCompleted) {
                Assert.IsNull(vm.question);
            } else {
                Assert.IsNotNull(vm.question);
                TriviaTimer.StartUser((int)await GetUserId(), vm.question.QuestionId);
            }
        }
        return Json(JsonConvert.SerializeObject(vm));
    }
    
    // AJAX
    [HttpGet]
    public async Task<IActionResult> SubmitDailyQuestion(int answerId) {
        Question question = await TriviaAPI.Client.CurrentDailyQuestion(db);
        int? userId = await GetUserId();
        if (userId is not null) {
            if (!TriviaTimer.UserAlreadyExists((int)userId)) {
                ViewBag["Error"] = "User hasn't started this question!";
                return RedirectToAction("Index", "Home");
            }
        }
        DailyQuestionSubmitViewModel vm = new();
        Answer? answer = question.Answers.FirstOrDefault(o => o.AnswerId == answerId);
        if (answer is null) {
            ViewBag["Error"] = "Answer doesn't exist!";
            return RedirectToAction("Index", "Home");
        }
        vm.state = Convert.ToBoolean(answer.IsCorrect) ? CompletionState.Correct : CompletionState.Incorrect;
        if (IsUser() && userId is not null) {
            vm.timing = TriviaTimer.FinishUser((int)userId);

            await db.UserQuestions.AddAsync(new UserQuestion() {
                CompletionState = (int)vm.state,
                CompletionTime = ((vm.timing.finishTime - vm.timing.startTime)!).Value.TotalSeconds,
                QuestionId = question.QuestionId,
                UserId = (int)userId
            });
            await db.SaveChangesAsync();
        }
        return Json(JsonConvert.SerializeObject(vm));
    }
    
    public async Task<IActionResult> Index() {
        HomeViewModel vm = new();
        bool isUser = HttpContext.User.Identity is not null && HttpContext.User.Identity.IsAuthenticated;
        Claim? userIdClaim = HttpContext.User.FindFirst("userid");
        if (isUser) {
            if (userIdClaim is null) {
                TempData["Error"] = "User not correctly logged in!";
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            User? user = db.Users.Include(o=>o.UserQuestions).FirstOrDefault(o => o.UserId == Convert.ToInt32(userIdClaim.Value));
            if (user is null) {
                TempData["Error"] = "User does not exist!";
                await HttpContext.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }
        }
        if (isUser) {
            User user = await db.Users.Include(o => o.UserQuestions).FirstAsync(o=>o.UserId == Convert.ToInt32(userIdClaim.Value));
            List<UserQuestion> userQuestions = user.UserQuestions.Where(o =>
                (byte)o.CompletionState != (byte)CompletionState.OutOfTime &&
                (byte)o.CompletionState != (byte)CompletionState.Unknown)
                .ToList();
            if (userQuestions.Count == 0) return View(vm);
            vm.TotalAnswered = userQuestions.Count;
            vm.TotalCorrect = userQuestions.Count(o => (byte)o.CompletionState == (byte)CompletionState.Correct);
            vm.TotalIncorrect = userQuestions.Count(o => (byte)o.CompletionState == (byte)CompletionState.Incorrect);

            vm.AverageAnswerTime = (float)userQuestions.Average(o => o.CompletionTime);
            vm.MinimumAnswerTime = (float)userQuestions.Min(o => o.CompletionTime);
            vm.MaximumAnswerTime = (float)userQuestions.Max(o => o.CompletionTime);
        }
        
        // Gen leaderboards
        List<Models.User> users = await db.Users.Include(o=>o.UserQuestions).Where(o=>o.DisplayLeaderboard == 1).ToListAsync();
        Leaderboard daily = new();
        foreach (Models.User user in users) {
            Question dailyQuestion = await TriviaAPI.Client.CurrentDailyQuestion(db);
            UserQuestion? answer = user.UserQuestions.FirstOrDefault(o=>o.QuestionId == dailyQuestion.QuestionId);
            if(answer is null) continue;
            if(answer.CompletionState != (int)CompletionState.Correct) continue;
            daily.Add(new LeaderboardEntry() {
                time = (float)answer.CompletionTime,
                username = user.Username
            });
            if(daily.Count >= maxLeaderboardSize) break;
        }
        if (daily.Count != 0) {
            daily = daily.OrderByDescending(o=>o.time).ToList();
        }
        vm.dailyLeaderboard = daily;
        Leaderboard global = new();
        foreach (Models.User user in users) {
            if (user.DisplayLeaderboard == 1) {
                if(user.UserQuestions.Count == 0) continue;
                List<UserQuestion> questions = user.UserQuestions.Where(o => o.CompletionState == (int)CompletionState.Correct).ToList();
                if(questions.Count == 0) continue;
                float time = (float)questions.Average(o => o.CompletionTime);
                global.Add(new LeaderboardEntry() {
                    time = time,
                    username = user.Username
                });
                if(global.Count >= maxLeaderboardSize) break;
            }
        }

        if (global.Count != 0) {
            global = global.OrderByDescending(o=>o.time).ToList();
        }

        vm.globalLeaderboard = global;
        return View(vm);
    }

    public async Task<IActionResult> Privacy() {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
