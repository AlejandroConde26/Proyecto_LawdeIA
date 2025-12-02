using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using LawdeIA.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LawdeIA.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Si el usuario ya está autenticado, redirigir al chat
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Chat");
            }
            return View();
        }

        [Authorize] // ✅ Requiere autenticación
        public IActionResult Chat()
        {
            // Ya no necesitas verificar Session, [Authorize] se encarga
            // Los datos del usuario están en User.Identity y Claims

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            ViewBag.UserID = userId;
            ViewBag.Username = username;
            ViewBag.Email = email;

            return View();
        }

        [Authorize] // ✅ Solo usuarios autenticados
        [HttpGet]
        public IActionResult DebugSession()
        {
            var userInfo = $"Usuario Autenticado: {User.Identity.IsAuthenticated}\n";
            userInfo += $"Nombre: {User.Identity.Name}\n";
            userInfo += $"UserID: {User.FindFirst(ClaimTypes.NameIdentifier)?.Value}\n";
            userInfo += $"Email: {User.FindFirst(ClaimTypes.Email)?.Value}\n";
            userInfo += $"Rol: {User.FindFirst(ClaimTypes.Role)?.Value}\n";

            // Información de Session (si aún la usas para algo)
            userInfo += $"Session ID: {HttpContext.Session.Id}\n";
            userInfo += $"Session UserID: {HttpContext.Session.GetInt32("UserID")}\n";

            return Content(userInfo);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}