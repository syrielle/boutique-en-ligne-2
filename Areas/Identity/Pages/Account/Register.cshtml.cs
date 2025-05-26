using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceBoutique.Models;

namespace ECommerceBoutique.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Le prénom est requis")]
            [Display(Name = "Prénom")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Le nom est requis")]
            [Display(Name = "Nom")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "L'adresse email est requise")]
            [EmailAddress(ErrorMessage = "L'adresse email n'est pas valide")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Phone(ErrorMessage = "Le numéro de téléphone n'est pas valide")]
            [Display(Name = "Téléphone")]
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "L'adresse est requise")]
            [Display(Name = "Adresse")]
            public string Address { get; set; }

            [Required(ErrorMessage = "La ville est requise")]
            [Display(Name = "Ville")]
            public string City { get; set; }

            [Required(ErrorMessage = "Le code postal est requis")]
            [Display(Name = "Code postal")]
            public string PostalCode { get; set; }

            [Required(ErrorMessage = "Le pays est requis")]
            [Display(Name = "Pays")]
            public string Country { get; set; }

            [Required(ErrorMessage = "Le mot de passe est requis")]
            [StringLength(100, ErrorMessage = "Le {0} doit faire au moins {2} caractères et au maximum {1} caractères.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mot de passe")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmer le mot de passe")]
            [Compare("Password", ErrorMessage = "Le mot de passe et sa confirmation ne correspondent pas.")]
            public string ConfirmPassword { get; set; }

            [Display(Name = "Je souhaite devenir vendeur")]
            public bool IsVendor { get; set; }

            [Display(Name = "Nom de l'entreprise")]
            public string? CompanyName { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            _logger.LogInformation("Début du processus d'inscription");
            
            // Supprimer la validation du CompanyName si l'utilisateur n'est pas un vendeur
            if (!Input.IsVendor)
            {
                ModelState.Remove("Input.CompanyName");
            }
            else if (string.IsNullOrWhiteSpace(Input.CompanyName))
            {
                ModelState.AddModelError("Input.CompanyName", "Le nom de l'entreprise est requis pour les vendeurs");
            }
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Le modèle n'est pas valide : {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Page();
            }

            try
            {
                var user = new User
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    PhoneNumber = Input.PhoneNumber,
                    Address = Input.Address,
                    City = Input.City,
                    PostalCode = Input.PostalCode,
                    Country = Input.Country,
                    CompanyName = Input.IsVendor ? Input.CompanyName : null,
                    IsVendor = Input.IsVendor,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Tentative de création de l'utilisateur : {Email}", Input.Email);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Utilisateur créé avec succès : {Email}", Input.Email);

                    // Créer le rôle approprié s'il n'existe pas
                    var roleName = Input.IsVendor ? "Vendor" : "Customer";
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        _logger.LogInformation("Création du rôle : {Role}", roleName);
                        await _roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                    
                    // Attribuer le rôle à l'utilisateur
                    _logger.LogInformation("Attribution du rôle {Role} à l'utilisateur {Email}", roleName, Input.Email);
                    var roleResult = await _userManager.AddToRoleAsync(user, roleName);
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError("Erreur lors de l'attribution du rôle {Role} à l'utilisateur {Email}: {Errors}", 
                            roleName, Input.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                        ModelState.AddModelError(string.Empty, "Erreur lors de l'attribution du rôle. Veuillez réessayer.");
                        return Page();
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("Utilisateur connecté avec succès : {Email}", Input.Email);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogError("Erreur lors de la création de l'utilisateur : {Error}", error.Description);
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de l'inscription");
                ModelState.AddModelError(string.Empty, $"Une erreur inattendue s'est produite : {ex.Message}");
                _logger.LogError("Stack trace : {StackTrace}", ex.StackTrace);
            }

            return Page();
        }
    }
} 