using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.App.Entities;
using Wms.Core.Common;
using Wms.Core.Entities;
using Wms.Core.Security;
using Wms.Core.Utils;
using Wms.Im;
using Wms.Im.Entities;

namespace Wms.App.Data;

/// <summary>
/// 数据库初始化器：首次启动时创建表结构并写入演示数据（权限、菜单、账号、基础资料与库存流水）。
/// </summary>
public static class DbInitializer
{
    /// <summary>初始化数据库</summary>
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WmsDbContext>();

        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (!await db.Users.AnyAsync(cancellationToken))
            await SeedAsync(db, scope.ServiceProvider, cancellationToken);

        await EnsurePluginConfigsAsync(db, cancellationToken);
    }

    private static async Task SeedAsync(WmsDbContext db, IServiceProvider provider, CancellationToken cancellationToken)
    {
        // -------------------- 权限 --------------------
        var permissions = new List<Permission>
        {
            NewPermission(PermissionCodes.DashboardView, "查看工作台", "工作台", 1),
            NewPermission(PermissionCodes.MaterialView, "查看物料", "基础资料", 11),
            NewPermission(PermissionCodes.MaterialEdit, "维护物料", "基础资料", 12),
            NewPermission(PermissionCodes.CategoryView, "查看物料分类", "基础资料", 13),
            NewPermission(PermissionCodes.CategoryEdit, "维护物料分类", "基础资料", 14),
            NewPermission(PermissionCodes.SupplierView, "查看供应商", "基础资料", 15),
            NewPermission(PermissionCodes.SupplierEdit, "维护供应商", "基础资料", 16),
            NewPermission(PermissionCodes.CustomerView, "查看客户", "基础资料", 17),
            NewPermission(PermissionCodes.CustomerEdit, "维护客户", "基础资料", 18),
            NewPermission(PermissionCodes.WarehouseView, "查看仓库", "仓库配置", 21),
            NewPermission(PermissionCodes.WarehouseEdit, "维护仓库", "仓库配置", 22),
            NewPermission(PermissionCodes.LocationView, "查看库位", "仓库配置", 23),
            NewPermission(PermissionCodes.LocationEdit, "维护库位", "仓库配置", 24),
            NewPermission(PermissionCodes.InboundView, "查看入库单", "入库作业", 31),
            NewPermission(PermissionCodes.InboundEdit, "维护入库单", "入库作业", 32),
            NewPermission(PermissionCodes.InboundExecute, "执行入库", "入库作业", 33),
            NewPermission(PermissionCodes.OutboundView, "查看出库单", "出库作业", 41),
            NewPermission(PermissionCodes.OutboundEdit, "维护出库单", "出库作业", 42),
            NewPermission(PermissionCodes.OutboundExecute, "执行出库", "出库作业", 43),
            NewPermission(PermissionCodes.StockTakeView, "查看盘点单", "库存作业", 51),
            NewPermission(PermissionCodes.StockTakeEdit, "维护盘点单", "库存作业", 52),
            NewPermission(PermissionCodes.StockTakeExecute, "执行盘点", "库存作业", 53),
            NewPermission(PermissionCodes.TransferView, "查看调拨单", "库存作业", 54),
            NewPermission(PermissionCodes.TransferEdit, "维护调拨单", "库存作业", 55),
            NewPermission(PermissionCodes.TransferExecute, "执行调拨", "库存作业", 56),
            NewPermission(PermissionCodes.InventoryView, "查看库存", "库存管理", 61),
            NewPermission(PermissionCodes.ReportView, "查看报表", "报表统计", 71),
            NewPermission(PermissionCodes.UserView, "查看用户", "系统管理", 91),
            NewPermission(PermissionCodes.UserEdit, "维护用户", "系统管理", 92),
            NewPermission(PermissionCodes.RoleView, "查看角色", "系统管理", 93),
            NewPermission(PermissionCodes.RoleEdit, "维护角色", "系统管理", 94),
            NewPermission(PermissionCodes.MenuView, "查看菜单", "系统管理", 95),
            NewPermission(PermissionCodes.MenuEdit, "维护菜单", "系统管理", 96),
            NewPermission(PermissionCodes.LogView, "查看操作日志", "系统管理", 97),
            NewPermission(PermissionCodes.PluginView, "查看插件", "系统管理", 98),
            NewPermission(PermissionCodes.PluginEdit, "维护插件", "系统管理", 99)
        };
        db.Permissions.AddRange(permissions);
        await db.SaveChangesAsync(cancellationToken);

        // -------------------- 菜单 --------------------
        var menuDefs = new (string Key, string Name, string? Path, string? Icon, string? Permission, string? ParentKey, MenuType Type)[]
        {
            ("dashboard", "工作台", "/dashboard", "el-icon-odometer", PermissionCodes.DashboardView, null, MenuType.Menu),
            ("master", "基础资料", null, "el-icon-collection", null, null, MenuType.Directory),
            ("master-materials", "物料管理", "/master/materials", "el-icon-box", PermissionCodes.MaterialView, "master", MenuType.Menu),
            ("master-categories", "物料分类", "/master/categories", "el-icon-folder-opened", PermissionCodes.CategoryView, "master", MenuType.Menu),
            ("master-suppliers", "供应商管理", "/master/suppliers", "el-icon-truck", PermissionCodes.SupplierView, "master", MenuType.Menu),
            ("master-customers", "客户管理", "/master/customers", "el-icon-user", PermissionCodes.CustomerView, "master", MenuType.Menu),
            ("warehouse-config", "仓库配置", null, "el-icon-office-building", null, null, MenuType.Directory),
            ("warehouses", "仓库管理", "/warehouse/warehouses", "el-icon-house", PermissionCodes.WarehouseView, "warehouse-config", MenuType.Menu),
            ("locations", "库位管理", "/warehouse/locations", "el-icon-location-outline", PermissionCodes.LocationView, "warehouse-config", MenuType.Menu),
            ("inbound", "入库作业", "/inbound", "el-icon-download", PermissionCodes.InboundView, null, MenuType.Menu),
            ("outbound", "出库作业", "/outbound", "el-icon-upload2", PermissionCodes.OutboundView, null, MenuType.Menu),
            ("inventory-mgmt", "库存管理", null, "el-icon-s-grid", null, null, MenuType.Directory),
            ("inventory", "库存查询", "/inventory", "el-icon-search", PermissionCodes.InventoryView, "inventory-mgmt", MenuType.Menu),
            ("inventory-transactions", "库存流水", "/inventory/transactions", "el-icon-tickets", PermissionCodes.InventoryView, "inventory-mgmt", MenuType.Menu),
            ("inventory-alerts", "库存预警", "/inventory/alerts", "el-icon-warning-outline", PermissionCodes.InventoryView, "inventory-mgmt", MenuType.Menu),
            ("operations", "库存作业", null, "el-icon-s-operation", null, null, MenuType.Directory),
            ("stocktake", "库存盘点", "/stocktake", "el-icon-edit-outline", PermissionCodes.StockTakeView, "operations", MenuType.Menu),
            ("transfer", "库存调拨", "/transfer", "el-icon-sort", PermissionCodes.TransferView, "operations", MenuType.Menu),
            ("reports", "报表统计", null, "el-icon-data-line", null, null, MenuType.Directory),
            ("report-stock", "库存汇总", "/reports/stock", "el-icon-pie-chart", PermissionCodes.ReportView, "reports", MenuType.Menu),
            ("report-inout", "收发存报表", "/reports/inout", "el-icon-document", PermissionCodes.ReportView, "reports", MenuType.Menu),
            ("system", "系统管理", null, "el-icon-setting", null, null, MenuType.Directory),
            ("system-users", "用户管理", "/system/users", "el-icon-user-solid", PermissionCodes.UserView, "system", MenuType.Menu),
            ("system-roles", "角色权限", "/system/roles", "el-icon-key", PermissionCodes.RoleView, "system", MenuType.Menu),
            ("system-menus", "菜单管理", "/system/menus", "el-icon-menu", PermissionCodes.MenuView, "system", MenuType.Menu),
            ("system-logs", "操作日志", "/system/logs", "el-icon-notebook-2", PermissionCodes.LogView, "system", MenuType.Menu),
            ("system-plugins", "插件管理", "/system/plugins", "el-icon-connection", PermissionCodes.PluginView, "system", MenuType.Menu)
        };

        var menuIdsByKey = new Dictionary<string, long>();
        foreach (var def in menuDefs)
        {
            var menu = new Menu
            {
                Name = def.Name,
                Path = def.Path,
                Component = def.Path,
                Icon = def.Icon,
                PermissionCode = def.Permission,
                ParentId = def.ParentKey == null ? 0 : menuIdsByKey[def.ParentKey],
                Sort = menuIdsByKey.Count + 1,
                Type = def.Type,
                Visible = true
            };
            db.Menus.Add(menu);
            await db.SaveChangesAsync(cancellationToken);
            menuIdsByKey[def.Key] = menu.Id;
        }

        // -------------------- 角色 --------------------
        var adminRole = new Role { Code = "admin", Name = "系统管理员", Remark = "拥有系统全部权限" };
        var keeperRole = new Role { Code = "keeper", Name = "仓库管理员", Remark = "负责出入库与库存作业" };
        var viewerRole = new Role { Code = "viewer", Name = "只读访客", Remark = "仅查看库存与报表" };
        db.Roles.AddRange(adminRole, keeperRole, viewerRole);
        await db.SaveChangesAsync(cancellationToken);

        var permissionByCode = permissions.ToDictionary(x => x.Code);
        var keeperCodes = new[]
        {
            PermissionCodes.DashboardView, PermissionCodes.MaterialView, PermissionCodes.MaterialEdit,
            PermissionCodes.CategoryView, PermissionCodes.CategoryEdit, PermissionCodes.WarehouseView,
            PermissionCodes.LocationView, PermissionCodes.InboundView, PermissionCodes.InboundEdit,
            PermissionCodes.InboundExecute, PermissionCodes.OutboundView, PermissionCodes.OutboundEdit,
            PermissionCodes.OutboundExecute, PermissionCodes.StockTakeView, PermissionCodes.StockTakeEdit,
            PermissionCodes.StockTakeExecute, PermissionCodes.TransferView, PermissionCodes.TransferEdit,
            PermissionCodes.TransferExecute, PermissionCodes.InventoryView, PermissionCodes.ReportView
        };
        var viewerCodes = new[]
        {
            PermissionCodes.DashboardView, PermissionCodes.MaterialView, PermissionCodes.CategoryView,
            PermissionCodes.WarehouseView, PermissionCodes.LocationView, PermissionCodes.InventoryView,
            PermissionCodes.ReportView
        };

        db.RolePermissions.AddRange(permissions.Select(p => new RolePermission { RoleId = adminRole.Id, PermissionId = p.Id }));
        db.RolePermissions.AddRange(keeperCodes.Select(code => new RolePermission { RoleId = keeperRole.Id, PermissionId = permissionByCode[code].Id }));
        db.RolePermissions.AddRange(viewerCodes.Select(code => new RolePermission { RoleId = viewerRole.Id, PermissionId = permissionByCode[code].Id }));

        var keeperMenuKeys = new[]
        {
            "dashboard", "master", "master-materials", "master-categories", "warehouse-config", "warehouses",
            "locations", "inbound", "outbound", "inventory-mgmt", "inventory", "inventory-transactions",
            "inventory-alerts", "operations", "stocktake", "transfer", "reports", "report-stock", "report-inout"
        };
        var viewerMenuKeys = new[]
        {
            "dashboard", "master", "master-materials", "master-categories", "warehouse-config", "warehouses",
            "locations", "inventory-mgmt", "inventory", "inventory-transactions", "inventory-alerts",
            "reports", "report-stock", "report-inout"
        };

        db.RoleMenus.AddRange(menuIdsByKey.Values.Select(id => new RoleMenu { RoleId = adminRole.Id, MenuId = id }));
        db.RoleMenus.AddRange(keeperMenuKeys.Select(key => new RoleMenu { RoleId = keeperRole.Id, MenuId = menuIdsByKey[key] }));
        db.RoleMenus.AddRange(viewerMenuKeys.Select(key => new RoleMenu { RoleId = viewerRole.Id, MenuId = menuIdsByKey[key] }));
        await db.SaveChangesAsync(cancellationToken);

        // -------------------- 用户 --------------------
        var admin = new User
        {
            UserName = "admin",
            NickName = "系统管理员",
            PasswordHash = PasswordHasher.Hash("admin123"),
            Email = "admin@wms.local",
            Phone = "13800000001",
            Status = CommonStatus.Enabled
        };
        var keeper = new User
        {
            UserName = "keeper",
            NickName = "仓库管理员",
            PasswordHash = PasswordHasher.Hash("keeper123"),
            Email = "keeper@wms.local",
            Phone = "13800000002",
            Status = CommonStatus.Enabled
        };
        var viewer = new User
        {
            UserName = "viewer",
            NickName = "数据查看员",
            PasswordHash = PasswordHasher.Hash("viewer123"),
            Email = "viewer@wms.local",
            Phone = "13800000003",
            Status = CommonStatus.Enabled
        };
        db.Users.AddRange(admin, keeper, viewer);
        await db.SaveChangesAsync(cancellationToken);

        db.UserRoles.AddRange(
            new UserRole { UserId = admin.Id, RoleId = adminRole.Id },
            new UserRole { UserId = keeper.Id, RoleId = keeperRole.Id },
            new UserRole { UserId = viewer.Id, RoleId = viewerRole.Id });
        await db.SaveChangesAsync(cancellationToken);

        // -------------------- 基础资料 --------------------
        var categories = new List<MaterialCategory>
        {
            new() { Name = "电子元件", Sort = 1 },
            new() { Name = "五金配件", Sort = 2 },
            new() { Name = "包装材料", Sort = 3 },
            new() { Name = "办公耗材", Sort = 4 }
        };
        db.MaterialCategories.AddRange(categories);
        await db.SaveChangesAsync(cancellationToken);

        var materialDefs = new (string Code, string Name, string Spec, int CategoryIndex, string Unit, string Brand, decimal Price, decimal SafetyStock)[]
        {
            ("M-1001", "贴片电阻", "0603 10kΩ ±1%", 0, "个", "国巨", 0.02m, 500m),
            ("M-1002", "贴片电容", "0603 100nF 16V", 0, "个", "村田", 0.05m, 400m),
            ("M-1003", "MCU 芯片", "STM32F103C8T6 LQFP48", 0, "个", "ST", 12.00m, 100m),
            ("M-1004", "无源晶振", "8MHz HC-49S", 0, "个", "泰晶", 1.20m, 200m),
            ("M-2001", "不锈钢螺丝", "M3×8 十字盘头", 1, "个", "精工", 0.15m, 300m),
            ("M-2002", "深沟球轴承", "6203-2RS 17×40×12", 1, "个", "哈轴", 3.50m, 80m),
            ("M-2003", "工业铝型材", "4040 标准型 6 米", 1, "米", "中铝", 8.00m, 100m),
            ("M-3001", "瓦楞纸箱", "400×300×300 五层", 2, "个", "恒信", 1.80m, 200m),
            ("M-3002", "缓冲气泡膜", "宽 1.2 米 双层", 2, "卷", "恒信", 0.40m, 150m),
            ("M-3003", "PP 打包带", "宽 12mm 白色", 2, "卷", "恒信", 2.20m, 100m),
            ("M-4001", "A4 复印纸", "70g 500 张/包", 3, "包", "得力", 3.50m, 60m),
            ("M-4002", "热敏标签纸", "100×80mm 500 张/卷", 3, "卷", "得力", 1.10m, 50m)
        };

        var materials = new List<Material>();
        foreach (var def in materialDefs)
        {
            materials.Add(new Material
            {
                Code = def.Code,
                Name = def.Name,
                Spec = def.Spec,
                CategoryId = categories[def.CategoryIndex].Id,
                Unit = def.Unit,
                Brand = def.Brand,
                Price = def.Price,
                SafetyStock = def.SafetyStock,
                Status = CommonStatus.Enabled
            });
        }
        db.Materials.AddRange(materials);
        await db.SaveChangesAsync(cancellationToken);

        var suppliers = new List<Supplier>
        {
            new() { Code = "S-1001", Name = "华东电子元器件有限公司", Contact = "张伟", Phone = "021-88880001", Address = "上海市浦东新区张江路 88 号" },
            new() { Code = "S-1002", Name = "精工五金制造有限公司", Contact = "李强", Phone = "0512-66660002", Address = "苏州市工业园区星湖街 100 号" },
            new() { Code = "S-1003", Name = "恒信包装材料有限公司", Contact = "王芳", Phone = "0571-88880003", Address = "杭州市余杭区良渚街道 18 号" }
        };
        db.Suppliers.AddRange(suppliers);

        var customers = new List<Customer>
        {
            new() { Code = "C-1001", Name = "星辰智能科技有限公司", Contact = "陈晨", Phone = "0755-88880011", Address = "深圳市南山区科技园南区" },
            new() { Code = "C-1002", Name = "蓝海精密制造有限公司", Contact = "刘洋", Phone = "0512-88880022", Address = "苏州市吴中区友新路 9 号" },
            new() { Code = "C-1003", Name = "恒达贸易有限公司", Contact = "赵敏", Phone = "021-88880033", Address = "上海市闵行区申长路 66 号" }
        };
        db.Customers.AddRange(customers);

        var warehouses = new List<Warehouse>
        {
            new() { Code = "WH01", Name = "上海主仓", Address = "上海市浦东新区物流园 1 号库", Manager = "周建国", Phone = "021-58880001" },
            new() { Code = "WH02", Name = "苏州分仓", Address = "苏州市工业园区物流大道 8 号", Manager = "孙丽", Phone = "0512-58880002" }
        };
        db.Warehouses.AddRange(warehouses);
        await db.SaveChangesAsync(cancellationToken);

        var locationDefs = new (long WarehouseId, string Code, string Name, string Zone, LocationType Type)[]
        {
            (warehouses[0].Id, "A-01-01", "A 区 01 排 01 位", "A 区", LocationType.Storage),
            (warehouses[0].Id, "A-01-02", "A 区 01 排 02 位", "A 区", LocationType.Storage),
            (warehouses[0].Id, "A-02-01", "A 区 02 排 01 位", "A 区", LocationType.Storage),
            (warehouses[0].Id, "B-01-01", "B 区 01 排 01 位", "B 区", LocationType.Storage),
            (warehouses[0].Id, "REC-01", "收货暂存位", "收货区", LocationType.Receiving),
            (warehouses[0].Id, "SHP-01", "发货暂存位", "发货区", LocationType.Shipping),
            (warehouses[1].Id, "A-01-01", "A 区 01 排 01 位", "A 区", LocationType.Storage),
            (warehouses[1].Id, "A-02-01", "A 区 02 排 01 位", "A 区", LocationType.Storage),
            (warehouses[1].Id, "REC-01", "收货暂存位", "收货区", LocationType.Receiving),
            (warehouses[1].Id, "SHP-01", "发货暂存位", "发货区", LocationType.Shipping)
        };

        var locations = new List<WarehouseLocation>();
        foreach (var def in locationDefs)
        {
            locations.Add(new WarehouseLocation
            {
                WarehouseId = def.WarehouseId,
                Code = def.Code,
                Name = def.Name,
                Zone = def.Zone,
                Type = def.Type,
                Capacity = 10000,
                Status = CommonStatus.Enabled
            });
        }
        db.WarehouseLocations.AddRange(locations);
        await db.SaveChangesAsync(cancellationToken);

        var materialByCode = materials.ToDictionary(x => x.Code);
        var wh1Locations = locations.Where(x => x.WarehouseId == warehouses[0].Id).ToDictionary(x => x.Code);
        var wh2Locations = locations.Where(x => x.WarehouseId == warehouses[1].Id).ToDictionary(x => x.Code);
        var inventory = provider.GetRequiredService<InventoryManager>();
        var operatorName = admin.NickName;

        // -------------------- 演示业务数据 --------------------
        await SeedInboundOrderAsync(db, inventory, DateTime.Now.AddDays(-12), warehouses[0].Id, suppliers[0].Id, operatorName, new[]
        {
            (materialByCode["M-1001"].Id, (long?)wh1Locations["A-01-01"].Id, 1000m, 0.02m, (string?)"B20260901"),
            (materialByCode["M-1002"].Id, (long?)wh1Locations["A-01-01"].Id, 800m, 0.05m, (string?)"B20260901"),
            (materialByCode["M-1003"].Id, (long?)wh1Locations["A-01-02"].Id, 300m, 12.00m, (string?)"B20260902"),
            (materialByCode["M-1004"].Id, (long?)wh1Locations["A-01-02"].Id, 500m, 1.20m, (string?)"B20260902")
        }, true, cancellationToken);

        await SeedInboundOrderAsync(db, inventory, DateTime.Now.AddDays(-9), warehouses[0].Id, suppliers[1].Id, operatorName, new[]
        {
            (materialByCode["M-2001"].Id, (long?)wh1Locations["A-02-01"].Id, 600m, 0.15m, (string?)null),
            (materialByCode["M-2002"].Id, (long?)wh1Locations["A-02-01"].Id, 200m, 3.50m, (string?)null),
            (materialByCode["M-2003"].Id, (long?)wh1Locations["B-01-01"].Id, 400m, 8.00m, (string?)null),
            (materialByCode["M-3001"].Id, (long?)wh1Locations["B-01-01"].Id, 1000m, 1.80m, (string?)null)
        }, true, cancellationToken);

        await SeedInboundOrderAsync(db, inventory, DateTime.Now.AddDays(-3), warehouses[0].Id, suppliers[2].Id, operatorName, new[]
        {
            (materialByCode["M-3002"].Id, (long?)wh1Locations["B-01-01"].Id, 800m, 0.40m, (string?)null),
            (materialByCode["M-3003"].Id, (long?)wh1Locations["B-01-01"].Id, 500m, 2.20m, (string?)null),
            (materialByCode["M-4001"].Id, (long?)wh1Locations["A-02-01"].Id, 300m, 3.50m, (string?)null),
            (materialByCode["M-4002"].Id, (long?)wh1Locations["B-01-01"].Id, 600m, 1.10m, (string?)null)
        }, true, cancellationToken);

        await SeedOutboundOrderAsync(db, inventory, DateTime.Now.AddDays(-7), warehouses[0].Id, customers[0].Id, operatorName, OrderStatus.Completed, new[]
        {
            (materialByCode["M-1001"].Id, (long?)wh1Locations["A-01-01"].Id, 120m, 0.03m, (string?)"B20260901"),
            (materialByCode["M-1003"].Id, (long?)wh1Locations["A-01-02"].Id, 40m, 15.00m, (string?)"B20260902"),
            (materialByCode["M-2001"].Id, (long?)wh1Locations["A-02-01"].Id, 80m, 0.20m, (string?)null)
        }, cancellationToken);

        await SeedOutboundOrderAsync(db, inventory, DateTime.Now.AddDays(-4), warehouses[0].Id, customers[1].Id, operatorName, OrderStatus.Completed, new[]
        {
            (materialByCode["M-1002"].Id, (long?)wh1Locations["A-01-01"].Id, 100m, 0.06m, (string?)"B20260901"),
            (materialByCode["M-2002"].Id, (long?)wh1Locations["A-02-01"].Id, 30m, 4.00m, (string?)null),
            (materialByCode["M-3001"].Id, (long?)wh1Locations["B-01-01"].Id, 150m, 2.00m, (string?)null)
        }, cancellationToken);

        await SeedTransferOrderAsync(db, inventory, DateTime.Now.AddDays(-2), warehouses[0].Id, warehouses[1].Id, operatorName, new[]
        {
            (materialByCode["M-1002"].Id, wh1Locations["A-01-01"].Id, wh2Locations["A-01-01"].Id, 150m, (string?)"B20260901"),
            (materialByCode["M-2003"].Id, wh1Locations["B-01-01"].Id, wh2Locations["A-02-01"].Id, 100m, (string?)null)
        }, cancellationToken);

        await SeedOutboundOrderAsync(db, inventory, DateTime.Now.AddDays(-1), warehouses[0].Id, customers[2].Id, operatorName, OrderStatus.Completed, new[]
        {
            (materialByCode["M-3002"].Id, (long?)wh1Locations["B-01-01"].Id, 200m, 0.45m, (string?)null),
            (materialByCode["M-4001"].Id, (long?)wh1Locations["A-02-01"].Id, 60m, 4.00m, (string?)null)
        }, cancellationToken);

        // 已确认（库存锁定）的出库单
        await SeedOutboundOrderAsync(db, inventory, DateTime.Now, warehouses[0].Id, customers[0].Id, operatorName, OrderStatus.Confirmed, new[]
        {
            (materialByCode["M-1001"].Id, (long?)wh1Locations["A-01-01"].Id, 50m, 0.03m, (string?)"B20260901"),
            (materialByCode["M-2001"].Id, (long?)wh1Locations["A-02-01"].Id, 40m, 0.20m, (string?)null)
        }, cancellationToken);

        // 待执行的入库单 / 出库单
        await SeedInboundOrderAsync(db, inventory, DateTime.Now, warehouses[0].Id, suppliers[1].Id, operatorName, new[]
        {
            (materialByCode["M-2003"].Id, (long?)wh1Locations["A-02-01"].Id, 200m, 8.50m, (string?)null)
        }, false, cancellationToken);

        await SeedOutboundOrderAsync(db, inventory, DateTime.Now, warehouses[0].Id, customers[2].Id, operatorName, OrderStatus.Draft, new[]
        {
            (materialByCode["M-3003"].Id, (long?)wh1Locations["B-01-01"].Id, 30m, 2.50m, (string?)null)
        }, cancellationToken);

        await SeedStockTakeAsync(db, warehouses[0].Id, operatorName, cancellationToken);
    }

    private static Permission NewPermission(string code, string name, string module, int sort)
        => new() { Code = code, Name = name, Module = module, Sort = sort };

    /// <summary>生成入库单及对应入库流水</summary>
    private static async Task SeedInboundOrderAsync(
        WmsDbContext db,
        InventoryManager inventory,
        DateTime occurredAt,
        long warehouseId,
        long? supplierId,
        string operatorName,
        (long MaterialId, long? LocationId, decimal Qty, decimal Price, string? BatchNo)[] lines,
        bool completed,
        CancellationToken cancellationToken)
    {
        var order = new InboundOrder
        {
            OrderNo = OrderNoGenerator.Generate("IN"),
            Type = InboundType.Purchase,
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            Status = completed ? OrderStatus.Completed : OrderStatus.Draft,
            PlannedDate = occurredAt.Date,
            CreatedAt = occurredAt,
            CreatedByName = operatorName,
            Remark = completed ? "初始化演示数据" : "待执行的入库计划"
        };

        foreach (var line in lines)
        {
            order.Items.Add(new InboundOrderItem
            {
                MaterialId = line.MaterialId,
                LocationId = line.LocationId,
                BatchNo = line.BatchNo,
                PlannedQty = line.Qty,
                ReceivedQty = line.Qty,
                Price = line.Price
            });
        }

        db.InboundOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        if (!completed) return;

        foreach (var item in order.Items)
        {
            var tx = await inventory.IncreaseAsync(new InventoryOperation
            {
                MaterialId = item.MaterialId,
                WarehouseId = warehouseId,
                LocationId = item.LocationId!.Value,
                BatchNo = item.BatchNo,
                Quantity = item.PlannedQty,
                UnitCost = item.Price,
                RefType = "Inbound",
                RefId = order.Id,
                RefNo = order.OrderNo,
                Remark = "演示数据入库",
                OperatorId = 1,
                OperatorName = operatorName
            }, InventoryTransactionType.Inbound, cancellationToken);

            tx.OccurredAt = occurredAt;
            tx.CreatedAt = occurredAt;
            item.ReceivedQty = item.PlannedQty;
        }

        order.Status = OrderStatus.Completed;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>生成出库单，并按状态执行库存锁定 / 发运</summary>
    private static async Task SeedOutboundOrderAsync(
        WmsDbContext db,
        InventoryManager inventory,
        DateTime occurredAt,
        long warehouseId,
        long? customerId,
        string operatorName,
        OrderStatus status,
        (long MaterialId, long? LocationId, decimal Qty, decimal Price, string? BatchNo)[] lines,
        CancellationToken cancellationToken)
    {
        var order = new OutboundOrder
        {
            OrderNo = OrderNoGenerator.Generate("OUT"),
            Type = OutboundType.Sales,
            CustomerId = customerId,
            WarehouseId = warehouseId,
            Status = OrderStatus.Draft,
            PlannedDate = occurredAt.Date,
            CreatedAt = occurredAt,
            CreatedByName = operatorName,
            Remark = status == OrderStatus.Draft ? "待执行的出库计划" : "初始化演示数据"
        };

        foreach (var line in lines)
        {
            order.Items.Add(new OutboundOrderItem
            {
                MaterialId = line.MaterialId,
                LocationId = line.LocationId,
                BatchNo = line.BatchNo,
                RequestedQty = line.Qty,
                ShippedQty = 0,
                Price = line.Price
            });
        }

        db.OutboundOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        if (status == OrderStatus.Draft) return;

        foreach (var item in order.Items)
        {
            var tx = await inventory.AllocateAsync(new InventoryOperation
            {
                MaterialId = item.MaterialId,
                WarehouseId = warehouseId,
                LocationId = item.LocationId!.Value,
                BatchNo = item.BatchNo,
                Quantity = item.RequestedQty,
                RefType = "Outbound",
                RefId = order.Id,
                RefNo = order.OrderNo,
                OperatorId = 1,
                OperatorName = operatorName
            }, cancellationToken);

            tx.OccurredAt = occurredAt;
            tx.CreatedAt = occurredAt;
        }

        if (status == OrderStatus.Confirmed)
        {
            order.Status = OrderStatus.Confirmed;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        foreach (var item in order.Items)
        {
            var tx = await inventory.ShipAsync(new InventoryOperation
            {
                MaterialId = item.MaterialId,
                WarehouseId = warehouseId,
                LocationId = item.LocationId!.Value,
                BatchNo = item.BatchNo,
                Quantity = item.RequestedQty,
                RefType = "Outbound",
                RefId = order.Id,
                RefNo = order.OrderNo,
                OperatorId = 1,
                OperatorName = operatorName
            }, cancellationToken);

            tx.OccurredAt = occurredAt;
            tx.CreatedAt = occurredAt;
            item.ShippedQty = item.RequestedQty;
        }

        order.Status = OrderStatus.Completed;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>生成已完成调拨单及调拨流水</summary>
    private static async Task SeedTransferOrderAsync(
        WmsDbContext db,
        InventoryManager inventory,
        DateTime occurredAt,
        long fromWarehouseId,
        long toWarehouseId,
        string operatorName,
        (long MaterialId, long FromLocationId, long ToLocationId, decimal Qty, string? BatchNo)[] lines,
        CancellationToken cancellationToken)
    {
        var order = new TransferOrder
        {
            OrderNo = OrderNoGenerator.Generate("TR"),
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            Status = OrderStatus.Draft,
            TransferDate = occurredAt.Date,
            CreatedAt = occurredAt,
            CreatedByName = operatorName,
            Remark = "初始化演示数据"
        };

        foreach (var line in lines)
        {
            order.Items.Add(new TransferOrderItem
            {
                MaterialId = line.MaterialId,
                FromLocationId = line.FromLocationId,
                ToLocationId = line.ToLocationId,
                BatchNo = line.BatchNo,
                Quantity = line.Qty
            });
        }

        db.TransferOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            var batchNo = item.BatchNo ?? string.Empty;
            var record = await db.InventoryRecords.FirstOrDefaultAsync(
                x => x.MaterialId == item.MaterialId
                     && x.WarehouseId == fromWarehouseId
                     && x.LocationId == item.FromLocationId
                     && x.BatchNo == batchNo,
                cancellationToken);

            var operation = new InventoryOperation
            {
                MaterialId = item.MaterialId,
                WarehouseId = fromWarehouseId,
                LocationId = item.FromLocationId,
                BatchNo = batchNo,
                Quantity = item.Quantity,
                UnitCost = record?.UnitCost ?? 0,
                RefType = "Transfer",
                RefId = order.Id,
                RefNo = order.OrderNo,
                OperatorId = 1,
                OperatorName = operatorName
            };

            var outTx = await inventory.TransferOutAsync(operation, cancellationToken);
            outTx.OccurredAt = occurredAt;
            outTx.CreatedAt = occurredAt;

            var inbound = operation.With(item.Quantity);
            inbound.WarehouseId = toWarehouseId;
            inbound.LocationId = item.ToLocationId;
            var inTx = await inventory.IncreaseAsync(inbound, InventoryTransactionType.TransferIn, cancellationToken);
            inTx.OccurredAt = occurredAt;
            inTx.CreatedAt = occurredAt;
        }

        order.Status = OrderStatus.Completed;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>生成一张待盘点状态的盘点单</summary>
    private static async Task SeedStockTakeAsync(
        WmsDbContext db,
        long warehouseId,
        string operatorName,
        CancellationToken cancellationToken)
    {
        var records = await db.InventoryRecords
            .Where(x => x.WarehouseId == warehouseId && x.OnHandQty > 0)
            .OrderBy(x => x.MaterialId)
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
            return;

        var order = new StockTakeOrder
        {
            OrderNo = OrderNoGenerator.Generate("TK"),
            WarehouseId = warehouseId,
            Status = StockTakeStatus.Draft,
            TakeDate = DateTime.Today,
            CreatedByName = operatorName,
            Remark = "初始化演示盘点单"
        };

        foreach (var record in records)
        {
            order.Items.Add(new StockTakeItem
            {
                MaterialId = record.MaterialId,
                InventoryRecordId = record.Id,
                LocationId = record.LocationId,
                BatchNo = record.BatchNo,
                SystemQty = record.OnHandQty,
                CountedQty = record.OnHandQty
            });
        }

        db.StockTakeOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>确保内置插件配置存在</summary>
    private static async Task EnsurePluginConfigsAsync(WmsDbContext db, CancellationToken cancellationToken)
    {
        var existing = await db.PluginConfigs.Select(x => x.PluginId).ToListAsync(cancellationToken);

        if (!existing.Contains("inventory-alert"))
        {
            db.PluginConfigs.Add(new PluginConfig
            {
                PluginId = "inventory-alert",
                Name = "库存安全预警",
                Enabled = true,
                SettingsJson = "{}"
            });
        }

        if (!existing.Contains("webhook-notify"))
        {
            db.PluginConfigs.Add(new PluginConfig
            {
                PluginId = "webhook-notify",
                Name = "Webhook 消息推送",
                Enabled = false,
                SettingsJson = "{\"webhookUrl\":\"\"}"
            });
        }

        if (!existing.Contains("erp-sync"))
        {
            db.PluginConfigs.Add(new PluginConfig
            {
                PluginId = "erp-sync",
                Name = "ERP 数据同步适配器",
                Enabled = false,
                SettingsJson = "{\"erpEndpoint\":\"\",\"erpToken\":\"\"}"
            });
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);
    }
}
