using CoreAxis.Modules.DemoModule.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DemoModule.Infrastructure
{
    /// <summary>
    /// Implementation of the demo item repository.
    /// </summary>
    /// <remarks>
    /// This is a simple in-memory implementation for demonstration purposes.
    /// In a real application, this would use a database or other persistent storage.
    /// </remarks>
    public class DemoItemRepository : IDemoItemRepository
    {
        private readonly List<DemoItem> _demoItems = new List<DemoItem>();

        /// <inheritdoc/>
        public Task<DemoItem?> GetByIdAsync(Guid id)
        {
            return Task.FromResult(_demoItems.FirstOrDefault(item => item.Id == id));
        }

        /// <inheritdoc/>
        public Task<IEnumerable<DemoItem>> GetAllAsync()
        {
            return Task.FromResult(_demoItems.AsEnumerable());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<DemoItem>> GetByCategoryAsync(string category)
        {
            return Task.FromResult(_demoItems.Where(item => item.Category == category).AsEnumerable());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<DemoItem>> GetFeaturedAsync()
        {
            return Task.FromResult(_demoItems.Where(item => item.IsFeatured).AsEnumerable());
        }

        /// <inheritdoc/>
        public Task AddAsync(DemoItem demoItem)
        {
            _demoItems.Add(demoItem);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task UpdateAsync(DemoItem demoItem)
        {
            var existingItem = _demoItems.FirstOrDefault(item => item.Id == demoItem.Id);
            if (existingItem != null)
            {
                _demoItems.Remove(existingItem);
                _demoItems.Add(demoItem);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteAsync(DemoItem demoItem)
        {
            _demoItems.RemoveAll(item => item.Id == demoItem.Id);
            return Task.CompletedTask;
        }
    }
}