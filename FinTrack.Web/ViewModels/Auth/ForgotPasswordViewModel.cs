
using System.ComponentModel.DataAnnotations;

namespace FinTrack.Web.ViewModels.Auth;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
