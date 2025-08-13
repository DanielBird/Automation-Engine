using System;
using Construction.Widgets;

namespace Construction.Belts
{
    public struct Move : IEquatable<Move>
    {
        public Belt Source;
        public Belt Target;
        public Widget Widget;

        public bool Equals(Move other)
        {
            return Equals(Source, other.Source) && Equals(Target, other.Target) && Equals(Widget, other.Widget);
        }

        public override bool Equals(object obj)
        {
            return obj is Move other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Source, Target, Widget);
        }
    }
}