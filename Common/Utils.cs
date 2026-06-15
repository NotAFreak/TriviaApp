#nullable enable
public static class Utils {
	public static string UpperFirstLetter(this string s) {
		if (string.IsNullOrEmpty(s))
			return string.Empty;

		char[] a = s.ToCharArray();
		a[0] = char.ToUpper(a[0]);
		return new string(a);
	}
}