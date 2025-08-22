using System;
using Engine.Construction.Resources;

namespace Engine.Construction.Belts
{
    public struct Move : IEquatable<Move>
    {
        public Belt Source;
        public Belt Target;
        public Resource Resource;

        public bool Equals(Move other)
        {
            return Equals(Source, other.Source) && Equals(Target, other.Target) && Equals(Resource, other.Resource);
        }

        public override bool Equals(object obj)
        {
            return obj is Move other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Source, Target, Resource);
        }
    }
}