using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.ViewModels.DialogViewModels;
using Xunit;

namespace Tracker.Tests.ViewModels
{
    public class TemplatePreviewViewModelTests
    {
        [Fact]
        public void Constructor_WithValidTemplate_ShouldSetProperties()
        {
            // Arrange
            var template = CreateTestTemplate();

            // Act
            var vm = new TemplatePreviewViewModel(template);

            // Assert
            vm.TemplateName.Should().Be("Annual Review");
            vm.TemplateDescription.Should().Be("Annual performance review template");
            vm.SectionCount.Should().Be(2);
            vm.QuestionCount.Should().Be(3);
        }

        [Fact]
        public void Constructor_WithEmptyTemplate_ShouldHandleGracefully()
        {
            // Arrange
            var template = new ReviewTemplate
            {
                Name = "Empty Template",
                Description = "No sections"
            };

            // Act
            var vm = new TemplatePreviewViewModel(template);

            // Assert
            vm.TemplateName.Should().Be("Empty Template");
            vm.SectionCount.Should().Be(0);
            vm.QuestionCount.Should().Be(0);
            vm.Sections.Should().BeEmpty();
        }

        [Fact]
        public void Sections_ShouldBeOrderedBySortOrder()
        {
            // Arrange
            var template = CreateTestTemplate();
            var sections = template.Sections.ToList();
            sections[0].SortOrder = 2;
            sections[1].SortOrder = 1;

            // Act
            var vm = new TemplatePreviewViewModel(template);

            // Assert
            vm.Sections[0].Title.Should().Be("Goals"); // SortOrder 1
            vm.Sections[1].Title.Should().Be("Performance"); // SortOrder 2
        }

        [Fact]
        public void PreviewSectionViewModel_ShouldPopulateQuestions()
        {
            // Arrange
            var template = CreateTestTemplate();

            // Act
            var vm = new TemplatePreviewViewModel(template);

            // Assert
            vm.Sections[0].Questions.Should().HaveCount(2);
            vm.Sections[0].Questions[0].Text.Should().Be("Rate performance");
        }

        [Fact]
        public void PreviewQuestionViewModel_ShouldDisplayQuestionType()
        {
            // Arrange
            var template = CreateTestTemplate();

            // Act
            var vm = new TemplatePreviewViewModel(template);

            // Assert
            vm.Sections[0].Questions[0].QuestionType.Should().Be(ReviewQuestionType.Rating);
        }

        [Fact]
        public void PreviewSectionViewModel_HasDescription_ShouldBeTrueWhenDescriptionExists()
        {
            // Arrange
            var template = CreateTestTemplate();
            template.Sections.First().Description = "Section description";

            // Act
            var vm = new TemplatePreviewViewModel(template);

            // Assert
            vm.Sections[0].HasDescription.Should().BeTrue();
        }

        [Fact]
        public void PreviewSectionViewModel_HasDescription_ShouldBeFalseWhenEmpty()
        {
            // Arrange
            var template = CreateTestTemplate();
            template.Sections.First().Description = string.Empty;

            // Act
            var vm = new TemplatePreviewViewModel(template);

            // Assert
            vm.Sections[0].HasDescription.Should().BeFalse();
        }

        private static ReviewTemplate CreateTestTemplate()
        {
            return new ReviewTemplate
            {
                Name = "Annual Review",
                Description = "Annual performance review template",
                Sections = new List<ReviewTemplateSection>
                {
                    new ReviewTemplateSection
                    {
                        Title = "Performance",
                        SortOrder = 1,
                        Questions = new List<ReviewTemplateQuestion>
                        {
                            new ReviewTemplateQuestion
                            {
                                Text = "Rate performance",
                                QuestionType = ReviewQuestionType.Rating,
                                SortOrder = 1
                            },
                            new ReviewTemplateQuestion
                            {
                                Text = "Comments",
                                QuestionType = ReviewQuestionType.LongText,
                                SortOrder = 2
                            }
                        }
                    },
                    new ReviewTemplateSection
                    {
                        Title = "Goals",
                        SortOrder = 2,
                        Questions = new List<ReviewTemplateQuestion>
                        {
                            new ReviewTemplateQuestion
                            {
                                Text = "Goals met?",
                                QuestionType = ReviewQuestionType.YesNo,
                                SortOrder = 1
                            }
                        }
                    }
                }
            };
        }
    }
}
