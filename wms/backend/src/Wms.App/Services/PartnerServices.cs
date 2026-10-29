using Microsoft.EntityFrameworkCore;
using Wms.App.Data;
using Wms.App.Dtos;
using Wms.Core.Common;
using Wms.Core.Entities;

namespace Wms.App.Services;

/// <summary>供应商服务</summary>
public class SupplierService
{
    private readonly WmsDbContext _db;

    public SupplierService(WmsDbContext db)
    {
        _db = db;
    }

    /// <summary>分页查询供应商</summary>
    public async Task<PagedResult<PartnerDto>> QueryAsync(PartnerQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.Suppliers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Contact != null && x.Contact.Contains(keyword)));
        }
        if (query.Status.HasValue)
            source = source.Where(x => x.Status == query.Status);

        var total = await source.LongCountAsync(cancellationToken);
        var items = await source
            .OrderBy(x => x.Code)
            .Skip(query.GetSkip()).Take(query.PageSize)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);

        return PagedResult<PartnerDto>.Create(items, total, query.Page, query.PageSize);
    }

    /// <summary>供应商下拉选项</summary>
    public async Task<List<OptionDto>> OptionsAsync(CancellationToken cancellationToken = default)
        => await _db.Suppliers
            .Where(x => x.Status == CommonStatus.Enabled)
            .OrderBy(x => x.Code)
            .Select(x => new OptionDto { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync(cancellationToken);

    /// <summary>创建供应商</summary>
    public async Task<long> CreateAsync(SavePartnerRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var code = request.Code.Trim();
        if (await _db.Suppliers.AnyAsync(x => x.Code == code, cancellationToken))
            throw new ConflictException($"供应商编码 {code} 已存在");

        var supplier = new Supplier();
        Apply(supplier, request);
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(cancellationToken);
        return supplier.Id;
    }

    /// <summary>更新供应商</summary>
    public async Task UpdateAsync(long id, SavePartnerRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var supplier = await _db.Suppliers.FindAsync(new object[] { id }, cancellationToken)
                       ?? throw new NotFoundException("供应商不存在");

        var code = request.Code.Trim();
        if (await _db.Suppliers.AnyAsync(x => x.Code == code && x.Id != id, cancellationToken))
            throw new ConflictException($"供应商编码 {code} 已存在");

        Apply(supplier, request);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>删除供应商</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var supplier = await _db.Suppliers.FindAsync(new object[] { id }, cancellationToken)
                       ?? throw new NotFoundException("供应商不存在");

        if (await _db.InboundOrders.AnyAsync(x => x.SupplierId == id, cancellationToken))
            throw new BusinessException("该供应商已被入库单引用，无法删除");

        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static PartnerDto ToDto(Supplier x) => new()
    {
        Id = x.Id,
        Code = x.Code,
        Name = x.Name,
        Contact = x.Contact,
        Phone = x.Phone,
        Address = x.Address,
        Status = x.Status,
        Remark = x.Remark
    };

    internal static void Validate(SavePartnerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessException("编码与名称不能为空");
    }

    private static void Apply(Supplier supplier, SavePartnerRequest request)
    {
        supplier.Code = request.Code.Trim().ToUpperInvariant();
        supplier.Name = request.Name.Trim();
        supplier.Contact = request.Contact;
        supplier.Phone = request.Phone;
        supplier.Address = request.Address;
        supplier.Status = request.Status;
        supplier.Remark = request.Remark;
    }
}

/// <summary>客户服务</summary>
public class CustomerService
{
    private readonly WmsDbContext _db;

    public CustomerService(WmsDbContext db)
    {
        _db = db;
    }

    /// <summary>分页查询客户</summary>
    public async Task<PagedResult<PartnerDto>> QueryAsync(PartnerQuery query, CancellationToken cancellationToken = default)
    {
        var source = _db.Customers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            source = source.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) || (x.Contact != null && x.Contact.Contains(keyword)));
        }
        if (query.Status.HasValue)
            source = source.Where(x => x.Status == query.Status);

        var total = await source.LongCountAsync(cancellationToken);
        var items = await source
            .OrderBy(x => x.Code)
            .Skip(query.GetSkip()).Take(query.PageSize)
            .Select(x => new PartnerDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Contact = x.Contact,
                Phone = x.Phone,
                Address = x.Address,
                Status = x.Status,
                Remark = x.Remark
            })
            .ToListAsync(cancellationToken);

        return PagedResult<PartnerDto>.Create(items, total, query.Page, query.PageSize);
    }

    /// <summary>客户下拉选项</summary>
    public async Task<List<OptionDto>> OptionsAsync(CancellationToken cancellationToken = default)
        => await _db.Customers
            .Where(x => x.Status == CommonStatus.Enabled)
            .OrderBy(x => x.Code)
            .Select(x => new OptionDto { Id = x.Id, Code = x.Code, Name = x.Name })
            .ToListAsync(cancellationToken);

    /// <summary>创建客户</summary>
    public async Task<long> CreateAsync(SavePartnerRequest request, CancellationToken cancellationToken = default)
    {
        SupplierService.Validate(request);
        var code = request.Code.Trim();
        if (await _db.Customers.AnyAsync(x => x.Code == code, cancellationToken))
            throw new ConflictException($"客户编码 {code} 已存在");

        var customer = new Customer
        {
            Code = code.ToUpperInvariant(),
            Name = request.Name.Trim(),
            Contact = request.Contact,
            Phone = request.Phone,
            Address = request.Address,
            Status = request.Status,
            Remark = request.Remark
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(cancellationToken);
        return customer.Id;
    }

    /// <summary>更新客户</summary>
    public async Task UpdateAsync(long id, SavePartnerRequest request, CancellationToken cancellationToken = default)
    {
        SupplierService.Validate(request);
        var customer = await _db.Customers.FindAsync(new object[] { id }, cancellationToken)
                       ?? throw new NotFoundException("客户不存在");

        var code = request.Code.Trim();
        if (await _db.Customers.AnyAsync(x => x.Code == code && x.Id != id, cancellationToken))
            throw new ConflictException($"客户编码 {code} 已存在");

        customer.Code = code.ToUpperInvariant();
        customer.Name = request.Name.Trim();
        customer.Contact = request.Contact;
        customer.Phone = request.Phone;
        customer.Address = request.Address;
        customer.Status = request.Status;
        customer.Remark = request.Remark;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>删除客户</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FindAsync(new object[] { id }, cancellationToken)
                       ?? throw new NotFoundException("客户不存在");

        if (await _db.OutboundOrders.AnyAsync(x => x.CustomerId == id, cancellationToken))
            throw new BusinessException("该客户已被出库单引用，无法删除");

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
