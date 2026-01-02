using Tracker.DataModels;
using Tracker.Help.ViewModels;
using Tracker.ViewModels;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.Factories
{
    /// <summary>
    /// Factory interface for creating ViewModels with dependency injection.
    /// </summary>
    public interface IViewModelFactory
    {
        /// <summary>
        /// Creates a ViewModel of the specified type.
        /// </summary>
        /// <typeparam name="T">The ViewModel type to create.</typeparam>
        /// <returns>A new instance of the ViewModel.</returns>
        T Create<T>() where T : BaseViewModel;

        /// <summary>
        /// Creates a SettingsViewModel with the specified callback.
        /// </summary>
        /// <param name="callback">The callback to invoke when the dialog closes.</param>
        /// <returns>A new SettingsViewModel instance.</returns>
        SettingsViewModel CreateSettingsViewModel(Action? callback);

        /// <summary>
        /// Creates a ReportsViewModel with the specified callback.
        /// </summary>
        /// <param name="callback">The callback to invoke when the dialog closes.</param>
        /// <returns>A new ReportsViewModel instance.</returns>
        ReportsViewModel CreateReportsViewModel(Action? callback);

        /// <summary>
        /// Creates a SetupWizardViewModel with the specified callback.
        /// </summary>
        /// <param name="callback">The callback to invoke when the wizard closes.</param>
        /// <returns>A new SetupWizardViewModel instance.</returns>
        SetupWizardViewModel CreateSetupWizardViewModel(Action? callback);

        /// <summary>
        /// Creates a LoginDialogViewModel with the specified callback.
        /// </summary>
        /// <param name="callback">The callback to invoke when the dialog closes.</param>
        /// <returns>A new LoginDialogViewModel instance.</returns>
        LoginDialogViewModel CreateLoginDialogViewModel(Action? callback);

        /// <summary>
        /// Creates a SendKudosViewModel with the specified callback and optional pre-selected member.
        /// </summary>
        /// <param name="callback">The callback to invoke when the dialog closes.</param>
        /// <param name="preselectedMember">Optional team member to pre-select.</param>
        /// <returns>A new SendKudosViewModel instance.</returns>
        //SendKudosViewModel CreateSendKudosViewModel(Action callback, TeamMember? preselectedMember = null);

        /// <summary>
        /// Creates a QuickMessageViewModel with the specified callback.
        /// </summary>
        /// <param name="callback">The callback to invoke when the dialog closes.</param>
        /// <returns>A new QuickMessageViewModel instance.</returns>
        QuickMessageViewModel CreateQuickMessageViewModel(Action callback);
    }
}
