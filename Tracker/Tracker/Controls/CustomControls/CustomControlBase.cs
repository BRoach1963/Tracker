using System.Windows;
using System.Windows.Controls;

namespace Tracker.Controls.CustomControls
{
    /// <summary>
    /// Base class for custom controls that provides standardized event handling and disposal patterns.
    /// 
    /// This class ensures consistent event subscription/unsubscription across all custom controls,
    /// preventing memory leaks and providing a unified lifecycle management approach.
    /// 
    /// Usage:
    /// <code>
    /// public partial class MyCustomControl : CustomControlBase
    /// {
    ///     protected override void SubscribeToEvents()
    ///     {
    ///         this.SomeEvent += OnSomeEvent;
    ///     }
    ///     
    ///     protected override void UnsubscribeFromEvents()
    ///     {
    ///         this.SomeEvent -= OnSomeEvent;
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class CustomControlBase : UserControl, IDisposable
    {
        #region Fields

        private bool _disposed = false;
        private bool _eventsSubscribed = false;

        #endregion

        #region Constructor

        protected CustomControlBase()
        {
            // Subscribe to events when control is loaded
            Loaded += (s, e) =>
            {
                if (!_eventsSubscribed)
                {
                    SubscribeToEvents();
                    _eventsSubscribed = true;
                }
            };

            // Unsubscribe from events when control is unloaded
            Unloaded += (s, e) =>
            {
                if (_eventsSubscribed)
                {
                    UnsubscribeFromEvents();
                    _eventsSubscribed = false;
                }
            };
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Override this method to subscribe to events in derived classes.
        /// This is called automatically when the control is loaded.
        /// </summary>
        protected virtual void SubscribeToEvents()
        {
            // Override in derived classes
        }

        /// <summary>
        /// Override this method to unsubscribe from events in derived classes.
        /// This is called automatically when the control is unloaded.
        /// </summary>
        protected virtual void UnsubscribeFromEvents()
        {
            // Override in derived classes
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes the control and unsubscribes from all events.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method for proper cleanup.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from all events to prevent memory leaks
                    if (_eventsSubscribed)
                    {
                        UnsubscribeFromEvents();
                        _eventsSubscribed = false;
                    }
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer to ensure cleanup if Dispose is not called.
        /// </summary>
        ~CustomControlBase()
        {
            Dispose(false);
        }

        #endregion
    }
}

