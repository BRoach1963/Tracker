namespace Tracker.Common.Enums
{
    /// <summary>
    /// How to aggregate values from multiple data sources.
    /// </summary>
    public enum AggregationTypeEnum
    {
        /// <summary>Use the most recent value.</summary>
        Latest,

        /// <summary>Sum all values together.</summary>
        Sum,

        /// <summary>Calculate the average of all values.</summary>
        Average,

        /// <summary>Use the minimum value.</summary>
        Min,

        /// <summary>Use the maximum value.</summary>
        Max,

        /// <summary>Calculate weighted average using source weights.</summary>
        WeightedAverage
    }
}


