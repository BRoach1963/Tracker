using System.Windows.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using Tracker.Logging;
using Tracker.Managers;
using Tracker.Services;
using Tracker.Services.Data;
using Tracker.Services.Data.Repositories;
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

                // Create repository directly with Dapper connection factory
                var connectionFactory = new DapperConnectionFactory();
                var pulseSurveyRepository = new PulseSurveyRepository(
                    connectionFactory, 
                    NullLogger<PulseSurveyRepository>.Instance);

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
