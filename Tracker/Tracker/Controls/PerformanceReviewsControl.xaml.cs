using System.Windows.Controls;
using Tracker.Classes;
using Tracker.Database;
using Tracker.Database.Repositories;
using Tracker.Logging;
using Tracker.Services;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Performance Reviews control for managing review templates and cycles.
    /// </summary>
    public partial class PerformanceReviewsControl : UserControl
    {
        public PerformanceReviewsControl()
        {
            InitializeComponent();
            try
            {
                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue || userId.Value == Guid.Empty)
                {
                    var logger = LoggingManager.GetComponentLogger("PerformanceReviewsControl");
                    logger.Warn("Cannot initialize PerformanceReviewsViewModel: no current user in OrganizationContext");
                    return;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                var context = contextFactory.CreateContext();

                var reviewCycleRepository = new ReviewCycleRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                var performanceReviewRepository = new PerformanceReviewRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                DataContext = new PerformanceReviewsViewModel(reviewCycleRepository, performanceReviewRepository);
            }
            catch (Exception ex)
            {
                var logger = LoggingManager.GetComponentLogger("PerformanceReviewsControl");
                logger.Exception(ex, "Failed to initialize PerformanceReviewsViewModel");
            }
        }
    }
}
