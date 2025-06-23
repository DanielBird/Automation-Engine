# Automation Engine 

This project is currently a work in progress. 

# To get started using the template

4 monobehaviour scripts are required in the scene: 

| Monobehavior            | Function  
|----------|----------|
| Map    | maintains a "base" grid, recording cell occupancy |
| Node Map    | mainatins a "Node" grid , recoridng cell occupancy of by Nodes | 
| Placement Manager    | coordinates game object placement on the grid, and creates Drag Sessions for click and drag operations |
| Placement Visuals    | manages visual aspects of game object placement | 

The Placement Manager class requires a scriptable object - PlacementSettings - that holds data about placement e.g. prefabs, input action references, the grid cell size.   


# Dependencies

- Unity Input System

- For zero garbage allocation async operations, this project uses UniTask from Cysharp.
- For zero garbage allocation LINQ operations, this project uses ZLinq from Cysharp. 
- See https://github.com/Cysharp

- Easing Functions by cjddmut.
- See https://gist.github.com/cjddmut
