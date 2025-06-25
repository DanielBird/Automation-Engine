# Automation Engine 

This project is currently a work in progress. 

![Demo](Media/BeltTest.gif)



# To get started using the template

5 monobehaviour scripts are required in the scene: 

| Monobehavior            | Function  
|----------|----------|
| Map    | maintains a "base" grid, recording which cells are occupied |
| Node Map    | mainatins a "Node" grid, recordidng where Nodes have been placed on the grid | 
| Placement Manager    | coordinates game object placement on the grid, and creates Drag Sessions for click and drag operations |
| Placement Visuals    | manages visual aspects of game object placement | 
| Belt Manager    | coordinates a belt graph, triggering widget movement | 

The Placement Manager class requires a scriptable object - PlacementSettings - that holds data about placement e.g. prefabs, input action references, the grid cell size.   

# Key Components 

Nodes 


├── Belts


│   ├── Producers

Widgets

- Nodes are any object you would want to place on the map that have a direction and a target
- Belts inherit from Nodes and ship widgets to one another
- Producers inherit from Belts and spawn new widgets


# Dependencies

Unity Input System.


For zero garbage allocation async operations, this project uses UniTask from Cysharp.


For zero garbage allocation LINQ operations, this project uses ZLinq from Cysharp. 


See https://github.com/Cysharp


Easing Functions by cjddmut.


See https://gist.github.com/cjddmut
