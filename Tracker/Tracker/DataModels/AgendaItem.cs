using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// An agenda item for 1:1 meetings - topics, concerns, questions to discuss.
    /// Can be linked to Tasks, OKRs, KPIs, or Projects for context.
    /// </summary>
    public class AgendaItem : AuditableEntity, INotifyPropertyChanged
    {
        private string _description = string.Empty;
        private AgendaItemCategory _category = AgendaItemCategory.Topic;
        private Severity _priority = Severity.Medium;
        private bool _isCompleted;
        private ObservableCollection<LinkedItem> _linkedItems = new();

        public int Id { get; set; }

        /// <summary>
        /// Brief description of the agenda item.
        /// </summary>
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Category of this agenda item.
        /// </summary>
        public AgendaItemCategory Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); OnPropertyChanged(nameof(CategoryDisplay)); }
        }

        /// <summary>
        /// Priority level (Low, Medium, High).
        /// </summary>
        public Severity Priority
        {
            get => _priority;
            set { _priority = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Resolution notes - how this item was addressed.
        /// </summary>
        public string Resolution { get; set; } = string.Empty;

        /// <summary>
        /// Whether this item has been completed/addressed.
        /// </summary>
        public bool IsCompleted
        {
            get => _isCompleted;
            set { _isCompleted = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Optional link to a MeetingTask if a task was created from this item.
        /// </summary>
        public int? LinkedTaskId { get; set; }

        /// <summary>
        /// FK to the 1:1 meeting this item belongs to.
        /// </summary>
        public int OneOnOneId { get; set; }

        #region Entity Linking

        /// <summary>
        /// Collection of linked items (Tasks, OKRs, KPIs, Projects).
        /// </summary>
        public ObservableCollection<LinkedItem> LinkedItems
        {
            get => _linkedItems;
            set 
            { 
                _linkedItems = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HasLinkedItems)); 
            }
        }

        /// <summary>
        /// Whether this agenda item has any linked entities.
        /// </summary>
        public bool HasLinkedItems => LinkedItems?.Count > 0;

        /// <summary>
        /// Adds a linked item to this agenda item.
        /// </summary>
        public void AddLinkedItem(LinkedItemType type, int itemId, string title)
        {
            LinkedItems.Add(new LinkedItem { Type = type, ItemId = itemId, Title = title });
            OnPropertyChanged(nameof(HasLinkedItems));
            OnPropertyChanged(nameof(LinkedItems));
        }

        /// <summary>
        /// Removes a linked item from this agenda item.
        /// </summary>
        public void RemoveLinkedItem(LinkedItem item)
        {
            LinkedItems.Remove(item);
            OnPropertyChanged(nameof(HasLinkedItems));
            OnPropertyChanged(nameof(LinkedItems));
        }

        /// <summary>
        /// Display text for the category badge.
        /// </summary>
        public string CategoryDisplay => Category.ToString();

        #endregion

        /// <summary>
        /// Computed property - resolved if completed, has resolution text, or linked task.
        /// </summary>
        public bool IsResolved => IsCompleted || !string.IsNullOrEmpty(Resolution) || LinkedTaskId.HasValue;

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
