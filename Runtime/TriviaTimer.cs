#nullable enable
using TriviaApp.Models;

namespace TriviaApp.Runtime;
public class UserTiming {
	public int questionId;
	public DateTime startTime = DateTime.UtcNow;
	public DateTime? finishTime = null;
}
public static class TriviaTimer {
	public static TimeSpan MaxDuration = TimeSpan.FromSeconds(32.371f);
	// UserID, Timings
	public static Dictionary<int, UserTiming> userTimings = new();
	
	public static void StartUser(int userId, int questionId) {
		UserTiming timing = new() {
			questionId = questionId
		};
		Assert.IsTrue(!userTimings.ContainsKey(userId));
		userTimings.Add(userId, timing);
	}

	public static UserTiming FinishUser(int userId) {
		Assert.IsTrue(userTimings.ContainsKey(userId));
		UserTiming timing = userTimings[userId];
		timing.finishTime = DateTime.UtcNow;
		userTimings.Remove(userId);
		return timing;
	}

	public static bool UserAlreadyExists(int userId) {
		return userTimings.ContainsKey(userId);
	}

	public static async Task CleanupExpired(TriviaContext db) {
		Queue<int> removeQueue = new();
		foreach (KeyValuePair<int, UserTiming> pair in userTimings) {
			Assert.IsNull(pair.Value.finishTime);
			DateTime now = DateTime.UtcNow;
			if ((now - pair.Value.startTime) > MaxDuration) {
				pair.Value.finishTime = now;
				removeQueue.Enqueue(pair.Key);
			}
		}
		while (removeQueue.Count > 0) {
			int id = removeQueue.Dequeue();
			UserTiming timing = userTimings[id];
			// they clearly failed
			await db.UserQuestions.AddAsync(new() {
				CompletionState = (int)CompletionState.OutOfTime,
				CompletionTime = (timing.finishTime - timing.startTime).Value.TotalSeconds
			});
			userTimings.Remove(id);
		}
		await db.SaveChangesAsync();
	}
}