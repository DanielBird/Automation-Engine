namespace Engine.Construction.Placement
{
    public enum Direction
    {
        North = 0,
        East = 1, 
        South = 2,
        West = 3,
        
        // Bit mapped directions
        // North = 1<<0 = 1
        // East = 1<<1 = 2
        // South = 1<<2 = 4
        // West = 1<<3 = 8
    }
}