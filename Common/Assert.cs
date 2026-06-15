#nullable enable
using System.Security.Cryptography;

public class AssertionException : Exception {
	public AssertionException() { }
	public AssertionException(string message) : base(message) { }
	public AssertionException(string message, Exception inner) : base(message, inner) { }
}
public static class Assert {
	public static void IsTrue(bool condition) {
		#if DEBUG
		if (!condition) {
			throw new AssertionException("Condition not true");
		}
		#endif
	}

	public static void IsNull(object? data) {
		#if DEBUG
		if (data is not null) {
			throw new AssertionException(data.ToString() + " is not null");
		}
		#endif
	}
	public static void IsNotNull(object? data) {
		#if DEBUG
		if (data is  null) {
			throw new AssertionException(data.ToString() + " is null");
		}
		#endif
	}
}