# WMS 工厂仓储管理系统

一个前后端分离、模块化分层、可直接二次开发的工厂仓储管理系统（WMS）。后端基于 .NET 8 WebAPI，采用 `core / app / im / plugin / webapi` 五模块分层架构；前端基于 Vue 2.x + Element UI + Vuex + Vue Router，提供响应式单页管理界面。

系统覆盖工厂 WMS 的核心业务闭环：基础资料 → 仓库配置 → 入库 → 出库 → 盘点 → 调拨 → 库存台账与流水 → 报表统计 → 系统权限管理。

> 仓库内置 SQLite 演示数据库与种子数据，首次启动即可完整体验全部流程，无需额外安装数据库。

## 目录

- [功能特性](#功能特性)
- [技术栈](#技术栈)
- [系统架构](#系统架构)
- [项目结构](#项目结构)
- [本地部署](#本地部署)
- [使用说明](#使用说明)
- [接口与权限](#接口与权限)
- [插件开发](#插件开发)
- [配置说明](#配置说明)
- [贡献指南](#贡献指南)
- [开源协议](#开源协议)

## 功能特性

| 模块 | 能力 |
| --- | --- |
| 基础资料管理 | 物料分类（支持两级）、物料主数据（编码/规格/单位/安全库存/参考单价）、供应商与客户档案 |
| 仓库配置 | 多仓库管理、库位管理（库区、库位类型、容量），仓库与库位级联 |
| 入库作业 | 采购/生产/退货/其他入库，草稿 → 确认 → 执行，执行时自动增加库存并写库存流水 |
| 出库作业 | 销售/生产领料/其他出库，确认时锁定库存、执行时扣减库存，取消自动释放锁定 |
| 库存盘点 | 按仓库自动生成账面快照，录入实盘数量，完成后按差异自动调整库存 |
| 库存调拨 | 跨仓库调拨，调出扣减 + 调入增加，同一事务内完成并生成两条流水 |
| 库存管理 | 库存台账（在库/锁定/可用）、库存流水（8 种类型）、仓库汇总、低于安全库存预警 |
| 报表统计 | 工作台概览、出入库趋势、库存金额分类分布、库存汇总报表、收发存报表、出库 TOP 物料 |
| 系统权限管理 | 用户、角色、菜单、接口权限（RBAC + 动态权限策略）、操作日志、插件管理 |
| 插件机制 | 内置库存预警、Webhook 通知、ERP 数据同步插件，支持反射加载外部 DLL 插件 |

其他特性：

- JWT 认证 + 动态权限策略，前后端菜单与按钮级权限双重校验
- 统一响应结构、统一异常处理、统一 401/403 输出、模型校验统一提示
- 全局逻辑删除、审计字段自动填充、操作日志自动记录
- 响应式布局，适配桌面与平板；工作台 ECharts 可视化

## 技术栈

### 后端

| 组件 | 版本 | 说明 |
| --- | --- | --- |
| .NET | 8.0 | ASP.NET Core WebAPI |
| EF Core | 8.0.8 | ORM，SQLite 提供程序 |
| SQLite | - | 零配置演示数据库，可平滑替换为 SQL Server / PostgreSQL / MySQL |
| JWT | 8.0.x | `System.IdentityModel.Tokens.Jwt` + `JwtBearer` |
| Swagger | 6.6.2 | Swashbuckle 在线接口文档 |
| PBKDF2-SHA256 | - | 10 万次迭代密码散列，内置实现无额外依赖 |

### 前端

| 组件 | 版本 | 说明 |
| --- | --- | --- |
| Vue | 2.7.16 | 渐进式框架（Vue 2 系列最终版） |
| Element UI | 2.15.14 | 桌面端组件库 |
| Vuex | 3.6.2 | 登录态、权限、菜单集中管理 |
| Vue Router | 3.6.5 | Hash 路由 + 权限守卫 |
| Axios | 1.6.x | 统一拦截器与错误处理 |
| ECharts | 5.5.x | 工作台与报表图表 |
| Vue CLI | 5.0.8 | 构建与开发服务器 |

## 系统架构

```
┌──────────────────────────── 前端 SPA (Vue 2 + Element UI) ────────────────────────────┐
│  登录页 → 工作台 → 基础资料 / 仓库 / 入库 / 出库 / 库存 / 盘点 / 调拨 / 报表 / 系统管理  │
└───────────────────────────────────────┬──────────────────────────────────────────────┘
                                        │  HTTP / JSON（JWT Bearer）
┌───────────────────────────────────────▼──────────────────────────────────────────────┐
│ Wms.WebApi  控制器 · 中间件 · 鉴权策略 · Swagger · 统一响应/异常/日志                    │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ Wms.App     业务服务 · DTO · EF Core 持久化 · RBAC · 种子数据 · 报表聚合                │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ Wms.Im      库存领域模型 · 库存引擎（占用/锁定/发运/调整）· 库存流水                     │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ Wms.Plugin  插件契约 · 插件管理器 · 内置插件 · 外部 DLL 动态加载                         │
├──────────────────────────────────────────────────────────────────────────────────────┤
│ Wms.Core    公共实体 · 枚举 · 统一结果 · 异常 · JWT/密码 · 订单号 · 权限编码             │
└──────────────────────────────────────────────────────────────────────────────────────┘
```

**模块依赖方向（单向，无循环）：**

```
WebApi ──> App ──> Im ──> Core
   │        └──> Plugin ──> Core
   └──> Im / Plugin / Core
```

`Wms.Im` 只定义库存领域模型与 `IInventoryRepository` 接口，由 `Wms.App` 提供 EF Core 实现并注入，从而保证库存逻辑内聚、依赖方向清晰。

## 项目结构

```
wms/
├── backend/
│   ├── Wms.sln
│   └── src/
│       ├── Wms.Core/                  # 核心基础层
│       │   ├── Common/                # 实体基类、枚举、统一响应、异常、权限编码
│       │   ├── Entities/              # 公共主数据实体（物料/供应商/仓库/库位等）
│       │   ├── Security/              # JWT 签发、PBKDF2 密码散列
│       │   └── Utils/                 # 单号生成、日期区间
│       ├── Wms.Im/                    # 库存核心域
│       │   ├── Entities/              # 库存台账、库存流水
│       │   ├── InventoryManager.cs    # 库存引擎（增加/锁定/释放/发运/调拨/调整）
│       │   └── IInventoryRepository.cs
│       ├── Wms.Plugin/                # 插件机制
│       │   ├── BuiltIn/               # 库存预警、Webhook 通知、ERP 同步
│       │   ├── PluginManager.cs       # 内置注册 + 外部 DLL 反射加载
│       │   └── IWmsPlugin.cs
│       ├── Wms.App/                   # 应用业务层
│       │   ├── Data/                  # DbContext、仓储实现、种子数据
│       │   ├── Dtos/                  # 请求/响应模型
│       │   ├── Entities/              # 用户/角色/权限/菜单、各类业务单据
│       │   └── Services/              # 认证、主数据、仓库、单据、库存、报表、插件
│       └── Wms.WebApi/                # 接口宿主
│           ├── Authorization/         # 动态权限策略与处理器
│           ├── Controllers/           # 全部 REST 控制器
│           ├── Middleware/            # 异常处理、操作日志
│           ├── Infrastructure/        # 当前登录用户
│           └── Program.cs
├── frontend/
│   ├── public/
│   ├── src/
│   │   ├── api/                       # 按模块拆分的接口封装
│   │   ├── assets/                    # 全局样式
│   │   ├── layout/                    # 布局与动态侧边栏
│   │   ├── router/                    # 静态路由 + 权限守卫
│   │   ├── store/                     # Vuex（用户/应用）
│   │   └── views/                     # 业务页面
│   ├── package.json
│   └── vue.config.js
├── scripts/                           # 本地快速启动脚本
├── .gitignore
├── LICENSE
└── README.md
```

## 本地部署

### 1. 环境依赖

| 依赖 | 最低版本 | 校验命令 |
| --- | --- | --- |
| .NET SDK | 8.0 | `dotnet --version` |
| Node.js | 18.x（推荐 20 LTS+） | `node -v` |
| npm | 9.x | `npm -v` |
| Git | 2.x | `git --version` |

无需安装数据库：后端首次启动会自动创建 `wms.db` 并写入演示数据。

数据库文件默认生成在 `backend/src/Wms.WebApi/` 目录下（即应用工作目录）。

### 2. 一条命令启动（Windows PowerShell）

```powershell
# 在仓库根目录执行，会分别打开后端与前端窗口
powershell -ExecutionPolicy Bypass -File scripts/start-all.ps1
```

也可以分别启动：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start-backend.ps1
powershell -ExecutionPolicy Bypass -File scripts/start-frontend.ps1
```

macOS / Linux：

```bash
chmod +x scripts/*.sh
./scripts/start-backend.sh    # 终端 1
./scripts/start-frontend.sh   # 终端 2
```

### 3. 手动启动后端

```bash
cd backend
dotnet restore
dotnet run --project src/Wms.WebApi/Wms.WebApi.csproj
```

启动成功后：

- 接口地址：<http://localhost:5080>
- Swagger 文档：<http://localhost:5080/swagger>
- 健康检查：<http://localhost:5080/api/health>（无需登录）

### 4. 手动启动前端

```bash
cd frontend
npm install          # 首次执行；国内网络可先 npm config set registry https://registry.npmmirror.com
npm run serve
```

浏览器访问 <http://localhost:8080>。开发服务器已配置 `/api` 代理到 `http://localhost:5080`，无需额外处理跨域。

### 5. 生产构建

```bash
cd frontend
npm run build         # 产物位于 frontend/dist
```

将 `dist` 交由 Nginx 等静态服务器托管，并把 `/api` 反向代理到后端；后端也可用 `dotnet publish -c Release` 发布。

## 使用说明

### 演示账号

| 账号 | 密码 | 角色 | 权限范围 |
| --- | --- | --- | --- |
| `admin` | `admin123` | 系统管理员 | 全部权限（含系统管理） |
| `keeper` | `keeper123` | 仓管员 | 基础资料、仓库、入库/出库/盘点/调拨执行、库存与报表 |
| `viewer` | `viewer123` | 只读用户 | 仅查询类权限 |

> 首次登录后建议在右上角「修改密码」中更换密码。生产环境务必修改 `appsettings.json` 中的 `Jwt.Secret`。

### 典型业务闭环

1. **维护基础资料**：物料分类 → 物料（设置安全库存）→ 供应商/客户。
2. **配置仓库**：新增仓库 → 为仓库新增库位。
3. **入库**：新建入库单（草稿）→ 确认 → 执行入库（库存增加，生成入库流水）。
4. **出库**：新建出库单（草稿）→ 确认（锁定库存）→ 执行出库（扣减库存）→ 结束后库存与流水同步更新。
5. **调拨**：选择调出/调入仓库与库位 → 执行调拨（调出扣减、调入增加）。
6. **盘点**：选择仓库生成盘点单 → 录入实盘数量 → 完成盘点（按差异调整库存）。
7. **查询分析**：库存台账、库存流水、库存预警、库存汇总报表、收发存报表、工作台趋势图。
8. **系统管理**：用户/角色授权、菜单维护、操作日志审计、插件启停与执行。

### 页面说明

- **工作台**：物料数、库存总量/金额、预警数、今日单据、近 7 天趋势、分类分布、最近流水。
- **库存预警**：按「物料 + 仓库」汇总，低于安全库存时显示缺口数量。
- **操作日志**：所有非 GET 的 `/api` 请求自动记录操作人、模块、路径、耗时与结果。

## 接口与权限

### 响应结构

所有接口统一返回：

```json
{
  "success": true,
  "code": 200,
  "message": "登录成功",
  "data": {},
  "traceId": "…"
}
```

失败时 `success = false`，`code` 为业务错误码（4000 参数错误、4010 未登录、4030 无权限、4040 不存在、4090 冲突等）。

### 权限模型

- 权限编码形如 `material:view`、`inbound:execute`、`system:user:edit`，集中在 `Wms.Core/Common/PermissionCodes.cs`。
- 控制器使用 `[HasPermission("xxx")]` 注解，由 `PermissionPolicyProvider` 动态构建策略，`PermissionAuthorizationHandler` 校验。
- 管理员通配权限 `*`；角色的权限与菜单授权在「角色权限」页面维护。
- 前端路由 `meta.permission` + `v-permission` 指令实现页面与按钮级控制。

### 主要接口分组

| 分组 | 路径前缀 |
| --- | --- |
| 认证 | `/api/auth`（login / profile / menus / change-password） |
| 系统 | `/api/users`、`/api/roles`、`/api/menus`、`/api/logs` |
| 基础资料 | `/api/material-categories`、`/api/materials`、`/api/suppliers`、`/api/customers` |
| 仓库 | `/api/warehouses`、`/api/locations` |
| 入库/出库 | `/api/inbound-orders`、`/api/outbound-orders` |
| 盘点/调拨 | `/api/stocktake-orders`、`/api/transfer-orders` |
| 库存 | `/api/inventory`、`/api/inventory/transactions`、`/api/inventory/summary`、`/api/inventory/alerts` |
| 报表 | `/api/reports/dashboard`、`/api/reports/stock-summary`、`/api/reports/in-out-summary`、`/api/reports/trend`、`/api/reports/top-materials` |
| 插件 | `/api/plugins` |

单据状态流转：`0 草稿 → 1 确认 → 2 完成`，或 `→ 3 取消`。

## 插件开发

插件契约位于 `Wms.Plugin/IWmsPlugin.cs`：

```csharp
public interface IWmsPlugin
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Category { get; }
    string Version { get; }
    string Author { get; }
    Task<PluginExecutionResult> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default);
}
```

开发步骤：

1. 新建类库项目，引用 `Wms.Plugin`。
2. 实现 `IWmsPlugin`，通过 `context.Settings` 读取配置、`context.Payload` 读取宿主数据、`context.Logger` 输出日志。
3. 编译后将 DLL 放入 `backend/src/Wms.WebApi/Plugins/` 目录，启动时由 `PluginManager.LoadExternalPlugins()` 反射加载。
4. 在「插件管理」页面启用、配置并执行插件。

内置插件：

- `inventory-alert`：库存预警汇总
- `webhook-notify`：将事件推送到自定义 Webhook 地址
- `erp-sync`：按字段映射把单据同步到 ERP 接口

## 配置说明

后端配置文件：`backend/src/Wms.WebApi/appsettings.json`

```json
{
  "ConnectionStrings": { "Default": "Data Source=wms.db" },
  "Jwt": {
    "Secret": "请务必在生产环境修改",
    "Issuer": "Wms",
    "Audience": "Wms.Client",
    "ExpireHours": 12
  },
  "Kestrel": { "Endpoints": { "Http": { "Url": "http://localhost:5080" } } }
}
```

切换数据库：修改 `Wms.App` 中 `AddApplicationModule` 的 Provider 配置，并替换对应的 EF Core 数据库包即可；实体与索引配置无需改动。

前端开发代理：`frontend/vue.config.js` 中 `devServer.proxy['/api'].target`。

## 贡献指南

欢迎提交 Issue 与 Pull Request。

1. Fork 本仓库并创建特性分支：`feat/xxx` 或 `fix/xxx`。
2. 遵循现有代码风格：后端保持五模块依赖方向，公共能力放 `Core`，业务放 `App`，库存规则放 `Im`，扩展放 `Plugin`；前端页面统一放在 `src/views` 并按模块分组。
3. 新增业务接口需补全 XML 注释、权限注解与 DTO 校验。
4. 提交前请确保：
   - `dotnet build backend/Wms.sln` 通过；
   - `npm run build`（frontend）通过；
   - 涉及权限的改动同步更新种子数据与 `PermissionCodes`。
5. 提交信息遵循 Conventional Commits：`feat:`、`fix:`、`docs:`、`refactor:`、`chore:` 等。

详细规范见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 开源协议

本项目采用 [MIT License](LICENSE) 开源，可自由用于商业与非商业项目，请保留版权声明。
