using Microsoft.Extensions.Logging.Abstractions;
using YonetIQ.Data.Models.AI;
using YonetIQ.Data.Services.AI;

namespace YonetIQ.Tests.Unit.AI;

public class SkillRegistryTests
{
    private static SkillRegistry CreateRegistry() => new(NullLogger<SkillRegistry>.Instance);

    private static SkillDefinition CreateSkill(string id, string module = "task") => new()
    {
        Id = id,
        Name = $"Test {id}",
        Category = SkillCategory.Task,
        Module = module,
        SystemPromptFile = $"{id}.system.md",
        UserPromptFile = $"{id}.user.md"
    };

    [Fact]
    public void Register_AddsSkill()
    {
        var reg = CreateRegistry();
        reg.Register(CreateSkill("task.clarify"));
        Assert.Single(reg.ListAll());
    }

    [Fact]
    public void Resolve_ExistingSkill_ReturnsIt()
    {
        var reg = CreateRegistry();
        reg.Register(CreateSkill("task.clarify"));
        var skill = reg.Resolve("task.clarify");
        Assert.NotNull(skill);
        Assert.Equal("task.clarify", skill.Id);
    }

    [Fact]
    public void Resolve_NonExistingSkill_ReturnsNull()
    {
        var reg = CreateRegistry();
        Assert.Null(reg.Resolve("nonexistent"));
    }

    [Fact]
    public void ResolveByModule_FiltersCorrectly()
    {
        var reg = CreateRegistry();
        reg.Register(CreateSkill("task.clarify", "task"));
        reg.Register(CreateSkill("meeting.summarize", "meeting"));
        reg.Register(CreateSkill("task.risk", "task"));

        var taskSkills = reg.ResolveByModule("task");
        Assert.Equal(2, taskSkills.Count);
        Assert.All(taskSkills, s => Assert.Equal("task", s.Module));
    }

    [Fact]
    public void Register_DuplicateId_OverwritesPrevious()
    {
        var reg = CreateRegistry();
        reg.Register(CreateSkill("task.clarify"));
        reg.Register(new SkillDefinition
        {
            Id = "task.clarify",
            Name = "Updated Name",
            Category = SkillCategory.Task,
            Module = "task",
            SystemPromptFile = "x.md",
            UserPromptFile = "y.md"
        });

        var skill = reg.Resolve("task.clarify");
        Assert.Equal("Updated Name", skill!.Name);
        Assert.Single(reg.ListAll());
    }

    [Fact]
    public void ListAll_ReturnsAllRegistered()
    {
        var reg = CreateRegistry();
        reg.Register(CreateSkill("a"));
        reg.Register(CreateSkill("b"));
        reg.Register(CreateSkill("c"));
        Assert.Equal(3, reg.ListAll().Count);
    }
}
