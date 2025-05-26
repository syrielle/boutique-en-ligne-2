using System.ComponentModel.DataAnnotations;

namespace ECommerceBoutique.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "L'adresse email est requise")]
        [EmailAddress(ErrorMessage = "Adresse email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [StringLength(100, ErrorMessage = "Le {0} doit avoir au moins {2} caractères.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmer le mot de passe")]
        [Compare("Password", ErrorMessage = "Le mot de passe et sa confirmation ne correspondent pas.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Le prénom est requis")]
        [Display(Name = "Prénom")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [Display(Name = "Nom")]
        public string LastName { get; set; }

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

        [Display(Name = "Je souhaite devenir vendeur")]
        public bool IsVendor { get; set; }
    }
} 