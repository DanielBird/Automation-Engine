using System;
using UnityEngine;

namespace Utilities
{
    public static class RemapValues 
    {
        /// <summary>
        /// Maps a value from one numerical range to another using linear interpolation. 
        /// </summary>
        /// /// <remarks>
        /// This method calculates the relative position of a provided value within the source range and then applies that proportion to the target range.
        /// If the source range is zero, the method returns the target range's lower bounds to avoid division by zero.
        /// 
        /// Note that the method does not clamp the input value to the source range.
        /// Values outside the range will be mapped proportionally, possibly resulting in values outside the target range.
        /// </remarks>
        /// <param name="value"> The value to be remapped. </param>
        /// <param name="fromSource"> The lower bounds of the original range. </param>
        /// <param name="toSource"> The upper bounds of the original range. </param>
        /// <param name="fromTarget"> The lower bounds of the target range to remap to. </param>
        /// <param name="toTarget"> The upper bounds of the target range to remap to </param>
        /// <returns>
        /// The remapped value.
        /// If the source range is zero, the from target value is returned.  
        /// </returns>
        
        public static float Map(float value, float fromSource, float toSource, float fromTarget, float toTarget)
        {
            if (Math.Abs(toSource - fromSource) < Mathf.Epsilon) return fromTarget; 
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }
        
        /// <summary>
        /// Maps a value from one numerical range to another using linear interpolation.
        /// Ensures that if the value falls outside the source range, the value is adjusted to the nearest bounds before being remapped to the target range 
        /// </summary>
        
        public static float ClampedMap(float value, float fromSource, float toSource, float fromTarget, float toTarget)
        {
            if (Math.Abs(toSource - fromSource) < Mathf.Epsilon) return fromTarget;
            float clampedValue = Mathf.Clamp(value, fromSource, toSource); 
            return (clampedValue - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }
    }
}