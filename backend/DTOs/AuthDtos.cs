namespace backend.DTOs;


// =====================================================
// SIGNUP
// =====================================================

public record SignUpRequest(
    string Name,
    string Email,
    string Mobile,
    string Password,
    int Age);

public record VerifySignupEmailRequest(string Email, string Code);
public record ResendSignupOtpRequest(string Email);
public record VerifyMyEmailRequest(string Code);


// =====================================================
// LOGIN
// =====================================================

public record LoginRequest(
    string Identifier,
    string Password);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);


// =====================================================
// GOOGLE
// =====================================================

public record GoogleTokenRequest(
    string IdToken);


// =====================================================
// LOGIN RESPONSE
// =====================================================

public record LoginResponse(
    string Token,
    int CustomerId,
    string Name,
    string? Email,
    int? Age,
    string? ProfileImage);


// =====================================================
// UPDATE PROFILE
// =====================================================

public record UpdateProfileRequest(
    string Name,
    string? Mobile,
    int? Age,
    DateTime? DateOfBirth,
    string? Gender);


// =====================================================
// PROFILE RESPONSE
// =====================================================

public record ProfileResponse(
    int Id,
    string Name,
    string? Email,
    string? Mobile,
    int? Age,
    string? ProfileImage,
    DateTime? DateOfBirth,
    string? Gender,
    bool EmailVerified);

