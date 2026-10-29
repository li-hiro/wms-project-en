using Microsoft.AspNetCore.Mvc;
using Wms.App.Dtos;
using Wms.App.Services;
using Wms.Core.Common;
using Wms.WebApi.Authorization;

namespace Wms.WebApi.Controllers;

/// <summary>物料分类接口</summary>
[ApiController]
[Route("api/material-categories")]
public class MaterialCategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public MaterialCategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>查询全部分类</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.CategoryView)]
    public async Task<ApiResult<List<CategoryDto>>> List(CancellationToken cancellationToken)
        => ApiResult<List<CategoryDto>>.Ok(await _categoryService.ListAsync(cancellationToken));

    /// <summary>创建分类</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.CategoryEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SaveCategoryRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _categoryService.CreateAsync(request, cancellationToken), "Category created successfully");

    /// <summary>更新分类</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.CategoryEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SaveCategoryRequest request, CancellationToken cancellationToken)
    {
        await _categoryService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Category updated successfully");
    }

    /// <summary>删除分类</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.CategoryEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Category deleted successfully");
    }
}

/// <summary>物料主数据接口</summary>
[ApiController]
[Route("api/materials")]
public class MaterialsController : ControllerBase
{
    private readonly MaterialService _materialService;

    public MaterialsController(MaterialService materialService)
    {
        _materialService = materialService;
    }

    /// <summary>分页查询物料</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.MaterialView)]
    public async Task<ApiResult<PagedResult<MaterialDto>>> Query([FromQuery] MaterialQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<MaterialDto>>.Ok(await _materialService.QueryAsync(query, cancellationToken));

    /// <summary>物料下拉选项（单据明细选择物料使用）</summary>
    [HttpGet("options")]
    [HasPermission(PermissionCodes.MaterialView)]
    public async Task<ApiResult<List<OptionDto>>> Options([FromQuery] string? keyword, CancellationToken cancellationToken)
        => ApiResult<List<OptionDto>>.Ok(await _materialService.OptionsAsync(keyword, cancellationToken));

    /// <summary>创建物料</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.MaterialEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SaveMaterialRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _materialService.CreateAsync(request, cancellationToken), "Material created successfully");

    /// <summary>更新物料</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.MaterialEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SaveMaterialRequest request, CancellationToken cancellationToken)
    {
        await _materialService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Material updated successfully");
    }

    /// <summary>删除物料</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.MaterialEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _materialService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Material deleted successfully");
    }
}

/// <summary>供应商接口</summary>
[ApiController]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly SupplierService _supplierService;

    public SuppliersController(SupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    /// <summary>分页查询供应商</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.SupplierView)]
    public async Task<ApiResult<PagedResult<PartnerDto>>> Query([FromQuery] PartnerQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<PartnerDto>>.Ok(await _supplierService.QueryAsync(query, cancellationToken));

    /// <summary>供应商下拉选项</summary>
    [HttpGet("options")]
    [HasPermission(PermissionCodes.SupplierView)]
    public async Task<ApiResult<List<OptionDto>>> Options(CancellationToken cancellationToken)
        => ApiResult<List<OptionDto>>.Ok(await _supplierService.OptionsAsync(cancellationToken));

    /// <summary>创建供应商</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.SupplierEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SavePartnerRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _supplierService.CreateAsync(request, cancellationToken), "Supplier created successfully");

    /// <summary>更新供应商</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.SupplierEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SavePartnerRequest request, CancellationToken cancellationToken)
    {
        await _supplierService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Supplier updated successfully");
    }

    /// <summary>删除供应商</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.SupplierEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _supplierService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Supplier deleted successfully");
    }
}

/// <summary>客户接口</summary>
[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;

    public CustomersController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    /// <summary>分页查询客户</summary>
    [HttpGet]
    [HasPermission(PermissionCodes.CustomerView)]
    public async Task<ApiResult<PagedResult<PartnerDto>>> Query([FromQuery] PartnerQuery query, CancellationToken cancellationToken)
        => ApiResult<PagedResult<PartnerDto>>.Ok(await _customerService.QueryAsync(query, cancellationToken));

    /// <summary>客户下拉选项</summary>
    [HttpGet("options")]
    [HasPermission(PermissionCodes.CustomerView)]
    public async Task<ApiResult<List<OptionDto>>> Options(CancellationToken cancellationToken)
        => ApiResult<List<OptionDto>>.Ok(await _customerService.OptionsAsync(cancellationToken));

    /// <summary>创建客户</summary>
    [HttpPost]
    [HasPermission(PermissionCodes.CustomerEdit)]
    public async Task<ApiResult<long>> Create([FromBody] SavePartnerRequest request, CancellationToken cancellationToken)
        => ApiResult<long>.Ok(await _customerService.CreateAsync(request, cancellationToken), "Customer created successfully");

    /// <summary>更新客户</summary>
    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.CustomerEdit)]
    public async Task<ApiResult<object>> Update(long id, [FromBody] SavePartnerRequest request, CancellationToken cancellationToken)
    {
        await _customerService.UpdateAsync(id, request, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Customer updated successfully");
    }

    /// <summary>删除客户</summary>
    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.CustomerEdit)]
    public async Task<ApiResult<object>> Delete(long id, CancellationToken cancellationToken)
    {
        await _customerService.DeleteAsync(id, cancellationToken);
        return ApiResult<object>.Ok(new object(), "Customer deleted successfully");
    }
}
