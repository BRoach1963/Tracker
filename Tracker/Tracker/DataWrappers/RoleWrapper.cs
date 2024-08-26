using Tracker.Common.Enums;
using Tracker.Extensions;
using Tracker.Helpers;

namespace Tracker.DataWrappers
{
    public class RoleWrapper : BaseDataWrapper
    {
        #region Fields

        private RoleEnum _role;

        #endregion

        #region Ctor

        public RoleWrapper(RoleEnum role)
        {
            _role = role;
            Description = ResourceHelper.StringResourceHelper.GetString(role.ToDescriptionString());
        }

        #endregion

        #region Public Properties

        public RoleEnum RoleEnum => _role;

        public string? Description { get; }

        #endregion
    }
}
