using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.Database.Repositories;
using Tracker.DataModels;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel for managing Performance Reviews, Templates, and Review Cycles.
    /// </summary>
    public class PerformanceReviewsViewModel : BaseViewModel
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("PerformanceReviewsVM");
        private readonly IReviewCycleRepository _reviewCycleRepository;
        private readonly IPerformanceReviewRepository _performanceReviewRepository;

        // Collections
        private ObservableCollection<ReviewTemplate> _templates = new();
        private ObservableCollection<PerformanceReviewCycle> _cycles = new();
        private ObservableCollection<PerformanceReview> _reviews = new();
        private ObservableCollection<TeamMember> _teamMembers = new();

        // Selection
        private ReviewTemplate? _selectedTemplate;
        private PerformanceReviewCycle? _selectedCycle;
        private PerformanceReview? _selectedReview;

        // State
        private bool _isEditing;
        private bool _isNewTemplate;
        private bool _isNewCycle;
        private bool _isLoading;
        private int _selectedTabIndex;

        // Template Edit Fields
        private string _editTemplateName = string.Empty;
        private string _editTemplateDescription = string.Empty;
        private ReviewType _editReviewType = ReviewType.Annual;
        private bool _editIsDefault;

        // Cycle Edit Fields
        private string _editCycleName = string.Empty;
        private string _editCycleDescription = string.Empty;
        private Guid? _editCycleTemplateId;
        private DateTime? _editSelfReviewStartDate;
        private DateTime? _editSelfReviewDueDate;
        private DateTime? _editManagerReviewStartDate;
        private DateTime? _editManagerReviewDueDate;

        #endregion

        #region Constructor

        public PerformanceReviewsViewModel(IReviewCycleRepository reviewCycleRepository, IPerformanceReviewRepository performanceReviewRepository)
        {
            _reviewCycleRepository = reviewCycleRepository ?? throw new ArgumentNullException(nameof(reviewCycleRepository));
            _performanceReviewRepository = performanceReviewRepository ?? throw new ArgumentNullException(nameof(performanceReviewRepository));
            _ = LoadDataAsync();
            DataMessenger.Register(this, OnDataChanged);
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DataMessenger.Unregister(this);
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Message Handlers

        private void OnDataChanged(DataChangeInfo info)
        {
            if (info.RefreshAll)
            {
                _logger.Info("Refreshing performance reviews due to data change");
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadDataAsync();
                });
            }
        }

        #endregion

        #region Properties - Collections

        public ObservableCollection<ReviewTemplate> Templates
        {
            get => _templates;
            private set
            {
                _templates = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<PerformanceReviewCycle> Cycles
        {
            get => _cycles;
            private set
            {
                _cycles = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<PerformanceReview> Reviews
        {
            get => _reviews;
            private set
            {
                _reviews = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;

        public Array ReviewTypes => Enum.GetValues(typeof(ReviewType));
        public Array CycleStatuses => Enum.GetValues(typeof(ReviewCycleStatus));
        public Array ReviewStatuses => Enum.GetValues(typeof(ReviewStatus));

        #endregion

        #region Properties - Selection

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                _selectedTabIndex = value;
                RaisePropertyChanged();
                CancelEditing();
            }
        }

        public ReviewTemplate? SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                _selectedTemplate = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedTemplate));
                RaisePropertyChanged(nameof(SelectedTemplateSections));

                if (_selectedTemplate != null && !IsNewTemplate)
                {
                    LoadTemplateForEditing(_selectedTemplate);
                }
            }
        }

        public PerformanceReviewCycle? SelectedCycle
        {
            get => _selectedCycle;
            set
            {
                _selectedCycle = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedCycle));
                RaisePropertyChanged(nameof(SelectedCycleReviews));
                RaisePropertyChanged(nameof(CanStartCycle));
                RaisePropertyChanged(nameof(CanCompleteCycle));

                if (_selectedCycle != null && !IsNewCycle)
                {
                    LoadCycleForEditing(_selectedCycle);
                    LoadCycleReviews();
                }
            }
        }

        public PerformanceReview? SelectedReview
        {
            get => _selectedReview;
            set
            {
                _selectedReview = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedReview));
            }
        }

        public ObservableCollection<ReviewTemplateSection>? SelectedTemplateSections =>
            _selectedTemplate?.Sections != null
                ? new ObservableCollection<ReviewTemplateSection>(_selectedTemplate.Sections.OrderBy(s => s.SortOrder))
                : null;

        public ObservableCollection<PerformanceReview>? SelectedCycleReviews =>
            _selectedCycle?.Reviews != null
                ? new ObservableCollection<PerformanceReview>(_selectedCycle.Reviews.OrderBy(r => r.TeamMember?.LastName))
                : null;

        public bool HasSelectedTemplate => _selectedTemplate != null;
        public bool HasSelectedCycle => _selectedCycle != null;
        public bool HasSelectedReview => _selectedReview != null;

        #endregion

        #region Properties - State

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsReadOnly));
            }
        }

        public bool IsReadOnly => !_isEditing;

        public bool IsNewTemplate
        {
            get => _isNewTemplate;
            set
            {
                _isNewTemplate = value;
                RaisePropertyChanged();
            }
        }

        public bool IsNewCycle
        {
            get => _isNewCycle;
            set
            {
                _isNewCycle = value;
                RaisePropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        public bool CanStartCycle => _selectedCycle?.Status == ReviewCycleStatus.Draft;
        public bool CanCompleteCycle => _selectedCycle?.Status == ReviewCycleStatus.ManagerReviewInProgress ||
                                        _selectedCycle?.Status == ReviewCycleStatus.Calibration;

        #endregion

        #region Properties - Template Edit Fields

        public string EditTemplateName
        {
            get => _editTemplateName;
            set
            {
                _editTemplateName = value;
                RaisePropertyChanged();
            }
        }

        public string EditTemplateDescription
        {
            get => _editTemplateDescription;
            set
            {
                _editTemplateDescription = value;
                RaisePropertyChanged();
            }
        }

        public ReviewType EditReviewType
        {
            get => _editReviewType;
            set
            {
                _editReviewType = value;
                RaisePropertyChanged();
            }
        }

        public bool EditIsDefault
        {
            get => _editIsDefault;
            set
            {
                _editIsDefault = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Properties - Cycle Edit Fields

        public string EditCycleName
        {
            get => _editCycleName;
            set
            {
                _editCycleName = value;
                RaisePropertyChanged();
            }
        }

        public string EditCycleDescription
        {
            get => _editCycleDescription;
            set
            {
                _editCycleDescription = value;
                RaisePropertyChanged();
            }
        }

        public Guid? EditCycleTemplateId
        {
            get => _editCycleTemplateId;
            set
            {
                _editCycleTemplateId = value;
                RaisePropertyChanged();
            }
        }

        public DateTime? EditSelfReviewStartDate
        {
            get => _editSelfReviewStartDate;
            set
            {
                _editSelfReviewStartDate = value;
                RaisePropertyChanged();
            }
        }

        public DateTime? EditSelfReviewDueDate
        {
            get => _editSelfReviewDueDate;
            set
            {
                _editSelfReviewDueDate = value;
                RaisePropertyChanged();
            }
        }

        public DateTime? EditManagerReviewStartDate
        {
            get => _editManagerReviewStartDate;
            set
            {
                _editManagerReviewStartDate = value;
                RaisePropertyChanged();
            }
        }

        public DateTime? EditManagerReviewDueDate
        {
            get => _editManagerReviewDueDate;
            set
            {
                _editManagerReviewDueDate = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Commands - Templates

        private ICommand? _newTemplateCommand;
        public ICommand NewTemplateCommand => _newTemplateCommand ??= new TrackerCommand(
            _ => CreateNewTemplate(),
            _ => !IsEditing && SelectedTabIndex == 0);

        private ICommand? _editTemplateCommand;
        public ICommand EditTemplateCommand => _editTemplateCommand ??= new TrackerCommand(
            _ => StartEditingTemplate(),
            _ => HasSelectedTemplate && !IsEditing && SelectedTabIndex == 0);

        private ICommand? _saveTemplateCommand;
        public ICommand SaveTemplateCommand => _saveTemplateCommand ??= new TrackerCommand(
            async _ => await SaveTemplateAsync(),
            _ => IsEditing && !string.IsNullOrWhiteSpace(EditTemplateName) && SelectedTabIndex == 0);

        private ICommand? _deleteTemplateCommand;
        public ICommand DeleteTemplateCommand => _deleteTemplateCommand ??= new TrackerCommand(
            async _ => await DeleteTemplateAsync(),
            _ => HasSelectedTemplate && !IsEditing && SelectedTabIndex == 0);

        #endregion

        #region Commands - Cycles

        private ICommand? _newCycleCommand;
        public ICommand NewCycleCommand => _newCycleCommand ??= new TrackerCommand(
            _ => CreateNewCycle(),
            _ => !IsEditing && SelectedTabIndex == 1 && Templates.Any());

        private ICommand? _editCycleCommand;
        public ICommand EditCycleCommand => _editCycleCommand ??= new TrackerCommand(
            _ => StartEditingCycle(),
            _ => HasSelectedCycle && !IsEditing && SelectedTabIndex == 1 && _selectedCycle?.Status == ReviewCycleStatus.Draft);

        private ICommand? _saveCycleCommand;
        public ICommand SaveCycleCommand => _saveCycleCommand ??= new TrackerCommand(
            async _ => await SaveCycleAsync(),
            _ => IsEditing && !string.IsNullOrWhiteSpace(EditCycleName) && EditCycleTemplateId.HasValue && SelectedTabIndex == 1);

        private ICommand? _deleteCycleCommand;
        public ICommand DeleteCycleCommand => _deleteCycleCommand ??= new TrackerCommand(
            async _ => await DeleteCycleAsync(),
            _ => HasSelectedCycle && !IsEditing && SelectedTabIndex == 1);

        private ICommand? _startCycleCommand;
        public ICommand StartCycleCommand => _startCycleCommand ??= new TrackerCommand(
            async _ => await StartCycleAsync(),
            _ => CanStartCycle && !IsEditing);

        private ICommand? _completeCycleCommand;
        public ICommand CompleteCycleCommand => _completeCycleCommand ??= new TrackerCommand(
            async _ => await CompleteCycleAsync(),
            _ => CanCompleteCycle && !IsEditing);

        #endregion

        #region Commands - Reviews

        private ICommand? _openReviewCommand;
        public ICommand OpenReviewCommand => _openReviewCommand ??= new TrackerCommand(
            _ => OpenReview(),
            _ => HasSelectedReview);

        private ICommand? _shareReviewCommand;
        public ICommand ShareReviewCommand => _shareReviewCommand ??= new TrackerCommand(
            async _ => await ShareReviewAsync(),
            _ => HasSelectedReview && _selectedReview?.Status == ReviewStatus.ManagerReviewComplete);

        #endregion

        #region Commands - Common

        private ICommand? _cancelCommand;
        public ICommand CancelCommand => _cancelCommand ??= new TrackerCommand(
            _ => CancelEditing(),
            _ => IsEditing);

        private ICommand? _refreshCommand;
        public ICommand RefreshCommand => _refreshCommand ??= new TrackerCommand(
            async _ => await LoadDataAsync(),
            _ => !IsLoading);

        #endregion

        #region Private Methods - Data Loading

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                // Use TrackerDataManager as single source of truth for all data
                var templates = await TrackerDataManager.Instance.GetReviewTemplates();
                _templates = new ObservableCollection<ReviewTemplate>(templates);
                RaisePropertyChanged(nameof(Templates));

                var cycles = await TrackerDataManager.Instance.GetReviewCycles();
                _cycles = new ObservableCollection<PerformanceReviewCycle>(cycles);
                RaisePropertyChanged(nameof(Cycles));

                var members = await TrackerDataManager.Instance.GetTeamData();
                _teamMembers = new ObservableCollection<TeamMember>(members);
                RaisePropertyChanged(nameof(TeamMembers));

                _logger.Info("Loaded {0} templates, {1} cycles", templates.Count, cycles.Count);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error loading performance review data");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadTemplateForEditing(ReviewTemplate template)
        {
            EditTemplateName = template.Name;
            EditTemplateDescription = template.Description;
            EditReviewType = template.ReviewType;
            EditIsDefault = template.IsDefault;
        }

        private void LoadCycleForEditing(PerformanceReviewCycle cycle)
        {
            EditCycleName = cycle.Name;
            EditCycleDescription = cycle.Description;
            EditCycleTemplateId = cycle.ReviewTemplateId;
            EditSelfReviewStartDate = cycle.SelfReviewStartDate;
            EditSelfReviewDueDate = cycle.SelfReviewDueDate;
            EditManagerReviewStartDate = cycle.ManagerReviewStartDate;
            EditManagerReviewDueDate = cycle.ManagerReviewDueDate;
        }

        private void LoadCycleReviews()
        {
            if (_selectedCycle != null)
            {
                _reviews = new ObservableCollection<PerformanceReview>(_selectedCycle.Reviews ?? new List<PerformanceReview>());
                RaisePropertyChanged(nameof(Reviews));
                RaisePropertyChanged(nameof(SelectedCycleReviews));
            }
        }

        #endregion

        #region Private Methods - Template Operations

        private void CreateNewTemplate()
        {
            _selectedTemplate = new ReviewTemplate
            {
                Name = "New Template",
                ReviewType = ReviewType.Annual,
                IsActive = true
            };

            LoadTemplateForEditing(_selectedTemplate);
            IsNewTemplate = true;
            IsEditing = true;

            RaisePropertyChanged(nameof(SelectedTemplate));
            RaisePropertyChanged(nameof(HasSelectedTemplate));
        }

        private void StartEditingTemplate()
        {
            if (_selectedTemplate != null)
            {
                LoadTemplateForEditing(_selectedTemplate);
                IsEditing = true;
            }
        }

        private async Task SaveTemplateAsync()
        {
            if (_selectedTemplate == null) return;

            try
            {
                _selectedTemplate.Name = EditTemplateName;
                _selectedTemplate.Description = EditTemplateDescription;
                _selectedTemplate.ReviewType = EditReviewType;
                _selectedTemplate.IsDefault = EditIsDefault;

                if (IsNewTemplate)
                {
                    // Add default section
                    _selectedTemplate.Sections.Add(new ReviewTemplateSection
                    {
                        Title = "Performance Summary",
                        SortOrder = 1,
                        Questions = new List<ReviewTemplateQuestion>
                        {
                            new() { Text = "What were your key accomplishments this period?", QuestionType = ReviewQuestionType.LongText, SortOrder = 1 },
                            new() { Text = "What challenges did you face?", QuestionType = ReviewQuestionType.LongText, SortOrder = 2 },
                            new() { Text = "Overall performance rating", QuestionType = ReviewQuestionType.Rating, SortOrder = 3 }
                        }
                    });

                    var id = await TrackerDataManager.Instance.AddReviewTemplate(_selectedTemplate);
                    if (id != Guid.Empty)
                    {
                        _selectedTemplate.Id = id;
                        _logger.Info("Created new template: {0}", _selectedTemplate.Name);
                        await LoadDataAsync();
                    }
                }
                else
                {
                    var success = await TrackerDataManager.Instance.UpdateReviewTemplate(_selectedTemplate);
                    if (success)
                    {
                        _logger.Info("Updated template: {0}", _selectedTemplate.Name);
                        await LoadDataAsync();
                    }
                }

                IsEditing = false;
                IsNewTemplate = false;
                RaisePropertyChanged(nameof(Templates));
                RaisePropertyChanged(nameof(SelectedTemplateSections));
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error saving template");
            }
        }

        private async Task DeleteTemplateAsync()
        {
            if (_selectedTemplate == null) return;

            try
            {
                var success = await TrackerDataManager.Instance.DeleteReviewTemplate(_selectedTemplate.Id);
                if (success)
                {
                    _selectedTemplate = null;
                    RaisePropertyChanged(nameof(SelectedTemplate));
                    RaisePropertyChanged(nameof(HasSelectedTemplate));
                    _logger.Info("Deleted template");
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting template");
            }
        }

        #endregion

        #region Private Methods - Cycle Operations

        private void CreateNewCycle()
        {
            var defaultTemplate = _templates.FirstOrDefault(t => t.IsDefault) ?? _templates.FirstOrDefault();
            
            _selectedCycle = new PerformanceReviewCycle
            {
                Name = $"{DateTime.Now.Year} Performance Review",
                ReviewTemplateId = defaultTemplate?.Id ?? Guid.Empty,
                Status = ReviewCycleStatus.Draft,
                SelfReviewStartDate = DateTime.Today,
                SelfReviewDueDate = DateTime.Today.AddDays(14),
                ManagerReviewStartDate = DateTime.Today.AddDays(14),
                ManagerReviewDueDate = DateTime.Today.AddDays(28)
            };

            LoadCycleForEditing(_selectedCycle);
            IsNewCycle = true;
            IsEditing = true;

            RaisePropertyChanged(nameof(SelectedCycle));
            RaisePropertyChanged(nameof(HasSelectedCycle));
        }

        private void StartEditingCycle()
        {
            if (_selectedCycle != null)
            {
                LoadCycleForEditing(_selectedCycle);
                IsEditing = true;
            }
        }

        private async Task SaveCycleAsync()
        {
            if (_selectedCycle == null || !EditCycleTemplateId.HasValue) return;

            try
            {
                _selectedCycle.Name = EditCycleName;
                _selectedCycle.Description = EditCycleDescription;
                _selectedCycle.ReviewTemplateId = EditCycleTemplateId.Value;
                _selectedCycle.SelfReviewStartDate = EditSelfReviewStartDate;
                _selectedCycle.SelfReviewDueDate = EditSelfReviewDueDate;
                _selectedCycle.ManagerReviewStartDate = EditManagerReviewStartDate;
                _selectedCycle.ManagerReviewDueDate = EditManagerReviewDueDate;

                if (IsNewCycle)
                {
                    var id = await TrackerDataManager.Instance.AddReviewCycle(_selectedCycle);
                    if (id != Guid.Empty)
                    {
                        _selectedCycle.Id = id;
                        _logger.Info("Created new cycle: {0}", _selectedCycle.Name);
                        await LoadDataAsync();
                    }
                }
                else
                {
                    var success = await TrackerDataManager.Instance.UpdateReviewCycle(_selectedCycle);
                    if (success)
                    {
                        _logger.Info("Updated cycle: {0}", _selectedCycle.Name);
                        await LoadDataAsync();
                    }
                }

                IsEditing = false;
                IsNewCycle = false;
                RaisePropertyChanged(nameof(Cycles));
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error saving cycle");
            }
        }

        private async Task DeleteCycleAsync()
        {
            if (_selectedCycle == null) return;

            try
            {
                var success = await TrackerDataManager.Instance.DeleteReviewCycle(_selectedCycle.Id);
                if (success)
                {
                    _selectedCycle = null;
                    RaisePropertyChanged(nameof(SelectedCycle));
                    RaisePropertyChanged(nameof(HasSelectedCycle));
                    _logger.Info("Deleted cycle");
                    await LoadDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error deleting cycle");
            }
        }

        private async Task StartCycleAsync()
        {
            if (_selectedCycle == null) return;

            try
            {
                // TODO: Migrate CreateReviewsForCycleAsync to TrackerDataManager or repository pattern
                // Create reviews for all team members (special operation, uses DB directly)
                var count = await TrackerDataManager.Instance.CreateReviewsForCycleAsync(_selectedCycle.Id);
                
                _selectedCycle.Status = ReviewCycleStatus.SelfReviewInProgress;
                await TrackerDataManager.Instance.UpdateReviewCycle(_selectedCycle);

                // Reload to get the created reviews
                _selectedCycle = await _reviewCycleRepository.GetReviewCycleByIdAsync(_selectedCycle.Id);
                
                RaisePropertyChanged(nameof(SelectedCycle));
                RaisePropertyChanged(nameof(SelectedCycleReviews));
                RaisePropertyChanged(nameof(CanStartCycle));
                RaisePropertyChanged(nameof(CanCompleteCycle));

                _logger.Info("Started cycle with {0} reviews", count);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error starting cycle");
            }
        }

        private async Task CompleteCycleAsync()
        {
            if (_selectedCycle == null) return;

            try
            {
                _selectedCycle.Status = ReviewCycleStatus.Completed;
                await TrackerDataManager.Instance.UpdateReviewCycle(_selectedCycle);

                RaisePropertyChanged(nameof(SelectedCycle));
                RaisePropertyChanged(nameof(CanStartCycle));
                RaisePropertyChanged(nameof(CanCompleteCycle));

                _logger.Info("Completed cycle: {0}", _selectedCycle.Name);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error completing cycle");
            }
        }

        #endregion

        #region Private Methods - Review Operations

        private void OpenReview()
        {
            if (_selectedReview == null) return;

            // This would open a detailed review dialog/view
            _logger.Info("Opening review for: {0}", _selectedReview.TeamMember?.FullName);
            // TODO: Implement detailed review view
        }

        private async Task ShareReviewAsync()
        {
            if (_selectedReview == null) return;

            try
            {
                // TODO: Migrate ShareReviewAsync to TrackerDataManager or repository pattern
                var success = await TrackerDataManager.Instance.ShareReviewAsync(_selectedReview.Id);
                if (success)
                {
                    _selectedReview.Status = ReviewStatus.Shared;
                    _selectedReview.SharedAt = DateTime.UtcNow;
                    RaisePropertyChanged(nameof(SelectedReview));
                    RaisePropertyChanged(nameof(SelectedCycleReviews));
                    _logger.Info("Shared review for: {0}", _selectedReview.TeamMember?.FullName);
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error sharing review");
            }
        }

        private void CancelEditing()
        {
            if (IsNewTemplate)
            {
                _selectedTemplate = null;
                RaisePropertyChanged(nameof(SelectedTemplate));
                RaisePropertyChanged(nameof(HasSelectedTemplate));
            }
            else if (IsNewCycle)
            {
                _selectedCycle = null;
                RaisePropertyChanged(nameof(SelectedCycle));
                RaisePropertyChanged(nameof(HasSelectedCycle));
            }
            else if (_selectedTemplate != null && SelectedTabIndex == 0)
            {
                LoadTemplateForEditing(_selectedTemplate);
            }
            else if (_selectedCycle != null && SelectedTabIndex == 1)
            {
                LoadCycleForEditing(_selectedCycle);
            }

            IsEditing = false;
            IsNewTemplate = false;
            IsNewCycle = false;
        }

        #endregion
    }
}
