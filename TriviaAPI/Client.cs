#nullable enable
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TriviaApp.Models;

namespace TriviaApp.TriviaAPI;

public class Client
{
    private const string BaseURL = "https://opentdb.com";
    private static Client? instance;
    private HttpClient client;

    private Models.Question? currentDailyQuestion;
    private DateTime currentDailyQuestionRetrieveTime;

    public Client()
    {
        instance = this;
        if (!ProxySettings.UseProxy)
        {
            client = new();
            return;
        }
        NetworkCredential networkCredential = new NetworkCredential(ProxySettings.ProxyUsername, ProxySettings.ProxyPassword);
        HttpClientHandler httpClientHandler = new()
        {
            Proxy = new WebProxy()
            {
                Address = new Uri("http://" + ProxySettings.ProxyAddress + ":" + ProxySettings.ProxyPort.ToString()),
            },
            UseProxy = true,
            DefaultProxyCredentials = networkCredential,
        };
        client = new HttpClient(httpClientHandler, false);
    }

    ~Client()
    {
        instance = null;
    }

    public static async Task GenerateDailyQuestion(TriviaContext db)
    {
        string json = "";
        try {
            json = await JsonFromAPI(BaseURL + "/api.php?amount=1");
        } catch (Exception ex) {
            // If failed (Like disconnected from internet), just use a previous one
            List<Question> questions = await db.Questions
                .Include(o => o.Answers)
                .Include(o => o.Category)
                .Include(o => o.Difficulty)
                .ToListAsync();
            DateTime now = DateTime.UtcNow;
            Random r = new(now.Year * now.Month * now.Day);
            instance.currentDailyQuestion = questions[r.Next(0, questions.Count)];
            instance.currentDailyQuestionRetrieveTime = now;
            return;
        }

        APIResponse? response = JsonConvert.DeserializeObject<APIResponse>(json);
        if (response is null) {
            throw new Exception("Couldn't deserialize new question!");
        }
        if (response.results.Count == 0) {
            throw new Exception("No questions in API response!");
        }
        APIQuestion question = response.results[0];
        Question? existingQuestion = await db.Questions.FirstOrDefaultAsync(o => o.Text == question.question);
        if (existingQuestion is not null) {
            instance.currentDailyQuestion = existingQuestion;
            instance.currentDailyQuestionRetrieveTime = DateTime.UtcNow;
            return;
        }
        string difficultyName = question.difficulty.UpperFirstLetter();
        Difficulty? difficulty = await db.Difficulties.FirstOrDefaultAsync(o => o.Name == difficultyName);
        if (difficulty is null) {
            db.Difficulties.Add(new() {
                Name = difficultyName
            });
            await db.SaveChangesAsync();
            difficulty = await db.Difficulties.FirstAsync(o => o.Name == difficultyName);
        }
        string categoryName = question.category.UpperFirstLetter();
        Category? category = await db.Categories.FirstOrDefaultAsync(o => o.Name == categoryName);
        if (category is null) {
            db.Categories.Add(new() {
                Name = categoryName
            });
            await db.SaveChangesAsync();
            category = await db.Categories.FirstAsync(o => o.Name == categoryName);
        }

        Question newQuestion = new();
        newQuestion.Text = question.question;
        newQuestion.DifficultyId = difficulty.DifficultyId;
        newQuestion.CategoryId = category.CategoryId;
        db.Questions.Add(newQuestion);

        newQuestion.Answers.Add(new() {
            IsCorrect = 1,
            Text = question.correct_answer
        });

        foreach (string incorrect in question.incorrect_answers) {
            newQuestion.Answers.Add(new() {
                IsCorrect = 0,
                Text = incorrect
            });
        }
        await db.SaveChangesAsync();

        instance.currentDailyQuestion = newQuestion;
        instance.currentDailyQuestionRetrieveTime = DateTime.UtcNow;
    }
    public static async Task<Question> CurrentDailyQuestion(TriviaContext db) {
        if (instance is null) {
            throw new Exception("API Client does not exist!");
        }
        if (instance.currentDailyQuestion is null) {
            await GenerateDailyQuestion(db);
            return instance.currentDailyQuestion;
        }
        DateTime now = DateTime.UtcNow;
        if (instance.currentDailyQuestionRetrieveTime.Day == now.Day) {
            return instance.currentDailyQuestion;
        }

        await GenerateDailyQuestion(db);
        return instance.currentDailyQuestion;
    }

    public static async Task<List<APICategory>> GetAllCategories() {
        string json = await JsonFromAPI(BaseURL + "/api_category.php");
        List<APICategory>? categories = JsonConvert.DeserializeObject<List<APICategory>>(json);
        if (categories is null) {
            throw new Exception("Couldn't convert categories from JSON!");
        }
        return categories;
    }

    private static async Task<string> JsonFromAPI(string url) {
        try {
            instance.client.Timeout = TimeSpan.FromSeconds(5);
            HttpResponseMessage response = await instance.client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        } catch (Exception ex) {
            throw new Exception("Couldn't download JSON from API: " + ex.Message);
        }
    }
}
