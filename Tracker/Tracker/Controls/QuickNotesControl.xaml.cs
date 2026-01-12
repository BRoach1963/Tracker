using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tracker.Classes;
using Tracker.DataModels;
using Tracker.Database;
using Tracker.Database.Repositories;
using Tracker.Logging;
using Tracker.Services;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Quick Notes control with master-detail layout.
    /// </summary>
    public partial class QuickNotesControl : UserControl
    {
        public QuickNotesControl()
        {
            InitializeComponent();
            try
            {
                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue || userId.Value == Guid.Empty)
                {
                    var logger = LoggingManager.GetComponentLogger("QuickNotesControl");
                    logger.Warn("Cannot initialize QuickNotesViewModel: no current user in OrganizationContext");
                    return;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                var context = contextFactory.CreateContext();

                var quickNoteRepository = new QuickNoteRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                DataContext = new QuickNotesViewModel(quickNoteRepository);
            }
            catch (Exception ex)
            {
                var logger = LoggingManager.GetComponentLogger("QuickNotesControl");
                logger.Exception(ex, "Failed to initialize QuickNotesViewModel");
            }
        }

        private void Note_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is QuickNote note)
            {
                if (DataContext is QuickNotesViewModel vm)
                {
                    vm.SelectedNote = note;
                }
            }
        }
    }
}
