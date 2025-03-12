using System;

using AAEmu.Game.Models.Game.Family;

using Xunit;

namespace AAEmu.UnitTests.Game.Models.Game.Family;

public class FamilyTests
{
    [Fact]
    public void TestAddMember()
    {
        // Создание объекта Family
        var family = new global::Family
        {
            Id = 1,
            Name = "Test Family",
            Level = 10,
            Exp = 1000,
            Content1 = "Content 1",
            Content2 = "Content 2",
            IncMemberCount = 5,
            ResetTime = DateTime.Now,
            ChangeNameTime = DateTime.Now.AddDays(1)
        };

        // Добавление члена семьи
        var member = new FamilyMember
        {
            Id = 1,
            Name = "Test Member",
            Role = 1,
            Title = "Member Title"
        };
        family.AddMember(member);

        // Проверка, что член семьи был добавлен
        Assert.Single(family.Members);
        Assert.Equal(member.Id, family.Members[0].Id);
        Assert.Equal(member.Name, family.Members[0].Name);
        Assert.Equal(member.Role, family.Members[0].Role);
        Assert.Equal(member.Title, family.Members[0].Title);
    }

    [Fact]
    public void TestRemoveMember()
    {
        // Создание объекта Family
        var family = new global::Family
        {
            Id = 1,
            Name = "Test Family",
            Level = 10,
            Exp = 1000,
            Content1 = "Content 1",
            Content2 = "Content 2",
            IncMemberCount = 5,
            ResetTime = DateTime.Now,
            ChangeNameTime = DateTime.Now.AddDays(1)
        };

        // Добавление члена семьи
        var member = new FamilyMember
        {
            Id = 1,
            Name = "Test Member",
            Role = 1,
            Title = "Member Title"
        };
        family.AddMember(member);

        // Удаление члена семьи
        family.RemoveMember(member);

        // Проверка, что член семьи был удален
        Assert.Empty(family.Members);
    }
}
