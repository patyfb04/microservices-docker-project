using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Common.Repositories;
using Play.Inventory.Dtos;
using Play.Inventory.Service.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Play.Inventory.Service.Controllers
{
    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<InventoryItem> _inventoryRepository;
        private readonly IRepository<CatalogItem> _catalogItemsRepository;
        private const string AdminRole = "Admin";
        public ItemsController(IRepository<InventoryItem> itemsRepository, IRepository<CatalogItem> catalogItemsRepository)
        {
            _inventoryRepository = itemsRepository;
            _catalogItemsRepository = catalogItemsRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId) 
        {

            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (Guid.Parse(currentUserId) != userId)
            {
                if (!User.IsInRole(AdminRole))
                {
                    return Forbid();
                }
            }

            var inventoryItemEntities = await _inventoryRepository.GetAllAsync(item => item.UserId == userId);
            var catalogItemIds = inventoryItemEntities.Select(c=> c.CatalogItemId);
            var catalogItemEntities = await _catalogItemsRepository.GetAllAsync(item => catalogItemIds.Contains(item.Id));

            var inventoryItemsDtos = inventoryItemEntities.Select(inventoryItem =>
            {
                var catalogItem = catalogItemEntities.SingleOrDefault(catalogItem => catalogItem.Id == inventoryItem.CatalogItemId);
                if (catalogItem == null)
                {
                    return null; 
                }
                return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
            }).Where(dto => dto != null);

            return Ok(inventoryItemsDtos);
        }

        [HttpPost]
        [Authorize(Roles = AdminRole)]
        public async Task<ActionResult> PostAsync(GrantItemsDto grantItemsDto)
        {
            var inventoryItem = await _inventoryRepository.GetAsync(item => item.UserId == grantItemsDto.UserId && item.CatalogItemId == grantItemsDto.CatalogItemId);
            if (inventoryItem == null) {
                inventoryItem = new InventoryItem()
                {
                    CatalogItemId = grantItemsDto.CatalogItemId,
                    Quantity = grantItemsDto.Quantity,
                    UserId = grantItemsDto.UserId,
                    AcquiredDate = DateTimeOffset.UtcNow
                };

                await _inventoryRepository.CreateAsync(inventoryItem);
            }
            else
            {
                inventoryItem.Quantity += grantItemsDto.Quantity;
                await _inventoryRepository.UpdateAsync(inventoryItem);
            }
          return Ok();
        }
    }
}
