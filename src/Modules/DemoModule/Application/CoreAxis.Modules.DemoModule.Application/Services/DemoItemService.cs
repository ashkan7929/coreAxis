using CoreAxis.Modules.DemoModule.Domain;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DemoModule.Application.Services
{
    /// <summary>
    /// Implementation of the demo item service.
    /// </summary>
    public class DemoItemService : IDemoItemService
    {
        private readonly IDemoItemRepository _demoItemRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DemoItemService"/> class.
        /// </summary>
        /// <param name="demoItemRepository">The demo item repository.</param>
        public DemoItemService(IDemoItemRepository demoItemRepository)
        {
            _demoItemRepository = demoItemRepository ?? throw new ArgumentNullException(nameof(demoItemRepository));
        }

        /// <inheritdoc/>
        public async Task<Result<DemoItem>> GetByIdAsync(Guid id)
        {
            var demoItem = await _demoItemRepository.GetByIdAsync(id);
            if (demoItem == null)
            {
                return Result<DemoItem>.Failure($"Demo item with ID {id} not found.");
            }

            return Result<DemoItem>.Success(demoItem);
        }

        /// <inheritdoc/>
        public async Task<PaginatedList<DemoItem>> GetAllAsync(int pageNumber, int pageSize)
        {
            var allItems = await _demoItemRepository.GetAllAsync();
            return PaginatedList<DemoItem>.Create(allItems.AsQueryable(), pageNumber, pageSize);
        }

        /// <inheritdoc/>
        public async Task<PaginatedList<DemoItem>> GetByCategoryAsync(string category, int pageNumber, int pageSize)
        {
            var categoryItems = await _demoItemRepository.GetByCategoryAsync(category);
            return PaginatedList<DemoItem>.Create(categoryItems.AsQueryable(), pageNumber, pageSize);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<DemoItem>> GetFeaturedAsync()
        {
            return await _demoItemRepository.GetFeaturedAsync();
        }

        /// <inheritdoc/>
        public async Task<Result<DemoItem>> CreateAsync(string name, string description, decimal price, string category)
        {
            try
            {
                var demoItem = DemoItem.Create(name, description, price, category);
                await _demoItemRepository.AddAsync(demoItem);
                return Result<DemoItem>.Success(demoItem);
            }
            catch (ArgumentException ex)
            {
                return Result<DemoItem>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                return Result<DemoItem>.Failure($"An error occurred while creating the demo item: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<Result<DemoItem>> UpdateAsync(Guid id, string name, string description, decimal price, string category)
        {
            try
            {
                var demoItem = await _demoItemRepository.GetByIdAsync(id);
                if (demoItem == null)
                {
                    return Result<DemoItem>.Failure($"Demo item with ID {id} not found.");
                }

                demoItem.Update(name, description, price, category);
                await _demoItemRepository.UpdateAsync(demoItem);
                return Result<DemoItem>.Success(demoItem);
            }
            catch (ArgumentException ex)
            {
                return Result<DemoItem>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                return Result<DemoItem>.Failure($"An error occurred while updating the demo item: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<Result<DemoItem>> SetFeaturedAsync(Guid id, bool isFeatured)
        {
            try
            {
                var demoItem = await _demoItemRepository.GetByIdAsync(id);
                if (demoItem == null)
                {
                    return Result<DemoItem>.Failure($"Demo item with ID {id} not found.");
                }

                demoItem.SetFeatured(isFeatured);
                await _demoItemRepository.UpdateAsync(demoItem);
                return Result<DemoItem>.Success(demoItem);
            }
            catch (Exception ex)
            {
                return Result<DemoItem>.Failure($"An error occurred while updating the featured status: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var demoItem = await _demoItemRepository.GetByIdAsync(id);
                if (demoItem == null)
                {
                    return Result<bool>.Failure($"Demo item with ID {id} not found.");
                }

                await _demoItemRepository.DeleteAsync(demoItem);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"An error occurred while deleting the demo item: {ex.Message}");
            }
        }
    }
}