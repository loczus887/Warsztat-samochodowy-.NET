using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Models;

namespace WorkshopManager.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // GET: Account/Login - DOSTĘPNE DLA WSZYSTKICH
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: Account/Login - DOSTĘPNE DLA WSZYSTKICH
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in.", email);
                    return LocalRedirect(returnUrl ?? "/");
                }
            }

            ModelState.AddModelError(string.Empty, "Nieprawidłowy email lub hasło.");
        }

        return View();
    }

    // GET: Account/Register - DOSTĘPNE DLA WSZYSTKICH
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: Account/Register - DOSTĘPNE DLA WSZYSTKICH
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string email, string password, string confirmPassword, string? firstName = null, string? lastName = null, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (password != confirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Hasła się nie zgadzają.");
            return View();
        }

        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Email} created a new account.", email);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl ?? "/");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View();
    }

    // POST: Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    // ZARZĄDZANIE UŻYTKOWNIKAMI - TYLKO DLA ADMINA
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ManageUsers()
    {
        try
        {
            var users = _userManager.Users.ToList();
            var userRoles = new Dictionary<string, IList<string>>();

            foreach (var user in users)
            {
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while loading user management page");
            TempData["ErrorMessage"] = "Wystąpił błąd podczas ładowania zarządzania użytkownikami.";
            return View(new List<ApplicationUser>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole(string userId, string role)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Użytkownik nie został znaleziony.";
                return RedirectToAction(nameof(ManageUsers));
            }

            // Usuń wszystkie istniejące role
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Dodaj nową rolę
            await _userManager.AddToRoleAsync(user, role);

            _logger.LogInformation("Role {Role} assigned to user {UserId}", role, userId);
            TempData["SuccessMessage"] = $"Rola {role} została przypisana użytkownikowi.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while assigning role {Role} to user {UserId}", role, userId);
            TempData["ErrorMessage"] = "Wystąpił błąd podczas przypisywania roli.";
        }

        return RedirectToAction(nameof(ManageUsers));
    }
}