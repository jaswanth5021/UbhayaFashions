using System.ComponentModel.DataAnnotations;

namespace LadiesDressStore.Web.Models;

public class ContactViewModel
{
    [Required(ErrorMessage = "Please enter your name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a subject")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your message")]
    public string Message { get; set; } = string.Empty;
}