using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using LawdeIA.Models;
using LawdeIA.Data;

public class AuthController : Controller
{
    private readonly LawdeIAContext _context;
    private readonly PasswordHasher<object> _passwordHasher;

    public AuthController(LawdeIAContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<object>();
    }

    // GET: /Auth/Login
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // POST: /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // Buscar usuario en la BD (Modelo)
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

            // Verificar si existe y contraseña es correcta
            if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Credenciales inválidas");
                return View(model);
            }

            // Actualizar último login
            user.LastLogin = DateTime.Now;
            await _context.SaveChangesAsync();

            // Crear la sesión (Cookie Authentication)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error durante el login: " + ex.Message);
            return View(model);
        }
    }

    // GET: /Auth/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // Verificar si el usuario ya existe
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "El usuario ya existe");
                return View(model);
            }

            // Crear nuevo usuario
            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                PasswordHash = HashPassword(model.Password),
                CreatedAt = DateTime.Now,
                Role = "User",
                IsActive = true
            };

            // Guardar en BD
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Auto-login después del registro
            var loginModel = new LoginViewModel
            {
                Email = model.Email,
                Password = model.Password,
                RememberMe = false
            };

            return await Login(loginModel);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error durante el registro: " + ex.Message);
            return View(model);
        }
    }

    // POST: /Auth/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth");
    }

    // Métodos helper para passwords
    private string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(null, password);
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(null, passwordHash, password);
        return result == PasswordVerificationResult.Success;
    }
}