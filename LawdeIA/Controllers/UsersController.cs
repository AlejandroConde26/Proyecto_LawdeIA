using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using LawdeIA.Models;
using LawdeIA.Data;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace LawdeIA.Controllers
{
    public class UsersController : Controller
    {
        private readonly LawdeIAContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(LawdeIAContext context, ILogger<UsersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            _logger.LogInformation("=== INICIO LOGIN ===");
            _logger.LogInformation($"Email recibido: {model.Email}");
            _logger.LogInformation($"ModelState válido: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState inválido");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning($"Error: {error.ErrorMessage}");
                }
                return View(model);
            }

            try
            {
                _logger.LogInformation("Buscando usuario en BD...");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

                if (user == null)
                {
                    _logger.LogWarning($"Usuario no encontrado: {model.Email}");

                    // Debug: Listar todos los usuarios
                    var allUsers = await _context.Users.ToListAsync();
                    _logger.LogInformation($"Total usuarios en BD: {allUsers.Count}");
                    foreach (var u in allUsers)
                    {
                        _logger.LogInformation($"Usuario BD: {u.Email} | Activo: {u.IsActive}");
                    }

                    ModelState.AddModelError("", "❌ Usuario no encontrado");
                    return View(model);
                }

                _logger.LogInformation($"Usuario encontrado: {user.Email}");

                var hashedInputPassword = HashPassword(model.Password);
                _logger.LogInformation($"Hash entrada: {hashedInputPassword.Substring(0, 10)}...");
                _logger.LogInformation($"Hash BD: {user.PasswordHash.Substring(0, 10)}...");

                if (hashedInputPassword != user.PasswordHash)
                {
                    _logger.LogWarning("Contraseña incorrecta");
                    ModelState.AddModelError("", "❌ Contraseña incorrecta");
                    return View(model);
                }

                // Login exitoso
                _logger.LogInformation("✅ Contraseña correcta, guardando sesión...");

                user.LastLogin = DateTime.Now;
                await _context.SaveChangesAsync();

                HttpContext.Session.SetInt32("UserID", user.UserID);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Email", user.Email);

                // Verificar que se guardó
                var savedUserID = HttpContext.Session.GetInt32("UserID");
                _logger.LogInformation($"UserID guardado en sesión: {savedUserID}");

                _logger.LogInformation("Redirigiendo a Chat...");
                return RedirectToAction("Chat", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError($"💥 Error en Login: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"💥 Error: {ex.Message}");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            _logger.LogInformation("=== INICIO REGISTRO ===");
            _logger.LogInformation($"Username: {model.Username}, Email: {model.Email}");
            _logger.LogInformation($"ModelState válido: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState inválido en registro");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning($"Error validación: {error.ErrorMessage}");
                }
                return View(model);
            }

            try
            {
                _logger.LogInformation("Verificando duplicados...");

                // Verificar duplicados
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    _logger.LogWarning($"Email duplicado: {model.Email}");
                    ModelState.AddModelError("Email", "❌ Este email ya está registrado");
                    return View(model);
                }

                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    _logger.LogWarning($"Username duplicado: {model.Username}");
                    ModelState.AddModelError("Username", "❌ Este usuario ya existe");
                    return View(model);
                }

                _logger.LogInformation("Creando nuevo usuario...");

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),
                    FullName = $"{model.FirstName} {model.LastName}",
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    Role = "User"
                };

                _logger.LogInformation($"Usuario a guardar: {user.Username} | {user.Email}");

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Usuario guardado con ID: {user.UserID}");

                // Auto-login
                HttpContext.Session.SetInt32("UserID", user.UserID);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Email", user.Email);

                var savedUserID = HttpContext.Session.GetInt32("UserID");
                _logger.LogInformation($"UserID guardado en sesión: {savedUserID}");

                _logger.LogInformation("Redirigiendo a Chat...");
                return RedirectToAction("Chat", "Home");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError($"💥 Error BD: {dbEx.Message}");
                _logger.LogError($"Inner Exception: {dbEx.InnerException?.Message}");
                _logger.LogError($"Stack Trace: {dbEx.StackTrace}");

                var errorMsg = "Error al guardar en la base de datos";
                if (dbEx.InnerException != null)
                {
                    errorMsg += $": {dbEx.InnerException.Message}";

                    if (dbEx.InnerException.Message.Contains("IDENTITY_INSERT"))
                    {
                        errorMsg += " - El problema puede ser con la columna IDENTITY";
                    }
                    if (dbEx.InnerException.Message.Contains("null"))
                    {
                        errorMsg += " - Hay campos requeridos vacíos";
                    }
                }

                ModelState.AddModelError("", $"💥 {errorMsg}");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"💥 Error inesperado: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"💥 Error inesperado: {ex.Message}");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Método de prueba para verificar conexión a BD
        [HttpGet]
        public async Task<IActionResult> TestDB()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var userCount = await _context.Users.CountAsync();

                return Content($"✅ Conexión BD: {canConnect}\n📊 Total usuarios: {userCount}");
            }
            catch (Exception ex)
            {
                return Content($"❌ Error: {ex.Message}");
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Email requerido")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Contraseña requerida")]
        public string Password { get; set; }
    }

    public class RegisterModel
    {
        [Required(ErrorMessage = "Usuario requerido")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email requerido")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Nombre requerido")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Apellido requerido")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Contraseña requerida")]
        [MinLength(6)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirmar contraseña")]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}