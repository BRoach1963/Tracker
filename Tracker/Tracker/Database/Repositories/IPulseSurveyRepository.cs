using Tracker.DataModels;
using Tracker.Common.Enums;

namespace Tracker.Database.Repositories
{
    /// <summary>
    /// Repository interface for PulseSurvey data access operations.
    /// Handles employee pulse/engagement surveys.
    /// </summary>
    public interface IPulseSurveyRepository
    {
        /// <summary>
        /// Gets all pulse surveys for the current user.
        /// </summary>
        Task<List<PulseSurvey>> GetPulseSurveysAsync();

        /// <summary>
        /// Gets a pulse survey by ID with all related data.
        /// </summary>
        Task<PulseSurvey?> GetPulseSurveyByIdAsync(int id);

        /// <summary>
        /// Adds a new pulse survey.
        /// </summary>
        Task<int> AddPulseSurveyAsync(PulseSurvey survey);

        /// <summary>
        /// Updates an existing pulse survey.
        /// </summary>
        Task<bool> UpdatePulseSurveyAsync(PulseSurvey survey);

        /// <summary>
        /// Deletes a pulse survey.
        /// </summary>
        Task<bool> DeletePulseSurveyAsync(int id);

        /// <summary>
        /// Gets active pulse surveys (sent but not closed).
        /// </summary>
        Task<List<PulseSurvey>> GetActivePulseSurveysAsync();

        /// <summary>
        /// Adds a survey response from a team member.
        /// </summary>
        Task<int> AddSurveyResponseAsync(PulseSurveyResponse response);
    }
}
