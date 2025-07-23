using CoreAxis.Modules.DemoModule.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DemoModule.API.Controllers
{
    /// <summary>
    /// Controller for demo items.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DemoItemsController : ControllerBase
    {
        private readonly IDemoItemService _demoItemService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoItemsController"/> class.
        /// </summary>
        /// <param name="demoItemService">The demo item service.</param>
        public DemoItemsController(IDemoItemService demoItemService)
        {
            _demoItemService = demoItemService ?? throw new ArgumentNullException(nameof(demoItemService));
        }

        /// <summary>
        /// Gets all demo items with pagination.
        /// </summary>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A paginated list of demo items.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            var result = await _demoItemService.GetAllAsync(pageNumber, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Gets a demo item by its ID.
        /// </summary>
        /// <param name="id">The ID of the demo item.</param>
        /// <returns>The demo item.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _demoItemService.GetByIdAsync(id);
            if (!result.IsSuccess)
            {
                return NotFound(result.Errors.Count > 0 ? result.Errors[0] : "Not found");
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Gets demo items by category with pagination.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <returns>A paginated list of demo items in the specified category.</returns>
        [HttpGet("category/{category}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByCategory(string category, int pageNumber = 1, int pageSize = 10)
        {
            var result = await _demoItemService.GetByCategoryAsync(category, pageNumber, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Gets featured demo items.
        /// </summary>
        /// <returns>A collection of featured demo items.</returns>
        [HttpGet("featured")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFeatured()
        {
            var result = await _demoItemService.GetFeaturedAsync();
            return Ok(result);
        }

        /// <summary>
        /// Creates a new demo item.
        /// </summary>
        /// <param name="request">The create demo item request.</param>
        /// <returns>The created demo item.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateDemoItemRequest request)
        {
            var result = await _demoItemService.CreateAsync(request.Name, request.Description, request.Price, request.Category);
            if (!result.IsSuccess)
            {
                return BadRequest(result.Errors.Count > 0 ? result.Errors[0] : "Bad request");
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        /// <summary>
        /// Updates a demo item.
        /// </summary>
        /// <param name="id">The ID of the demo item to update.</param>
        /// <param name="request">The update demo item request.</param>
        /// <returns>The updated demo item.</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDemoItemRequest request)
        {
            var result = await _demoItemService.UpdateAsync(id, request.Name, request.Description, request.Price, request.Category);
            if (!result.IsSuccess)
            {
                if (result.Errors.Count > 0 && result.Errors[0].Contains("not found"))
                {
                    return NotFound(result.Errors[0]);
                }

                return BadRequest(result.Errors.Count > 0 ? result.Errors[0] : "Bad request");
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Sets whether a demo item is featured.
        /// </summary>
        /// <param name="id">The ID of the demo item.</param>
        /// <param name="request">The set featured request.</param>
        /// <returns>The updated demo item.</returns>
        [HttpPatch("{id}/featured")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SetFeatured(Guid id, [FromBody] SetFeaturedRequest request)
        {
            var result = await _demoItemService.SetFeaturedAsync(id, request.IsFeatured);
            if (!result.IsSuccess)
            {
                if (result.Errors.Count > 0 && result.Errors[0].Contains("not found"))
                {
                    return NotFound(result.Errors[0]);
                }

                return BadRequest(result.Errors.Count > 0 ? result.Errors[0] : "Bad request");
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Deletes a demo item.
        /// </summary>
        /// <param name="id">The ID of the demo item to delete.</param>
        /// <returns>No content.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _demoItemService.DeleteAsync(id);
            if (!result.IsSuccess)
            {
                return NotFound(result.Errors.Count > 0 ? result.Errors[0] : "Not found");
            }

            return NoContent();
        }
    }

    /// <summary>
    /// Request to create a demo item.
    /// </summary>
    public class CreateDemoItemRequest
    {
        /// <summary>
        /// Gets or sets the name of the demo item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the demo item.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the price of the demo item.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the category of the demo item.
        /// </summary>
        public string Category { get; set; }
    }

    /// <summary>
    /// Request to update a demo item.
    /// </summary>
    public class UpdateDemoItemRequest
    {
        /// <summary>
        /// Gets or sets the name of the demo item.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the demo item.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the price of the demo item.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the category of the demo item.
        /// </summary>
        public string Category { get; set; }
    }

    /// <summary>
    /// Request to set whether a demo item is featured.
    /// </summary>
    public class SetFeaturedRequest
    {
        /// <summary>
        /// Gets or sets a value indicating whether the demo item is featured.
        /// </summary>
        public bool IsFeatured { get; set; }
    }
}