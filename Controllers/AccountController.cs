using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TriviaApp.Models;

namespace TriviaApp.Controllers;

public class AccountController : Controller {
    private TriviaContext db;

    public AccountController(TriviaContext db) {
        this.db = db;
    }
    public async Task AssignCookie(User user) {
        List<Claim> claims = [
            new("userid", user.UserId.ToString()),
            new("username", user.Username),
            new("password", user.Password),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, ((Role)user.Role).ToString())
        ];

        ClaimsIdentity claimIdentities = new (claims, CookieAuthenticationDefaults.AuthenticationScheme);
        ClaimsPrincipal claimPrinciples = new ClaimsPrincipal(claimIdentities);
        HttpContext authenticationManager = Request.HttpContext;
        authenticationManager.User = claimPrinciples;

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimIdentities)
        );
    }
    public string HashPassword(string extraSalt, string password) {
        byte[] salt = Encoding.UTF8.GetBytes(":3 jvnko salty :3" + extraSalt);
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password, salt,
            KeyDerivationPrf.HMACSHA512,
            16,
            256 / 8
        ));
        return hashed;
    }
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index() {
        return View();
    }
    [HttpGet]
    public async Task<IActionResult> Login() {
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> LoginPost(string email, string password) {
        if (string.IsNullOrWhiteSpace(password)) {
            TempData["Error"] = "Password required!";
            return RedirectToAction("Login", "Account");
        }
        User? user = await db.Users.FirstOrDefaultAsync(o=>o.Email == email);
        
        if (user is null) {
            TempData["Error"] = "User with email not found!";
            return RedirectToAction("Login", "Account");
        }

        if (user.Password != HashPassword(user.Email, password)) {
            TempData["Error"] = "Incorrect Password!";
            return RedirectToAction("Login", "Account");
        }

        await HttpContext.SignOutAsync();
        // Finished verifying, now log in create them
        await AssignCookie(user);
        await db.SaveChangesAsync();
        return RedirectToAction("Index", "Home");
    }
    [HttpGet]
    public async Task<IActionResult> Signup() {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateInfoPost(string? username, string? email, string? password, string confirmPassword) {
        // Verify the user
        Claim? idClaim = HttpContext.User.FindFirst("userid");
        Claim? passwordClaim = HttpContext.User.FindFirst("password");
        if (idClaim is null || passwordClaim is null) {
            TempData["Error"] = "Couldn't verify cookies! Logged out";
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
        User? user = await db.Users.FirstOrDefaultAsync(o=>o.UserId == Convert.ToInt32(idClaim.Value));
        if (user is null) {
            TempData["Error"] = "User does not exist! Logged out";
            await HttpContext.SignOutAsync();
            return RedirectToAction("Signup", "Account");
        }
        if (user.Password != passwordClaim.Value.ToString()) {
            TempData["Error"] = "Password not correct! Logged out";
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Account");
        }
        if (user.Password != HashPassword(user.Email, confirmPassword)) {
            TempData["Error"] = "Confirmation password is not correct!";
            return RedirectToAction("Index", "Account");
        }
        
        // Actually update info now
        if (email is not null) {
            user.Email = email;
        }

        if (username is not null) {
            user.Username = username;
        }

        if (password is not null) {
            user.Password = HashPassword(user.Email, password);
        }
        db.Users.Update(user);
        await db.SaveChangesAsync();
        await AssignCookie(user);
        return RedirectToAction("Index", "Account");
    }
    [HttpPost]
    public async Task<IActionResult> SignupPost(string email, string username, string password) {
        if (string.IsNullOrWhiteSpace(email.Trim())) {
            TempData["Error"] = "Email required!";
            return RedirectToAction("Signup", "Account");
        }
        if (string.IsNullOrWhiteSpace(password)) {
            TempData["Error"] = "Password required!";
            return RedirectToAction("Signup", "Account");
        }
        if (!Regex.IsMatch(email, "^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$")) {
            TempData["Error"] = "Email not valid!";
            return RedirectToAction("Signup", "Account");
        }

        if (username.Length > 16) {
            TempData["Error"] = "Username needs to be less than or equal to 16 characters long!";
            return RedirectToAction("Signup", "Account");
        }
        if (password.Length is < 6 or > 32) {
            TempData["Error"] = "Password needs to be 6 or more, and less than 32 characters long!";
            return RedirectToAction("Signup", "Account");
        }
        if (await db.Users.FirstOrDefaultAsync(o => o.Email == email) != null) {
            TempData["Error"] = "User with that Email already exists!";
            return RedirectToAction("Signup", "Account");
        }
        if (await db.Users.FirstOrDefaultAsync(o => o.Username == username) != null) {
            TempData["Error"] = "User with that Username already exists!";
            return RedirectToAction("Signup", "Account");
        }
        await HttpContext.SignOutAsync();
        // Finished verifying, now actually create them
        User newUser = new() {
            Email = email,
            Username = username,
            Password = HashPassword(email, password),
        };
        db.Users.Add(newUser);
        await AssignCookie(newUser);
        await db.SaveChangesAsync();
        return RedirectToAction("Index", "Home");
    }
    [HttpPost]
    public async Task<IActionResult> DeleteAccountPost(string password) {
        Claim? idClaim = HttpContext.User.FindFirst("userid");
        Claim? passwordClaim = HttpContext.User.FindFirst("password");
        if (idClaim is null || passwordClaim is null) {
            TempData["Error"] = "Couldn't verify cookies! Logged out";
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
        User? user = await db.Users.FirstOrDefaultAsync(o=>o.UserId == Convert.ToInt32(idClaim.Value));
        if (user is null) {
            TempData["Error"] = "User does not exist! Logged out";
            await HttpContext.SignOutAsync();
            return RedirectToAction("Signup", "Account");
        }
        if (user.Password != passwordClaim.Value.ToString()) {
            TempData["Error"] = "Password not correct! Logged out";
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Account");
        }
        if (user.Password != HashPassword(user.Email, password)) {
            TempData["Error"] = "Confirmation password is not correct!";
            return RedirectToAction("Index", "Account");
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
    [HttpPost]
    public async Task<IActionResult> LogoutPost(string email, string username, string password) {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
