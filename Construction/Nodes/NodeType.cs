namespace Construction.Nodes
{
    public enum NodeType
    {
        Straight,       // Standard belt  
        LeftCorner,     // Standard belt that turns 90 degrees counterclockwise
        RightCorner,    // Standard belt that turns 90 degrees clockwise
        GenericBelt,    // Any of the above standard belts - used to request general belt construction 
        Intersection,   // A belt that allows to belt paths to cross
        Producer,       // A belt that generates new widgets
        Splitter,       // A belt that separates one belt path into two 
        Combiner        // A belt that combines two belt paths into one
    }
}