using System.ComponentModel.DataAnnotations;
namespace JobForFresher.Models;
public class ChangePasswordInput
{
    [Required, DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
    [Required, StringLength(128, MinimumLength = 12), DataType(DataType.Password)] public string NewPassword { get; set; } = "";
    [Required, Compare(nameof(NewPassword)), DataType(DataType.Password)] public string ConfirmPassword { get; set; } = "";
}