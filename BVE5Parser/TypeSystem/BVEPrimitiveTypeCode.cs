using System;

namespace BVE5Language.TypeSystem
{
    /// <summary>
    /// BVE5's primitive data type codes.
    /// </summary>
    public enum BVEPrimitiveTypeCode : byte
    {
        None,
        /// <summary>
        /// Represents the integer literal.
        /// </summary>
        Integer,

        /// <summary>
        /// Represents the floating point literal.
        /// </summary>
        Float,

        /// <summary>
        /// Represents the name type 
        /// </summary>
        Name,

        /// <summary>
        /// Represents the path literal.
        /// </summary>
        FilePath,

        /// <summary>
        /// Represents the time literal.
        /// </summary>
        Time,
        
        /// <summary>
        /// Represents the enumeration used to specify the tilting option. It can take a value of 0~3.
        /// </summary>
        EnumTilt,
        
        /// <summary>
        /// Represents the enumeration used to specify the direction in which doors will open. It can be either -1 or 1.
        /// </summary>
        EnumDirection,

        /// <summary>
        /// Represents the enumeration used to specify the direction in which other trains will proceed. It can be either -1 or 1.
        /// </summary>
        EnumForwardDirection
    }
}
