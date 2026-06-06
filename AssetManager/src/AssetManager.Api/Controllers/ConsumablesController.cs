using AssetManager.Core.Entities;
using AssetManager.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsumablesController : ControllerBase
{
    private readonly AssetManagerDbContext _context;

    public ConsumablesController(AssetManagerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<Consumable>>> GetConsumables([FromQuery] string? keyword)
    {
        var query = _context.Consumables.AsQueryable();
        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(c => c.Name.Contains(keyword) || c.ItemCode.Contains(keyword));

        var items = await query.OrderBy(c => c.Name).ToListAsync();
        return Ok(items);
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<List<Consumable>>> GetAlertItems()
    {
        var items = await _context.Consumables
            .Where(c => c.CurrentStock <= c.AlertThreshold)
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<Consumable>> CreateConsumable(Consumable consumable)
    {
        consumable.ItemCode = await GenerateItemCode();
        _context.Consumables.Add(consumable);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetConsumable), new { id = consumable.Id }, consumable);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Consumable>> GetConsumable(int id)
    {
        var item = await _context.Consumables
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost("{id}/stockin")]
    public async Task<IActionResult> StockIn(int id, [FromBody] StockRequest request)
    {
        var item = await _context.Consumables.FindAsync(id);
        if (item == null) return NotFound();

        item.CurrentStock += request.Quantity;

        _context.ConsumableTransactions.Add(new ConsumableTransaction
        {
            ConsumableId = id,
            Type = TransactionType.StockIn,
            Quantity = request.Quantity,
            StockAfter = item.CurrentStock,
            OperatorName = User.Identity?.Name ?? "系统",
            Remarks = request.Remarks
        });

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id}/stockout")]
    public async Task<IActionResult> StockOut(int id, [FromBody] StockOutRequest request)
    {
        var item = await _context.Consumables.FindAsync(id);
        if (item == null) return NotFound();

        if (item.CurrentStock < request.Quantity)
            return BadRequest("库存不足");

        item.CurrentStock -= request.Quantity;

        _context.ConsumableTransactions.Add(new ConsumableTransaction
        {
            ConsumableId = id,
            Type = TransactionType.StockOut,
            Quantity = request.Quantity,
            StockAfter = item.CurrentStock,
            OperatorName = User.Identity?.Name ?? "系统",
            ReceiverName = request.ReceiverName,
            Department = request.Department,
            Remarks = request.Remarks
        });

        await _context.SaveChangesAsync();
        return Ok();
    }

    private async Task<string> GenerateItemCode()
    {
        var count = await _context.Consumables.CountAsync() + 1;
        return $"HC-{DateTime.Now:yyyyMM}-{count:D4}";
    }
}

public class StockRequest
{
    public int Quantity { get; set; }
    public string? Remarks { get; set; }
}

public class StockOutRequest : StockRequest
{
    public string ReceiverName { get; set; } = string.Empty;
    public string? Department { get; set; }
}
