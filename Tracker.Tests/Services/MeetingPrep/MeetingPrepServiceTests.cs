using Tracker.Common.Enums;
using Tracker.DataModels;
using Xunit;

namespace Tracker.Tests.Services.MeetingPrep
{
    /// <summary>
    /// Tests for meeting prep data models and helper methods.
    /// Note: Full service integration tests require database setup.
    /// These tests focus on the model logic that can be tested in isolation.
    /// </summary>
    public class MeetingPrepModelTests
    {
        #region MeetingPrep Model Tests

        [Fact]
        public void MeetingPrep_NewInstance_HasEmptySections()
        {
            // Act
            var prep = new Tracker.DataModels.MeetingPrep();

            // Assert
            Assert.NotNull(prep.Sections);
            Assert.Empty(prep.Sections);
        }

        [Fact]
        public void MeetingPrep_GetOrCreateSection_CreatesNewSection()
        {
            // Arrange
            var prep = new Tracker.DataModels.MeetingPrep();

            // Act
            var section = prep.GetOrCreateSection(PrepSectionType.Urgent);

            // Assert
            Assert.NotNull(section);
            Assert.Equal(PrepSectionType.Urgent, section.Type);
            Assert.Single(prep.Sections);
        }

        [Fact]
        public void MeetingPrep_GetOrCreateSection_ReturnsSameInstance()
        {
            // Arrange
            var prep = new Tracker.DataModels.MeetingPrep();
            var section1 = prep.GetOrCreateSection(PrepSectionType.Urgent);

            // Act
            var section2 = prep.GetOrCreateSection(PrepSectionType.Urgent);

            // Assert
            Assert.Same(section1, section2);
            Assert.Single(prep.Sections);
        }

        [Fact]
        public void MeetingPrep_AddItem_CreatesSection()
        {
            // Arrange
            var prep = new Tracker.DataModels.MeetingPrep();
            var item = new PrepItem { Title = "Test Item" };

            // Act
            prep.AddItem(PrepSectionType.TaskStatus, item);

            // Assert
            Assert.Single(prep.Sections);
            var section = prep.Sections[0];
            Assert.Equal(PrepSectionType.TaskStatus, section.Type);
            Assert.Single(section.Items);
        }

        [Fact]
        public void MeetingPrep_TotalItemCount_CountsAllItems()
        {
            // Arrange
            var prep = new Tracker.DataModels.MeetingPrep();
            prep.AddItem(PrepSectionType.Urgent, new PrepItem { Title = "Item 1" });
            prep.AddItem(PrepSectionType.Urgent, new PrepItem { Title = "Item 2" });
            prep.AddItem(PrepSectionType.TaskStatus, new PrepItem { Title = "Item 3" });

            // Act
            var count = prep.TotalItemCount;

            // Assert
            Assert.Equal(3, count);
        }

        [Fact]
        public void MeetingPrep_PruneEmptySections_RemovesSectionsWithNoItems()
        {
            // Arrange
            var prep = new Tracker.DataModels.MeetingPrep();
            prep.GetOrCreateSection(PrepSectionType.Urgent); // Empty
            prep.AddItem(PrepSectionType.TaskStatus, new PrepItem { Title = "Has Item" });
            prep.GetOrCreateSection(PrepSectionType.GoalProgress); // Empty

            // Act
            prep.PruneEmptySections();

            // Assert
            Assert.Single(prep.Sections);
            Assert.Equal(PrepSectionType.TaskStatus, prep.Sections[0].Type);
        }

        [Fact]
        public void MeetingPrep_SortAllItems_SortsByPriority()
        {
            // Arrange
            var prep = new Tracker.DataModels.MeetingPrep();
            prep.AddItem(PrepSectionType.Urgent, new PrepItem { Title = "Low", Priority = PrepItemPriority.Low });
            prep.AddItem(PrepSectionType.Urgent, new PrepItem { Title = "Critical", Priority = PrepItemPriority.Critical });
            prep.AddItem(PrepSectionType.Urgent, new PrepItem { Title = "High", Priority = PrepItemPriority.High });
            prep.AddItem(PrepSectionType.Urgent, new PrepItem { Title = "Normal", Priority = PrepItemPriority.Normal });

            // Act
            prep.SortAllItems();

            // Assert
            var items = prep.Sections[0].Items;
            Assert.Equal("Critical", items[0].Title);
            Assert.Equal("High", items[1].Title);
            Assert.Equal("Normal", items[2].Title);
            Assert.Equal("Low", items[3].Title);
        }

        [Fact]
        public void MeetingPrep_LimitItemsPerSection_TruncatesItems()
        {
            // Arrange
            var prep = new Tracker.DataModels.MeetingPrep();
            for (int i = 0; i < 10; i++)
            {
                prep.AddItem(PrepSectionType.Urgent, new PrepItem { Title = $"Item {i}" });
            }

            // Act
            prep.LimitItemsPerSection(5);

            // Assert
            Assert.Equal(5, prep.Sections[0].Items.Count);
        }

        #endregion

        #region PrepSection Factory Tests

        [Fact]
        public void PrepSection_Create_Urgent_HasCorrectProperties()
        {
            // Act
            var section = PrepSection.Create(PrepSectionType.Urgent);

            // Assert
            Assert.Contains("Urgent", section.Title);
            Assert.Equal(PrepSectionType.Urgent, section.Type);
            Assert.NotEmpty(section.Icon);
        }

        [Fact]
        public void PrepSection_Create_TaskStatus_HasCorrectProperties()
        {
            // Act
            var section = PrepSection.Create(PrepSectionType.TaskStatus);

            // Assert
            Assert.Contains("Task Status", section.Title);
            Assert.Equal(PrepSectionType.TaskStatus, section.Type);
        }

        [Fact]
        public void PrepSection_Create_GoalProgress_HasCorrectProperties()
        {
            // Act
            var section = PrepSection.Create(PrepSectionType.GoalProgress);

            // Assert
            Assert.Contains("Goal Progress", section.Title);
            Assert.Equal(PrepSectionType.GoalProgress, section.Type);
        }

        [Fact]
        public void PrepSection_Create_Recognition_HasCorrectProperties()
        {
            // Act
            var section = PrepSection.Create(PrepSectionType.Recognition);

            // Assert
            Assert.Contains("Recognition", section.Title);
            Assert.Equal(PrepSectionType.Recognition, section.Type);
        }

        #endregion

        #region PrepItem Tests

        [Fact]
        public void PrepItem_ToAgendaText_WithTitle_ReturnsTitle()
        {
            // Arrange
            var item = new PrepItem { Title = "Test Task" };

            // Act
            var result = item.ToAgendaText();

            // Assert
            Assert.Equal("Test Task", result);
        }

        [Fact]
        public void PrepItem_ToAgendaText_WithTitleAndSubtext_ReturnsFormatted()
        {
            // Arrange
            var item = new PrepItem
            {
                Title = "Review OKR",
                Subtext = "Q4 Revenue Target"
            };

            // Act
            var result = item.ToAgendaText();

            // Assert
            Assert.Equal("Review OKR (Q4 Revenue Target)", result);
        }

        [Fact]
        public void PrepItem_DefaultPriority_IsNormal()
        {
            // Act
            var item = new PrepItem { Title = "Test" };

            // Assert
            Assert.Equal(PrepItemPriority.Normal, item.Priority);
        }

        [Fact]
        public void PrepItem_IsAddedToAgenda_DefaultsFalse()
        {
            // Act
            var item = new PrepItem { Title = "Test" };

            // Assert
            Assert.False(item.IsAddedToAgenda);
        }

        [Fact]
        public void PrepItem_WithLinkType_HasLinkInfo()
        {
            // Arrange
            var item = new PrepItem
            {
                Title = "Task Review",
                LinkType = PrepItemLinkType.Task,
                LinkId = 42
            };

            // Assert
            Assert.Equal(PrepItemLinkType.Task, item.LinkType);
            Assert.Equal(42, item.LinkId);
        }

        #endregion

        #region Statistics Tests

        [Fact]
        public void MeetingPrep_OverdueTaskCount_DefaultsToZero()
        {
            // Act
            var prep = new Tracker.DataModels.MeetingPrep();

            // Assert
            Assert.Equal(0, prep.OverdueTaskCount);
        }

        [Fact]
        public void MeetingPrep_CanSetStatistics()
        {
            // Arrange
            var prep = new Tracker.DataModels.MeetingPrep
            {
                OverdueTaskCount = 3,
                OpenActionItemCount = 5,
                OkrsAtRiskCount = 2,
                DaysSinceLastMeeting = 14
            };

            // Assert
            Assert.Equal(3, prep.OverdueTaskCount);
            Assert.Equal(5, prep.OpenActionItemCount);
            Assert.Equal(2, prep.OkrsAtRiskCount);
            Assert.Equal(14, prep.DaysSinceLastMeeting);
        }

        #endregion

        #region AI Suggestions Tests

        [Fact]
        public void MeetingPrep_AiSuggestedAgenda_DefaultsEmpty()
        {
            // Act
            var prep = new Tracker.DataModels.MeetingPrep();

            // Assert
            Assert.NotNull(prep.AiSuggestedAgenda);
            Assert.Empty(prep.AiSuggestedAgenda);
        }

        [Fact]
        public void MeetingPrep_CanSetAiSuggestedAgenda()
        {
            // Arrange
            var prep = new Tracker.DataModels.MeetingPrep
            {
                AiSuggestedAgenda = "• Discuss project timeline\n• Review team capacity"
            };

            // Assert
            Assert.Contains("Discuss project timeline", prep.AiSuggestedAgenda);
        }

        #endregion
    }
}
