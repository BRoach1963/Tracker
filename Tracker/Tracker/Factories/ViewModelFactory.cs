using Microsoft.Extensions.DependencyInjection;
using Tracker.DataModels;
using Tracker.Help.ViewModels;
using Tracker.ViewModels;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Factories
{
    /// <summary>
    /// Factory for creating ViewModels with dependency injection.
    /// </summary>
    public class ViewModelFactory : IViewModelFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the ViewModelFactory.
        /// </summary>
        /// <param name="serviceProvider">The DI service provider.</param>
        public ViewModelFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public T Create<T>() where T : BaseViewModel
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        /// <inheritdoc/>
        public SettingsViewModel CreateSettingsViewModel(Action? callback)
        {
            var factory = _serviceProvider.GetRequiredService<Func<Action?, SettingsViewModel>>();
            return factory(callback);
        }

        /// <inheritdoc/>
        public ReportsViewModel CreateReportsViewModel(Action? callback)
        {
            var factory = _serviceProvider.GetRequiredService<Func<Action?, ReportsViewModel>>();
            return factory(callback);
        }

        /// <inheritdoc/>
        public SetupWizardViewModel CreateSetupWizardViewModel(Action? callback)
        {
            var factory = _serviceProvider.GetRequiredService<Func<Action?, SetupWizardViewModel>>();
            return factory(callback);
        }

        /// <inheritdoc/>
        public LoginDialogViewModel CreateLoginDialogViewModel(Action? callback)
        {
            var factory = _serviceProvider.GetRequiredService<Func<Action?, LoginDialogViewModel>>();
            return factory(callback);
        }

        /// <inheritdoc/>
        //public SendKudosViewModel CreateSendKudosViewModel(Action callback, TeamMember? preselectedMember = null)
        //{
        //    var factory = _serviceProvider.GetRequiredService<Func<Action, TeamMember?, SendKudosViewModel>>();
        //    return factory(callback, preselectedMember);
        //}

        /// <inheritdoc/>
        public QuickMessageViewModel CreateQuickMessageViewModel(Action callback)
        {
            var factory = _serviceProvider.GetRequiredService<Func<Action, QuickMessageViewModel>>();
            return factory(callback);
        }
    }
}
