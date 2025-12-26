using System.Collections.ObjectModel;
using System.Windows.Input;
using Tracker.Command;
using Tracker.Common.Enums;
using Tracker.Database;
using Tracker.DataModels;
using Tracker.Eventing;
using Tracker.Eventing.Messages;
using Tracker.Logging;
using Tracker.Managers;

namespace Tracker.ViewModels
{
    /// <summary>
    /// ViewModel for the Quick Notes/Journal feature with master-detail layout.
    /// </summary>
    public class QuickNotesViewModel : BaseViewModel, IDisposable
    {
        #region Fields

        private readonly ILogger _logger = LoggingManager.GetComponentLogger("QuickNotesVM");
        
        private ObservableCollection<QuickNote> _notes = new();
        private ObservableCollection<QuickNote> _filteredNotes = new();
        private ObservableCollection<TeamMember> _teamMembers = new();
        private ObservableCollection<Project> _projects = new();
        private ObservableCollection<KeyPerformanceIndicator> _kpis = new();
        private ObservableCollection<ObjectiveKeyResult> _okrs = new();
        
        private QuickNote? _selectedNote;
        private bool _isEditing;
        private bool _isNewNote;
        
        // Filter fields
        private string _searchText = string.Empty;
        private NoteCategory? _selectedCategoryFilter;
        private NoteLinkedEntityType? _selectedEntityTypeFilter;
        private bool _showArchived;
        private bool _showPinnedOnly;
        
        // Edit fields
        private string _editTitle = string.Empty;
        private string _editContent = string.Empty;
        private NoteCategory _editCategory = NoteCategory.General;
        private string _editTags = string.Empty;
        private NoteLinkedEntityType _editLinkedEntityType = NoteLinkedEntityType.None;
        private object? _editLinkedEntity;

        #endregion

        #region Constructor

        public QuickNotesViewModel()
        {
            _ = LoadDataAsync(); // Fire and forget
            
            // Subscribe to data change messages
            DataMessenger.Register(this, OnDataChanged);
        }

        #endregion

        #region IDisposable

        public new void Dispose()
        {
            DataMessenger.Unregister(this);
        }

        #endregion

        #region Message Handlers

        private void OnDataChanged(DataChangeInfo info)
        {
            _logger.Debug("OnDataChanged received. RefreshAll={0}, Types={1}", 
                info.RefreshAll, string.Join(",", info.ChangedTypes));
            
            if (info.RefreshAll || info.Includes(DataChangeType.QuickNotes))
            {
                _logger.Info("Refreshing notes due to data change");
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(async () =>
                {
                    await LoadDataAsync();
                });
            }
        }

        #endregion

        #region Properties - Collections

        public ObservableCollection<QuickNote> Notes => _notes;
        
        public ObservableCollection<QuickNote> FilteredNotes
        {
            get => _filteredNotes;
            private set
            {
                _filteredNotes = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<TeamMember> TeamMembers => _teamMembers;
        public ObservableCollection<Project> Projects => _projects;
        public ObservableCollection<KeyPerformanceIndicator> Kpis => _kpis;
        public ObservableCollection<ObjectiveKeyResult> Okrs => _okrs;

        public Array Categories => Enum.GetValues(typeof(NoteCategory));
        public Array LinkedEntityTypes => Enum.GetValues(typeof(NoteLinkedEntityType));

        #endregion

        #region Properties - Selection & State

        public QuickNote? SelectedNote
        {
            get => _selectedNote;
            set
            {
                _selectedNote = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasSelectedNote));
                
                // Load selected note into edit fields
                if (_selectedNote != null && !IsNewNote)
                {
                    LoadNoteForEditing(_selectedNote);
                }
            }
        }

        public bool HasSelectedNote => _selectedNote != null;

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

        public bool IsNewNote
        {
            get => _isNewNote;
            set
            {
                _isNewNote = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Properties - Filters

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                RaisePropertyChanged();
                ApplyFilters();
            }
        }

        public NoteCategory? SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                _selectedCategoryFilter = value;
                RaisePropertyChanged();
                ApplyFilters();
            }
        }

        public NoteLinkedEntityType? SelectedEntityTypeFilter
        {
            get => _selectedEntityTypeFilter;
            set
            {
                _selectedEntityTypeFilter = value;
                RaisePropertyChanged();
                ApplyFilters();
            }
        }

        public bool ShowArchived
        {
            get => _showArchived;
            set
            {
                _showArchived = value;
                RaisePropertyChanged();
                LoadNotesAsync();
            }
        }

        public bool ShowPinnedOnly
        {
            get => _showPinnedOnly;
            set
            {
                _showPinnedOnly = value;
                RaisePropertyChanged();
                ApplyFilters();
            }
        }

        #endregion

        #region Properties - Edit Fields

        public string EditTitle
        {
            get => _editTitle;
            set
            {
                _editTitle = value;
                RaisePropertyChanged();
            }
        }

        public string EditContent
        {
            get => _editContent;
            set
            {
                _editContent = value;
                RaisePropertyChanged();
            }
        }

        public NoteCategory EditCategory
        {
            get => _editCategory;
            set
            {
                _editCategory = value;
                RaisePropertyChanged();
            }
        }

        public string EditTags
        {
            get => _editTags;
            set
            {
                _editTags = value;
                RaisePropertyChanged();
            }
        }

        public NoteLinkedEntityType EditLinkedEntityType
        {
            get => _editLinkedEntityType;
            set
            {
                _editLinkedEntityType = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ShowTeamMemberPicker));
                RaisePropertyChanged(nameof(ShowProjectPicker));
                RaisePropertyChanged(nameof(ShowKpiPicker));
                RaisePropertyChanged(nameof(ShowOkrPicker));
                
                // Clear the linked entity when type changes
                EditLinkedEntity = null;
            }
        }

        public object? EditLinkedEntity
        {
            get => _editLinkedEntity;
            set
            {
                _editLinkedEntity = value;
                RaisePropertyChanged();
            }
        }

        // Visibility helpers for linked entity pickers
        public bool ShowTeamMemberPicker => EditLinkedEntityType == NoteLinkedEntityType.TeamMember;
        public bool ShowProjectPicker => EditLinkedEntityType == NoteLinkedEntityType.Project;
        public bool ShowKpiPicker => EditLinkedEntityType == NoteLinkedEntityType.KPI;
        public bool ShowOkrPicker => EditLinkedEntityType == NoteLinkedEntityType.OKR;

        #endregion

        #region Properties - Statistics

        public int TotalNotes => _notes.Count;
        public int PinnedCount => _notes.Count(n => n.IsPinned);
        public int FilteredCount => _filteredNotes.Count;

        #endregion

        #region Commands

        private ICommand? _newNoteCommand;
        public ICommand NewNoteCommand => _newNoteCommand ??= new TrackerCommand(_ => NewNote());

        private ICommand? _editNoteCommand;
        public ICommand EditNoteCommand => _editNoteCommand ??= new TrackerCommand(_ => EditNote(), _ => HasSelectedNote);

        private ICommand? _saveNoteCommand;
        public ICommand SaveNoteCommand => _saveNoteCommand ??= new TrackerCommand(_ => SaveNoteAsync(), _ => CanSaveNote());

        private ICommand? _cancelEditCommand;
        public ICommand CancelEditCommand => _cancelEditCommand ??= new TrackerCommand(_ => CancelEdit());

        private ICommand? _deleteNoteCommand;
        public ICommand DeleteNoteCommand => _deleteNoteCommand ??= new TrackerCommand(_ => DeleteNoteAsync(), _ => HasSelectedNote);

        private ICommand? _togglePinCommand;
        public ICommand TogglePinCommand => _togglePinCommand ??= new TrackerCommand(_ => TogglePinAsync(), _ => HasSelectedNote);

        private ICommand? _archiveNoteCommand;
        public ICommand ArchiveNoteCommand => _archiveNoteCommand ??= new TrackerCommand(_ => ArchiveNoteAsync(), _ => HasSelectedNote);

        private ICommand? _clearFiltersCommand;
        public ICommand ClearFiltersCommand => _clearFiltersCommand ??= new TrackerCommand(_ => ClearFilters());

        private ICommand? _refreshCommand;
        public ICommand RefreshCommand => _refreshCommand ??= new TrackerCommand(_ => LoadNotesAsync());

        #endregion

        #region Command Implementations

        private void NewNote()
        {
            IsNewNote = true;
            IsEditing = true;
            
            // Clear edit fields
            EditTitle = string.Empty;
            EditContent = string.Empty;
            EditCategory = NoteCategory.General;
            EditTags = string.Empty;
            EditLinkedEntityType = NoteLinkedEntityType.None;
            EditLinkedEntity = null;
            
            // Create a placeholder note
            SelectedNote = new QuickNote
            {
                CreatedAt = DateTime.Now
            };
        }

        private void EditNote()
        {
            if (SelectedNote == null) return;
            IsEditing = true;
            IsNewNote = false;
        }

        private bool CanSaveNote()
        {
            return IsEditing && !string.IsNullOrWhiteSpace(EditContent);
        }

        private async void SaveNoteAsync()
        {
            if (!CanSaveNote()) return;

            try
            {
                var note = IsNewNote ? new QuickNote() : SelectedNote!;
                
                note.Title = EditTitle?.Trim() ?? string.Empty;
                note.Content = EditContent.Trim();
                note.Category = EditCategory;
                note.Tags = EditTags?.Trim() ?? string.Empty;
                
                // Set linked entity
                int? linkedEntityId = EditLinkedEntity switch
                {
                    TeamMember tm => tm.Id,
                    Project p => p.ID,
                    KeyPerformanceIndicator kpi => kpi.KpiId,
                    ObjectiveKeyResult okr => okr.ObjectiveId,
                    _ => null
                };
                note.SetLinkedEntity(EditLinkedEntityType, linkedEntityId);

                if (IsNewNote)
                {
                    var id = await TrackerDbManager.Instance.AddQuickNoteAsync(note);
                    if (id > 0)
                    {
                        note.Id = id;
                        _notes.Insert(0, note);
                        NotificationManager.Instance.ShowSuccess("Note Created", "Your note has been saved.");
                    }
                }
                else
                {
                    var success = await TrackerDbManager.Instance.UpdateQuickNoteAsync(note);
                    if (success)
                    {
                        NotificationManager.Instance.ShowSuccess("Note Updated", "Your changes have been saved.");
                    }
                }

                IsEditing = false;
                IsNewNote = false;
                SelectedNote = note;
                
                ApplyFilters();
                RefreshStatistics();
            }
            catch (Exception ex)
            {
                NotificationManager.Instance.ShowError("Save Failed", ex.Message);
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            
            if (IsNewNote)
            {
                IsNewNote = false;
                SelectedNote = _filteredNotes.FirstOrDefault();
            }
            else if (SelectedNote != null)
            {
                // Reload the original values
                LoadNoteForEditing(SelectedNote);
            }
        }

        private async void DeleteNoteAsync()
        {
            if (SelectedNote == null) return;

            var result = await TrackerDbManager.Instance.DeleteQuickNoteAsync(SelectedNote.Id);
            if (result)
            {
                var index = _notes.IndexOf(SelectedNote);
                _notes.Remove(SelectedNote);
                _filteredNotes.Remove(SelectedNote);
                
                // Select the next note or previous
                if (_filteredNotes.Count > 0)
                {
                    SelectedNote = _filteredNotes[Math.Min(index, _filteredNotes.Count - 1)];
                }
                else
                {
                    SelectedNote = null;
                }
                
                RefreshStatistics();
                NotificationManager.Instance.ShowInfo("Note Deleted", "The note has been deleted.");
            }
        }

        private async void TogglePinAsync()
        {
            if (SelectedNote == null) return;

            var result = await TrackerDbManager.Instance.ToggleNotePinnedAsync(SelectedNote.Id);
            if (result)
            {
                SelectedNote.IsPinned = !SelectedNote.IsPinned;
                ApplyFilters();
                RefreshStatistics();
            }
        }

        private async void ArchiveNoteAsync()
        {
            if (SelectedNote == null) return;

            var result = await TrackerDbManager.Instance.ArchiveNoteAsync(SelectedNote.Id);
            if (result)
            {
                SelectedNote.IsArchived = true;
                
                if (!ShowArchived)
                {
                    _notes.Remove(SelectedNote);
                    _filteredNotes.Remove(SelectedNote);
                    SelectedNote = _filteredNotes.FirstOrDefault();
                }
                
                RefreshStatistics();
                NotificationManager.Instance.ShowInfo("Note Archived", "The note has been archived.");
            }
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategoryFilter = null;
            SelectedEntityTypeFilter = null;
            ShowPinnedOnly = false;
            ApplyFilters();
        }

        #endregion

        #region Private Methods

        private async Task LoadDataAsync()
        {
            await LoadNotesAsync();
            await LoadLinkedEntityOptionsAsync();
        }

        private async Task LoadNotesAsync()
        {
            var notes = await TrackerDbManager.Instance.GetQuickNotesAsync(ShowArchived);
            _notes.Clear();
            foreach (var note in notes)
            {
                _notes.Add(note);
            }
            
            ApplyFilters();
            RefreshStatistics();
            
            // Select first note if nothing selected
            if (SelectedNote == null && _filteredNotes.Count > 0)
            {
                SelectedNote = _filteredNotes.First();
            }
        }

        private async Task LoadLinkedEntityOptionsAsync()
        {
            var teamMembers = await TrackerDbManager.Instance.GetTeamMembersAsync();
            _teamMembers.Clear();
            foreach (var tm in teamMembers) _teamMembers.Add(tm);

            var projects = await TrackerDbManager.Instance.GetProjectsAsync();
            _projects.Clear();
            foreach (var p in projects) _projects.Add(p);

            var kpis = await TrackerDbManager.Instance.GetKPIsAsync();
            _kpis.Clear();
            foreach (var k in kpis) _kpis.Add(k);

            var okrs = await TrackerDbManager.Instance.GetOKRsAsync();
            _okrs.Clear();
            foreach (var o in okrs) _okrs.Add(o);
        }

        private void ApplyFilters()
        {
            var filtered = _notes.AsEnumerable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLowerInvariant();
                filtered = filtered.Where(n =>
                    n.Title.Contains(search, StringComparison.InvariantCultureIgnoreCase) ||
                    n.Content.Contains(search, StringComparison.InvariantCultureIgnoreCase) ||
                    n.Tags.Contains(search, StringComparison.InvariantCultureIgnoreCase));
            }

            // Category filter
            if (SelectedCategoryFilter.HasValue)
            {
                filtered = filtered.Where(n => n.Category == SelectedCategoryFilter.Value);
            }

            // Entity type filter
            if (SelectedEntityTypeFilter.HasValue)
            {
                filtered = filtered.Where(n => n.LinkedEntityType == SelectedEntityTypeFilter.Value);
            }

            // Pinned only filter
            if (ShowPinnedOnly)
            {
                filtered = filtered.Where(n => n.IsPinned);
            }

            // Sort: pinned first, then by created date descending
            filtered = filtered
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.CreatedAt);

            FilteredNotes = new ObservableCollection<QuickNote>(filtered);
            RaisePropertyChanged(nameof(FilteredCount));
        }

        private void LoadNoteForEditing(QuickNote note)
        {
            EditTitle = note.Title;
            EditContent = note.Content;
            EditCategory = note.Category;
            EditTags = note.Tags;
            EditLinkedEntityType = note.LinkedEntityType;
            
            // Set the linked entity object
            EditLinkedEntity = note.LinkedEntityType switch
            {
                NoteLinkedEntityType.TeamMember => _teamMembers.FirstOrDefault(t => t.Id == note.LinkedEntityId),
                NoteLinkedEntityType.Project => _projects.FirstOrDefault(p => p.ID == note.LinkedEntityId),
                NoteLinkedEntityType.KPI => _kpis.FirstOrDefault(k => k.KpiId == note.LinkedEntityId),
                NoteLinkedEntityType.OKR => _okrs.FirstOrDefault(o => o.ObjectiveId == note.LinkedEntityId),
                _ => null
            };
        }

        private void RefreshStatistics()
        {
            RaisePropertyChanged(nameof(TotalNotes));
            RaisePropertyChanged(nameof(PinnedCount));
            RaisePropertyChanged(nameof(FilteredCount));
        }

        #endregion
    }
}
