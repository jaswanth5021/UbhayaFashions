using System.ComponentModel.DataAnnotations;

namespace LadiesDressStore.Web.Models;


// =====================================================
// LOGIN
// =====================================================

public class LoginViewModel
{
    [Required]
    [Display(Name = "Email or mobile number")]
    public string Identifier { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

// Shared model for the sign-in and animated sign-up panels on Account/Login.
public class LoginExperienceViewModel
{
    public LoginViewModel Login { get; set; } = new();
    public SignupViewModel Signup { get; set; } = new();
}


// =====================================================
// SIGNUP
// =====================================================

public class SignupViewModel : IValidatableObject
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [Phone]
    [Display(Name = "Mobile number")]
    public string Mobile { get; set; } = "";

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime? DateOfBirth { get; set; }

    // Keep sending age for compatibility with the current signup API contract.
    public int Age => DateOfBirth is DateTime dateOfBirth
        ? CalculateAge(dateOfBirth, DateTime.Today)
        : 0;

    [Required]
    [MinLength(6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required]
    [Compare(nameof(Password))]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateOfBirth is not DateTime dateOfBirth) yield break;

        var age = CalculateAge(dateOfBirth, DateTime.Today);
        if (dateOfBirth.Date > DateTime.Today || age < 18 || age > 120)
            yield return new ValidationResult("You must be between 18 and 120 years old.", [nameof(DateOfBirth)]);
    }

    private static int CalculateAge(DateTime dateOfBirth, DateTime today)
    {
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}

public class VerifySignupEmailViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, RegularExpression("^[0-9]{6}$", ErrorMessage = "Enter the 6-digit code from your email.")]
    [Display(Name = "Verification code")]
    public string Code { get; set; } = "";
}


// =====================================================
// LOGIN RESPONSE
// =====================================================

public class AuthResponse
{
    public string Token { get; set; } = "";

    public int CustomerId { get; set; }

    public string Name { get; set; } = "";

    public string? Email { get; set; }

    public int? Age { get; set; }

    public string? ProfileImage { get; set; }

}


// =====================================================
// PROFILE
// =====================================================

public class ProfileViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [EmailAddress]
    public string? Email { get; set; }

    public string? Mobile { get; set; }

    [Range(18, 120)]
    public int? Age { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [StringLength(30)]
    public string? Gender { get; set; }

    public string? ProfileImage { get; set; }

    public bool EmailVerified { get; set; }
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = "";

    [Required, MinLength(8), DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
}

public class ResetPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Token { get; set; } = "";

    [Required, MinLength(8), DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class AdminChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}

public class AdminForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";
}

public class AdminResetPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Token { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
}
