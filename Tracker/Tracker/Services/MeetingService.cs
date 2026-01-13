using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tracker.DataModels;
using Tracker.Services.Data.Repositories;

namespace Tracker.Services
{
    /// <summary>
    /// Business logic service for Meeting operations.
    /// Wraps MeetingRepository and provides high-level meeting operations.
    /// 
    /// ViewModels call this service instead of calling repositories directly.
    /// This keeps ViewModels decoupled from data access infrastructure.
    /// </summary>
    public interface IMeetingService
    {
        /// <summary>
        /// Get meetings for a user (as organizer or participant).
        /// </summary>
        Task<IEnumerable<Meeting>> GetUserMeetingsAsync(Guid userId);

        /// <summary>
        /// Get upcoming meetings for a user.
        /// </summary>
        Task<IEnumerable<Meeting>> GetUpcomingMeetingsAsync(Guid userId, DateTime fromDate);

        /// <summary>
        /// Get past meetings for a user.
        /// </summary>
        Task<IEnumerable<Meeting>> GetPastMeetingsAsync(Guid userId, DateTime upToDate);

        /// <summary>
        /// Get the most recent meeting between two users (for 1:1 tracking).
        /// </summary>
        Task<Meeting?> GetPreviousMeetingAsync(Guid user1Id, Guid user2Id);

        /// <summary>
        /// Create a new meeting.
        /// </summary>
        Task<Meeting> CreateMeetingAsync(Meeting meeting);

        /// <summary>
        /// Update an existing meeting.
        /// </summary>
        Task UpdateMeetingAsync(Meeting meeting);

        /// <summary>
        /// Delete a meeting (soft delete).
        /// </summary>
        Task DeleteMeetingAsync(Guid meetingId);

        /// <summary>
        /// Get a single meeting by ID.
        /// </summary>
        Task<Meeting?> GetMeetingAsync(Guid meetingId);
    }

    public class MeetingService : IMeetingService
    {
        private readonly IMeetingRepository _repository;
        private readonly ILogger<MeetingService> _logger;

        public MeetingService(IMeetingRepository repository, ILogger<MeetingService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<Meeting>> GetUserMeetingsAsync(Guid userId)
        {
            try
            {
                return await _repository.GetByUserAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meetings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Meeting>> GetUpcomingMeetingsAsync(Guid userId, DateTime fromDate)
        {
            try
            {
                return await _repository.GetUpcomingByUserAsync(userId, fromDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming meetings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Meeting>> GetPastMeetingsAsync(Guid userId, DateTime upToDate)
        {
            try
            {
                return await _repository.GetPastByUserAsync(userId, upToDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting past meetings for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Meeting?> GetPreviousMeetingAsync(Guid user1Id, Guid user2Id)
        {
            try
            {
                // Get all meetings between user1 and user2, then return the most recent
                var userMeetings = await _repository.GetByUserAsync(user1Id);
                
                var previousMeeting = userMeetings
                    .Where(m => m.ParticipantId == user2Id || m.OrganizerId == user2Id)
                    .OrderByDescending(m => m.ScheduledAt)
                    .FirstOrDefault();

                return previousMeeting;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting previous meeting between {User1Id} and {User2Id}", user1Id, user2Id);
                throw;
            }
        }

        public async Task<Meeting> CreateMeetingAsync(Meeting meeting)
        {
            try
            {
                return await _repository.CreateAsync(meeting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating meeting");
                throw;
            }
        }

        public async Task UpdateMeetingAsync(Meeting meeting)
        {
            try
            {
                await _repository.UpdateAsync(meeting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating meeting {MeetingId}", meeting.Id);
                throw;
            }
        }

        public async Task DeleteMeetingAsync(Guid meetingId)
        {
            try
            {
                await _repository.DeleteAsync(meetingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting meeting {MeetingId}", meetingId);
                throw;
            }
        }

        public async Task<Meeting?> GetMeetingAsync(Guid meetingId)
        {
            try
            {
                return await _repository.GetByIdAsync(meetingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting meeting {MeetingId}", meetingId);
                throw;
            }
        }
    }
}
