using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tracker.Extensions;
using Tracker.Helpers;

namespace Tracker.DataWrappers
{
    public class EnumWrapper<TEnum> : BaseDataWrapper where TEnum : Enum
    {
        #region Fields

        private readonly TEnum _enumValue;

        #endregion

        #region Ctor

        public EnumWrapper(TEnum enumValue)
        {
            _enumValue = enumValue;
            Description = ResourceHelper.StringResourceHelper.GetString(enumValue.ToDescriptionString());
        }

        #endregion

        #region Public Properties

        public TEnum EnumValue => _enumValue;

        public string? Description { get; }

        #endregion
    }
}
