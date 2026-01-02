using System;
using Tracker.ViewModels.DialogViewModels;

namespace Tracker.ViewModels.DialogViewModels
{
    /// <summary>
    /// ViewModel for the Performance Review Dialog - Currently disabled/stubbed
    /// </summary>
    public class PerformanceReviewDialogViewModel : BaseDialogViewModel
    {
        /// <summary>
        /// Event raised when the dialog should be closed.
        /// </summary>
        public event EventHandler<bool>? RequestClose;

        public PerformanceReviewDialogViewModel(Action? closeCallback = null) : base(closeCallback)
        {
        }

        /// <summary>
        /// Raises the RequestClose event.
        /// </summary>
        protected void OnRequestClose(bool result)
        {
            RequestClose?.Invoke(this, result);
            _closeCallback?.Invoke();
        }

        private Action? _closeCallback;
    }
}

