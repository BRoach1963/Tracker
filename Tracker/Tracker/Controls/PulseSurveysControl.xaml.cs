using System.Windows.Controls;
using Tracker.Classes;
using Tracker.Database;
using Tracker.Services.Data.Repositories;
using Tracker.Logging;
using Tracker.Services;
using Tracker.ViewModels;

namespace Tracker.Controls
{
    /// <summary>
    /// Pulse Surveys control for managing engagement surveys.
    /// </summary>
    public partial class PulseSurveysControl : UserControl
    {
        public PulseSurveysControl()
        {
            InitializeComponent();
            try
            {
                var userId = OrganizationContext.Current.UserIdOrNull;
                if (!userId.HasValue || userId.Value == Guid.Empty)
                {
                    var logger = LoggingManager.GetComponentLogger("PulseSurveysControl");
                    logger.Warn("Cannot initialize PulseSurveysViewModel: no current user in OrganizationContext");
                    return;
                }

                var contextFactory = TrackerDbContextFactory.Instance;
                var context = contextFactory.CreateContext();

                var pulseSurveyRepository = new PulseSurveyRepository(
                    context,
                    userId.Value,
                    () => contextFactory.CreateContext());

                DataContext = new PulseSurveysViewModel(pulseSurveyRepository);
            }
            catch (Exception ex)
            {
                var logger = LoggingManager.GetComponentLogger("PulseSurveysControl");
                logger.Exception(ex, "Failed to initialize PulseSurveysViewModel");
            }
        }
    }
}
