using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ECommerceBoutique.Models;

namespace ECommerceBoutique.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public IndexModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }
        public string Email { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Le prénom est requis")]
            [Display(Name = "Prénom")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Le nom est requis")]
            [Display(Name = "Nom")]
            public string LastName { get; set; }

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

            [Display(Name = "Nom de l'entreprise")]
            public string CompanyName { get; set; }
        }

        private async Task LoadAsync(User user)
        {
            var userName = await _userManager.GetUserNameAsync(user);

            Username = userName;
            Email = await _userManager.GetEmailAsync(user);

            Input = new InputModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                City = user.City,
                PostalCode = user.PostalCode,
                Country = user.Country,
                CompanyName = user.CompanyName
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Impossible de charger l'utilisateur avec l'ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Impossible de charger l'utilisateur avec l'ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            user.FirstName = Input.FirstName;
            user.LastName = Input.LastName;
            user.PhoneNumber = Input.PhoneNumber;
            user.Address = Input.Address;
            user.City = Input.City;
            user.PostalCode = Input.PostalCode;
            user.Country = Input.Country;
            user.CompanyName = Input.CompanyName;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = "Votre profil a été mis à jour";
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadAsync(user);
            return Page();
        }
    }
} 