using System;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Infrastructure.Data;
using CoreAxis.SharedKernel.Ports;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Services;

public class FormClient : IFormClient
{
    private readonly DynamicFormDbContext _context;

    public FormClient(DynamicFormDbContext context)
    {
        _context = context;
    }

    public async Task<bool> FormExistsAsync(Guid formId, string? version = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(version))
        {
            return await _context.Forms.AnyAsync(f => f.Id == formId, cancellationToken);
        }

        // If version is specified, check if that version exists
        if (int.TryParse(version, out int v))
        {
            return await _context.FormVersions.AnyAsync(fv => fv.FormId == formId && fv.Version == v, cancellationToken);
        }

        return false;
    }

    public async Task<bool> IsFormPublishedAsync(Guid formId, string version, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(version, out int v))
        {
            return false;
        }

        return await _context.FormVersions.AnyAsync(fv => 
            fv.FormId == formId && 
            fv.Version == v && 
            fv.IsPublished, 
            cancellationToken);
    }
}
