using FeraPrompt.Api.Data;
using FeraPrompt.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FeraPrompt.Api.Repositories;

/// <summary>
/// Interface para operações de Prompt no banco de dados
/// </summary>
public interface IPromptRepository
{
    Task<Prompt?> GetByIdAsync(int id);
    Task<List<Prompt>> GetAllAsync();
    Task<Prompt> CreateAsync(Prompt prompt);
    Task<Prompt> UpdateAsync(Prompt prompt);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

/// <summary>
/// Implementação do repositório de Prompts
/// </summary>
public class PromptRepository : IPromptRepository
{
    private readonly ApplicationDbContext _context;

    public PromptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Prompt?> GetByIdAsync(int id)
    {
        return await _context.Prompts
            .Include(p => p.PromptHistories)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Prompt>> GetAllAsync()
    {
        return await _context.Prompts
            .Include(p => p.PromptHistories)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Prompt> CreateAsync(Prompt prompt)
    {
        _context.Prompts.Add(prompt);
        await _context.SaveChangesAsync();
        return prompt;
    }

    public async Task<Prompt> UpdateAsync(Prompt prompt)
    {
        _context.Entry(prompt).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return prompt;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var prompt = await _context.Prompts.FindAsync(id);
        if (prompt == null)
            return false;

        _context.Prompts.Remove(prompt);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Prompts.AnyAsync(p => p.Id == id);
    }
}
