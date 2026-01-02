using FluentAssertions;
using Tracker.Common.Enums;
using Tracker.DataModels;
using Tracker.ViewModels.DialogViewModels;
using Xunit;

namespace Tracker.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for PerformanceReviewDialogViewModel.
    /// </summary>
    public class PerformanceReviewDialogViewModelTests : IDisposable
    {
        private PerformanceReviewDialogViewModel? _viewModel;

        public void Dispose()
        {
            _viewModel?.Dispose();
        }

        #region Test Helpers

        private static ReviewTemplate CreateTestTemplate(int sectionCount = 2, int questionsPerSection = 3)
        {
            var template = new ReviewTemplate
            {
                Id = 1,
                Name = "Test Template",
                Sections = new List<ReviewTemplateSection>()
            };

            for (int s = 1; s <= sectionCount; s++)
            {
                var section = new ReviewTemplateSection
                {
                    Id = s,
                    Title = $"Section {s}",
                    Description = s == 1 ? "Section with description" : string.Empty,
                    SortOrder = s,
                    Questions = new List<ReviewTemplateQuestion>()
                };

                for (int q = 1; q <= questionsPerSection; q++)
                {
                    section.Questions.Add(new ReviewTemplateQuestion
                    {
                        Id = (s - 1) * questionsPerSection + q,
                        Text = $"Question {q} in Section {s}",
                        QuestionType = q == 1 ? ReviewQuestionType.Rating :
                                       q == 2 ? ReviewQuestionType.YesNo :
                                       ReviewQuestionType.LongText,
                        IsRequired = q <= 2,
                        SortOrder = q
                    });
                }

                template.Sections.Add(section);
            }

            return template;
        }

        private static PerformanceReview CreateTestReview(ReviewTemplate template, string teamMemberName = "John Doe", string cycleName = "2024 Q1 Review")
        {
            return new PerformanceReview
            {
                Id = 1,
                TeamMember = new TeamMember { FirstName = "John", LastName = "Doe" },
                PerformanceReviewCycle = new PerformanceReviewCycle { Name = cycleName, ReviewTemplate = template },
                Status = ReviewStatus.NotStarted,
                Sections = new List<PerformanceReviewSection>()
            };
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidInputs_SetsRevieweeName()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.RevieweeName.Should().Be("John Doe");
        }

        [Fact]
        public void Constructor_WithValidInputs_SetsCycleName()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template, cycleName: "2024 Annual Review");

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.CycleName.Should().Be("2024 Annual Review");
        }

        [Fact]
        public void Constructor_WithNullTeamMember_UsesDefaultName()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = new PerformanceReview
            {
                Id = 1,
                TeamMember = null!,
                PerformanceReviewCycle = new PerformanceReviewCycle { Name = "Test Cycle", ReviewTemplate = template },
                Status = ReviewStatus.NotStarted,
                Sections = new List<PerformanceReviewSection>()
            };

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.RevieweeName.Should().Be("Team Member");
        }

        [Fact]
        public void Constructor_BuildsSectionsFromTemplate()
        {
            // Arrange
            var template = CreateTestTemplate(sectionCount: 3, questionsPerSection: 2);
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.Sections.Should().HaveCount(3);
        }

        [Fact]
        public void Constructor_SetsInitialSaveStatus()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.SaveStatus.Should().Be("All changes saved");
        }

        #endregion

        #region Section Tests

        [Fact]
        public void Sections_ShouldBeOrderedBySortOrder()
        {
            // Arrange
            var template = CreateTestTemplate(sectionCount: 3);
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.Sections[0].Title.Should().Be("Section 1");
            _viewModel.Sections[1].Title.Should().Be("Section 2");
            _viewModel.Sections[2].Title.Should().Be("Section 3");
        }

        [Fact]
        public void Sections_ShouldPopulateQuestionsCorrectly()
        {
            // Arrange
            var template = CreateTestTemplate(sectionCount: 1, questionsPerSection: 4);
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.Sections[0].Questions.Should().HaveCount(4);
        }

        #endregion

        #region TotalQuestions Tests

        [Fact]
        public void TotalQuestions_ShouldSumAllQuestions()
        {
            // Arrange
            var template = CreateTestTemplate(sectionCount: 2, questionsPerSection: 3);
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.TotalQuestions.Should().Be(6);
        }

        [Fact]
        public void TotalQuestions_WithEmptyTemplate_ShouldBeZero()
        {
            // Arrange
            var template = new ReviewTemplate
            {
                Id = 1,
                Name = "Empty Template",
                Sections = new List<ReviewTemplateSection>()
            };
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.TotalQuestions.Should().Be(0);
        }

        #endregion

        #region CompletedQuestions Tests

        [Fact]
        public void CompletedQuestions_InitiallyZero()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.CompletedQuestions.Should().Be(0);
        }

        #endregion

        #region CanSubmit Tests

        [Fact]
        public void CanSubmit_WhenRequiredQuestionsNotAnswered_ReturnsFalse()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.CanSubmit.Should().BeFalse();
        }

        [Fact]
        public void CanSubmit_WhenAllRequiredQuestionsAnswered_ReturnsTrue()
        {
            // Arrange
            var template = new ReviewTemplate
            {
                Id = 1,
                Name = "Simple Template",
                Sections = new List<ReviewTemplateSection>
                {
                    new ReviewTemplateSection
                    {
                        Id = 1,
                        Title = "Section 1",
                        SortOrder = 1,
                        Questions = new List<ReviewTemplateQuestion>
                        {
                            new ReviewTemplateQuestion
                            {
                                Id = 1,
                                Text = "Optional Question",
                                QuestionType = ReviewQuestionType.LongText,
                                IsRequired = false,
                                SortOrder = 1
                            }
                        }
                    }
                }
            };
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert - No required questions, so CanSubmit should be true
            _viewModel.CanSubmit.Should().BeTrue();
        }

        #endregion

        #region ReviewSectionViewModel Tests

        [Fact]
        public void ReviewSectionViewModel_HasDescription_WhenDescriptionProvided()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert - First section has description
            _viewModel.Sections[0].HasDescription.Should().BeTrue();
            _viewModel.Sections[0].Description.Should().Be("Section with description");
        }

        [Fact]
        public void ReviewSectionViewModel_HasDescription_FalseWhenNoDescription()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert - Second section has no description
            _viewModel.Sections[1].HasDescription.Should().BeFalse();
        }

        [Fact]
        public void ReviewSectionViewModel_QuestionCount_ReturnsCorrectCount()
        {
            // Arrange
            var template = CreateTestTemplate(sectionCount: 1, questionsPerSection: 5);
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.Sections[0].QuestionCount.Should().Be(5);
        }

        [Fact]
        public void ReviewSectionViewModel_AnsweredCount_InitiallyZero()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.Sections[0].AnsweredCount.Should().Be(0);
        }

        [Fact]
        public void ReviewSectionViewModel_IsComplete_FalseWhenRequiredUnanswered()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);

            // Act
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Assert
            _viewModel.Sections[0].IsComplete.Should().BeFalse();
        }

        #endregion

        #region ReviewQuestionViewModel Tests

        [Fact]
        public void ReviewQuestionViewModel_RatingQuestion_IsAnsweredWhenRatingSet()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Act - Set rating on first question (Rating type)
            var ratingQuestion = _viewModel.Sections[0].Questions[0];
            ratingQuestion.RatingValue = 4;

            // Assert
            ratingQuestion.IsAnswered.Should().BeTrue();
        }

        [Fact]
        public void ReviewQuestionViewModel_RatingQuestion_NotAnsweredWhenNull()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Act - Don't set rating
            var ratingQuestion = _viewModel.Sections[0].Questions[0];

            // Assert
            ratingQuestion.IsAnswered.Should().BeFalse();
        }

        [Fact]
        public void ReviewQuestionViewModel_YesNoQuestion_IsAnsweredWhenYes()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Act - Second question is YesNo type
            var yesNoQuestion = _viewModel.Sections[0].Questions[1];
            yesNoQuestion.IsYes = true;

            // Assert
            yesNoQuestion.IsAnswered.Should().BeTrue();
            yesNoQuestion.Answer.Should().Be("Yes");
        }

        [Fact]
        public void ReviewQuestionViewModel_YesNoQuestion_IsAnsweredWhenNo()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Act
            var yesNoQuestion = _viewModel.Sections[0].Questions[1];
            yesNoQuestion.IsNo = true;

            // Assert
            yesNoQuestion.IsAnswered.Should().BeTrue();
            yesNoQuestion.Answer.Should().Be("No");
        }

        [Fact]
        public void ReviewQuestionViewModel_OpenEndedQuestion_IsAnsweredWhenTextProvided()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Act - Third question is OpenEnded type
            var openEndedQuestion = _viewModel.Sections[0].Questions[2];
            openEndedQuestion.Answer = "This is my detailed response.";

            // Assert
            openEndedQuestion.IsAnswered.Should().BeTrue();
        }

        [Fact]
        public void ReviewQuestionViewModel_OpenEndedQuestion_NotAnsweredWhenWhitespace()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Act
            var openEndedQuestion = _viewModel.Sections[0].Questions[2];
            openEndedQuestion.Answer = "   ";

            // Assert
            openEndedQuestion.IsAnswered.Should().BeFalse();
        }

        [Fact]
        public void ReviewQuestionViewModel_QuestionId_HasCorrectFormat()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Act
            var question = _viewModel.Sections[0].Questions[0];

            // Assert
            question.QuestionId.Should().StartWith("Question_");
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Act & Assert
            var action = () => _viewModel.Dispose();
            action.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var template = CreateTestTemplate();
            var review = CreateTestReview(template);
            _viewModel = new PerformanceReviewDialogViewModel(review, template);

            // Act & Assert
            var action = () =>
            {
                _viewModel.Dispose();
                _viewModel.Dispose();
            };
            action.Should().NotThrow();
        }

        #endregion
    }
}
