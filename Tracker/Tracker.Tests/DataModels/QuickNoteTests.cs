using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.Tests.Infrastructure;

namespace Tracker.Tests.DataModels
{
    public class QuickNoteTests
    {
        [Fact]
        public void NewQuickNote_ShouldHaveDefaultValues()
        {
            var note = new QuickNote();

            note.Id.Should().Be(0);
            note.Content.Should().Be(string.Empty);
            note.Category.Should().Be(NoteCategory.General);
            note.IsPinned.Should().BeFalse();
            note.IsArchived.Should().BeFalse();
        }

        [Fact]
        public void TestDataBuilder_ShouldCreateValidQuickNote()
        {
            var note = TestDataBuilder.CreateQuickNote("Test content", NoteCategory.Meeting);

            note.Content.Should().Be("Test content");
            note.Category.Should().Be(NoteCategory.Meeting);
        }

        [Theory]
        [InlineData(NoteCategory.General)]
        [InlineData(NoteCategory.Meeting)]
        [InlineData(NoteCategory.Idea)]
        [InlineData(NoteCategory.Todo)]
        [InlineData(NoteCategory.Decision)]
        public void Category_ShouldAcceptAllValidValues(NoteCategory category)
        {
            var note = new QuickNote { Category = category };
            note.Category.Should().Be(category);
        }

        [Fact]
        public void IsPinned_ShouldBeToggleable()
        {
            var note = new QuickNote { IsPinned = false };
            
            note.IsPinned = true;
            note.IsPinned.Should().BeTrue();
            
            note.IsPinned = false;
            note.IsPinned.Should().BeFalse();
        }

        [Fact]
        public void IsArchived_ShouldBeToggleable()
        {
            var note = new QuickNote { IsArchived = false };
            
            note.IsArchived = true;
            note.IsArchived.Should().BeTrue();
        }

        [Fact]
        public void Tags_ShouldBeSettable()
        {
            var note = new QuickNote { Tags = "important,urgent" };
            note.Tags.Should().Be("important,urgent");
        }
    }
}

