# 贡献指南

感谢你对本项目的关注！参与贡献前，请先阅读以下约定。

## 提交 Issue

- 提交前请先搜索是否已有相同或类似 Issue。
- Bug 反馈请包含：复现步骤、期望结果、实际结果、环境信息（.NET / Node 版本、操作系统）。
- 功能建议请说明使用场景与预期收益，避免只描述实现方式。

## 开发流程

1. Fork 仓库并克隆到本地。
2. 基于 `main` 创建分支：
   - 新功能：`feat/feature-name`
   - 缺陷修复：`fix/bug-name`
   - 文档改进：`docs/topic`
3. 本地开发与验证（见 [README](README.md#本地部署)）。
4. 提交 Pull Request，描述变更动机、实现要点与验证方式。

## 代码规范

### 后端（C# / .NET 8）

- 保持模块依赖方向：`WebApi → App → Im → Core`，`Plugin → Core`，禁止反向引用与循环依赖。
- 公共能力（实体基类、工具、异常、权限编码）放在 `Wms.Core`；业务编排放在 `Wms.App`；库存规则放在 `Wms.Im`；扩展能力放在 `Wms.Plugin`。
- 对外 DTO 与实体分离，请求模型使用数据注解做基础校验，业务校验抛 `BusinessException` / `NotFoundException` / `ConflictException`。
- 控制器方法必须返回 `ApiResult<T>`，并显式标注 `[HasPermission]`；查询类使用 `xxx:view`，写入类使用 `xxx:edit`，执行类使用 `xxx:execute`。
- 所有 public 成员补充 XML 注释；命名使用 PascalCase / camelCase，异步方法以 `Async` 结尾。
- 数据库结构变更需同步更新 `WmsDbContext` 配置与种子数据。

### 前端（Vue 2）

- 页面按模块放在 `src/views` 下，接口封装统一放在 `src/api`。
- 状态只在 Vuex 中维护登录态、权限与菜单；页面级数据保存在组件内。
- 所有请求通过 `src/api/request.js`，禁止在组件内直接使用 axios。
- 按钮与路由均需绑定权限编码，新增页面同时更新后端菜单种子数据与前端静态路由。
- 代码风格：2 空格缩进、单引号、无分号结尾可选但与同文件保持一致。

## 提交信息

采用 Conventional Commits：

```
feat(im): 新增库存锁定与释放能力
fix(app): 修复出库完成时实发数量未回写
docs: 补充插件开发说明
chore: 更新依赖版本
```

## 提交前检查清单

- [ ] `dotnet build backend/Wms.sln` 通过且无新增警告
- [ ] `npm run build`（frontend）通过
- [ ] 新增接口已在 Swagger 中验证，权限注解生效
- [ ] 新增页面已通过浏览器自测，无控制台报错
- [ ] 文档与注释同步更新
