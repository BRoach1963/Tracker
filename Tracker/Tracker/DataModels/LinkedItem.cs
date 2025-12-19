using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Tracker.Common.Enums;

namespace Tracker.DataModels
{
    /// <summary>
    /// Represents a link from an agenda item to another entity (Task, OKR, KPI, Project).
    /// </summary>
    public class LinkedItem : INotifyPropertyChanged
    {
        private LinkedItemType _type;
        private int _itemId;
        private string _title = string.Empty;

        /// <summary>
        /// Primary key for the linked item.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the parent AgendaItem.
        /// </summary>
        public int AgendaItemId { get; set; }

        /// <summary>
        /// Navigation property to the parent AgendaItem.
        /// </summary>
        [ForeignKey(nameof(AgendaItemId))]
        public virtual AgendaItem? AgendaItem { get; set; }

        /// <summary>
        /// Type of the linked entity.
        /// </summary>
        public LinkedItemType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); OnPropertyChanged(nameof(TypeDisplay)); }
        }

        /// <summary>
        /// ID of the linked entity.
        /// </summary>
        public int ItemId
        {
            get => _itemId;
            set { _itemId = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Display title of the linked entity.
        /// </summary>
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Short type display for badge (e.g., "Task", "OKR").
        /// </summary>
        [NotMapped]
        public string TypeDisplay => Type.ToString();

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}

