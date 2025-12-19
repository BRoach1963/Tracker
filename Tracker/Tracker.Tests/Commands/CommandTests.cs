using FluentAssertions;
using Tracker.Command;

namespace Tracker.Tests.Commands
{
    public class CommandTests
    {
        #region TrackerCommand Tests

        [Fact]
        public void TrackerCommand_Execute_ShouldCallAction()
        {
            bool executed = false;
            var command = new TrackerCommand(_ => executed = true);

            command.Execute(null);

            executed.Should().BeTrue();
        }

        [Fact]
        public void TrackerCommand_CanExecute_ShouldReturnTrue_WhenNoPredicate()
        {
            var command = new TrackerCommand(_ => { });

            var result = command.CanExecute(null);

            result.Should().BeTrue();
        }

        [Fact]
        public void TrackerCommand_CanExecute_ShouldRespectPredicate()
        {
            var command = new TrackerCommand(_ => { }, _ => false);

            var result = command.CanExecute(null);

            result.Should().BeFalse();
        }

        [Fact]
        public void TrackerCommand_CanExecute_ShouldPassParameter()
        {
            object? receivedParam = null;
            var command = new TrackerCommand(_ => { }, p => { receivedParam = p; return true; });

            command.CanExecute("test");

            receivedParam.Should().Be("test");
        }

        [Fact]
        public void TrackerCommand_Execute_ShouldPassParameter()
        {
            object? receivedParam = null;
            var command = new TrackerCommand(p => receivedParam = p);

            command.Execute("test");

            receivedParam.Should().Be("test");
        }

        #endregion

        #region AsyncCommand Tests

        [Fact]
        public async Task AsyncCommand_Execute_ShouldCallAsyncAction()
        {
            bool executed = false;
            var command = new AsyncCommand(async _ => 
            {
                await Task.Delay(10);
                executed = true;
            });

            command.Execute(null);
            await Task.Delay(50); // Wait for async operation

            executed.Should().BeTrue();
        }

        [Fact]
        public void AsyncCommand_CanExecute_ShouldReturnTrue_WhenNoPredicate()
        {
            var command = new AsyncCommand(async _ => await Task.CompletedTask);

            var result = command.CanExecute(null);

            result.Should().BeTrue();
        }

        [Fact]
        public void AsyncCommand_CanExecute_ShouldRespectPredicate()
        {
            var command = new AsyncCommand(async _ => await Task.CompletedTask, _ => false);

            var result = command.CanExecute(null);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AsyncCommand_IsExecuting_ShouldBeTrue_WhileExecuting()
        {
            var tcs = new TaskCompletionSource<bool>();
            var command = new AsyncCommand(async _ => await tcs.Task);

            command.Execute(null);
            await Task.Delay(10);
            
            command.IsExecuting.Should().BeTrue();
            
            tcs.SetResult(true);
            await Task.Delay(50);
            
            command.IsExecuting.Should().BeFalse();
        }

        [Fact]
        public async Task AsyncCommand_CanExecute_ShouldBeFalse_WhileExecuting()
        {
            var tcs = new TaskCompletionSource<bool>();
            var command = new AsyncCommand(async _ => await tcs.Task);

            command.Execute(null);
            await Task.Delay(10);
            
            command.CanExecute(null).Should().BeFalse();
            
            tcs.SetResult(true);
        }

        #endregion

        #region Command Parameter Tests

        [Fact]
        public void Command_WithTypedParameter_ShouldCastCorrectly()
        {
            int receivedValue = 0;
            var command = new TrackerCommand(p => 
            {
                if (p is int intValue)
                    receivedValue = intValue;
            });

            command.Execute(42);

            receivedValue.Should().Be(42);
        }

        [Fact]
        public void Command_WithNullParameter_ShouldNotThrow()
        {
            var command = new TrackerCommand(_ => { });

            var action = () => command.Execute(null);

            action.Should().NotThrow();
        }

        #endregion
    }
}
