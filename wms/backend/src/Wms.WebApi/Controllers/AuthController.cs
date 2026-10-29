using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wms.App.Dtos;
using Wms.App.Services;
using Wms.Core.Common;

namespace Wms.WebApi.Controllers;

/// <summary>
/// 认证相关接口：登录、获取当前用户信息、侧边栏菜单与修改密码。
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly MenuService _menuService;
    private readonly ICurrentUser _currentUser;

    public AuthController(AuthService authService, MenuService menuService, ICurrentUser currentUser)
    {
        _authService = authService;
        _menuService = menuService;
        _currentUser = currentUser;
    }

    /// <summary>账号密码登录，成功返回 JWT 令牌与用户信息</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ApiResult<LoginResult>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        => ApiResult<LoginResult>.Ok(await _authService.LoginAsync(request, cancellationToken), "登录成功");

    /// <summary>获取当前登录用户信息与权限集合</summary>
    [HttpGet("profile")]
    public async Task<ApiResult<UserProfileDto>> Profile(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
                     ?? throw new BusinessException("登录状态已失效，请重新登录", ErrorCodes.Unauthorized);
        return ApiResult<UserProfileDto>.Ok(await _authService.GetProfileAsync(userId, cancellationToken));
    }

    /// <summary>获取当前用户可见的侧边栏菜单树</summary>
    [HttpGet("menus")]
    public async Task<ApiResult<List<MenuTreeDto>>> Menus(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
                     ?? throw new BusinessException("登录状态已失效，请重新登录", ErrorCodes.Unauthorized);
        return ApiResult<List<MenuTreeDto>>.Ok(await _menuService.GetUserMenuTreeAsync(userId, cancellationToken));
    }

    /// <summary>修改当前登录用户密码</summary>
    [HttpPost("change-password")]
    public async Task<ApiResult<object>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
                     ?? throw new BusinessException("登录状态已失效，请重新登录", ErrorCodes.Unauthorized);
        await _authService.ChangePasswordAsync(userId, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "密码修改成功");
    }
}
