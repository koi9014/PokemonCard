using System.ComponentModel.DataAnnotations;

namespace PokemonCard.Models;

public class AdminLoginViewModel
{
    [Required(ErrorMessage = "請輸入管理員帳號")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}
