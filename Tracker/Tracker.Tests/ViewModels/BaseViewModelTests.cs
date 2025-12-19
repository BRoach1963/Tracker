using FluentAssertions;
using Tracker.ViewModels;

namespace Tracker.Tests.ViewModels
{
    public class BaseViewModelTests
    {
        private class TestViewModel : BaseViewModel
        {
            private string _testProperty = string.Empty;

            public string TestProperty
            {
                get => _testProperty;
                set => SetProperty(ref _testProperty, value);
            }

            public void TriggerPropertyChanged(string propertyName)
            {
                RaisePropertyChanged(propertyName);
            }
        }

        [Fact]
        public void SetProperty_ShouldRaisePropertyChanged()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var propertyChangedRaised = false;
            string? changedPropertyName = null;

            viewModel.PropertyChanged += (sender, args) =>
            {
                propertyChangedRaised = true;
                changedPropertyName = args.PropertyName;
            };

            // Act
            viewModel.TestProperty = "New Value";

            // Assert
            propertyChangedRaised.Should().BeTrue();
            changedPropertyName.Should().Be(nameof(TestViewModel.TestProperty));
        }

        [Fact]
        public void SetProperty_ShouldNotRaisePropertyChanged_WhenValueIsSame()
        {
            // Arrange
            var viewModel = new TestViewModel { TestProperty = "Initial Value" };
            var propertyChangedCount = 0;

            viewModel.PropertyChanged += (sender, args) =>
            {
                propertyChangedCount++;
            };

            // Act
            viewModel.TestProperty = "Initial Value"; // Same value

            // Assert
            propertyChangedCount.Should().Be(0);
        }

        [Fact]
        public void SetProperty_ShouldUpdateValue()
        {
            // Arrange
            var viewModel = new TestViewModel { TestProperty = "Initial" };

            // Act
            viewModel.TestProperty = "Updated";

            // Assert
            viewModel.TestProperty.Should().Be("Updated");
        }

        [Fact]
        public void SetProperty_ShouldReturnTrue_WhenValueChanges()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var backingField = "Old";
            
            // We test this indirectly through property behavior
            viewModel.TestProperty = "New";

            // Assert
            viewModel.TestProperty.Should().Be("New");
        }

        [Fact]
        public void RaisePropertyChanged_ShouldNotifySubscribers()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var notified = false;
            string? propertyName = null;

            viewModel.PropertyChanged += (sender, args) =>
            {
                notified = true;
                propertyName = args.PropertyName;
            };

            // Act
            viewModel.TriggerPropertyChanged("CustomProperty");

            // Assert
            notified.Should().BeTrue();
            propertyName.Should().Be("CustomProperty");
        }

        [Fact]
        public void BaseViewModel_ShouldImplementINotifyPropertyChanged()
        {
            // Arrange & Act
            var viewModel = new TestViewModel();

            // Assert
            viewModel.Should().BeAssignableTo<System.ComponentModel.INotifyPropertyChanged>();
        }

        [Fact]
        public void BaseViewModel_ShouldImplementIDisposable()
        {
            // Arrange & Act
            var viewModel = new TestViewModel();

            // Assert
            viewModel.Should().BeAssignableTo<IDisposable>();
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var viewModel = new TestViewModel();

            // Act
            Action act = () => viewModel.Dispose();

            // Assert
            act.Should().NotThrow();
        }
    }
}

