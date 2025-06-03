using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkshopManager.Models;

namespace WorkshopManager.Controllers;

[Authorize(Roles = "Admin")]
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    // GET: Account/ManageUsers
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

    // POST: Account/AssignRole
    [HttpPost]
    [ValidateAntiForgeryToken]
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