using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TriviaApp.Models;
using TriviaApp.ViewModels;
using Question = TriviaApp.Models.Question;

namespace TriviaApp.Controllers;

public class HomeController : Controller {
    private TriviaContext db;

    public HomeController(TriviaContext db) {
        this.db = db;
    }

    public async Task TryAddNewQuestion(Question question) {
        
    }
    
    // AJAX
    [HttpGet]
    public async Task<IActionResult> GetDailyQuestion() {
        DailyQuestionViewModel vm = new();
        bool isUser = HttpContext.User.Identity is not null && HttpContext.User.Identity.IsAuthenticated;
        if (isUser) {
            Claim? userIdClaim = HttpContext.User.FindFirst("userid");
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
            
        } else {
            Claim? claim = HttpContext.User.FindFirst("completed_Daily");
            if (claim is null) {
                vm.HasCompleted = false;
            } else {
                vm.HasCompleted = Convert.ToBoolean(claim.Value);
            }
        }

        Question? originalQuestion = null;
        if (!vm.HasCompleted) {
            originalQuestion = await TriviaAPI.Client.CurrentDailyQuestion(db);
            if (originalQuestion is not null) {
                TryAddNewQuestion(originalQuestion);
            }
            vm.question = new ClientQuestion(originalQuestion);
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
