using System.ComponentModel.DataAnnotations;

namespace ECommerceBoutique.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "L'adresse email est requise")]
        [EmailAddress(ErrorMessage = "Adresse email invalide")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [DataType(DataType.Password)]
        [Display(Name = "Mot de passe")]
        public string Password { get; set; }

        [Display(Name = "Se souvenir de moi")]
        public bool RememberMe { get; set; }
    }
} 