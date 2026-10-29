using System.ComponentModel.DataAnnotations;

namespace Wms.App.Dtos;

/// <summary>登录请求</summary>
public class LoginRequest
{
    [Required(ErrorMessage = "请输入登录账号")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入登录密码")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>登录响应</summary>
public class LoginResult
{
    /// <summary>访问令牌</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>令牌过期时间</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>用户信息</summary>
    public UserProfileDto User { get; set; } = new();
}

/// <summary>当前用户信息</summary>
public class UserProfileDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    /// <summary>角色编码</summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>角色名称</summary>
    public List<string> RoleNames { get; set; } = new();

    /// <summary>权限编码</summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>侧边栏菜单树节点</summary>
public class MenuTreeDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public string? Permission { get; set; }
    public int Type { get; set; }
    public List<MenuTreeDto> Children { get; set; } = new();
}

/// <summary>修改密码请求</summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "请输入原密码")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入新密码")]
    [MinLength(6, ErrorMessage = "新密码长度不能少于 6 位")]
    public string NewPassword { get; set; } = string.Empty;
}
