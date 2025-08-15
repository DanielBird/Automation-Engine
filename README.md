# Automation Engine 

Automation engine is an open source template for 3d, automation games.

This project is regularly being updated. 

It is being built in Unity 6.  

# Features

A grid based construction system. 
Player placement uses breadth first search to avoid barriers to constuction.  

A belt graph that handles deliveries.

![Demo](Media/BeltTest.gif)

Intersections - allowing two distinct paths to cross. 

![Demo](Media/IntersectionGif.gif)

Belt path splitters - coming

Delivery combiners - comning

Path elevation handling - coming

Belt path loop detection and automatic avoidance. 

Player removal of placed game objects. 

# To get started using the template

Monobehaviour scripts required in the scene: 

| Monobehavior            | Function  
|----------|----------|
| Map    | maintains a "base" grid, recording which cells are occupied |
| Node Map    | mainatins a "Node" grid, recordidng where Nodes have been placed on the grid | 
| Placement Manager    | coordinates game object placement on the grid, and creates Drag Sessions for click and drag operations |
| Placement Visuals    | manages visual aspects of game object placement | 
| Belt Manager    | coordinates a belt graph, triggering widget movement | 
| Removal Manager    | coordinates game object removal via right mouse click and hold |
| Player Click Manager   | casts a raycast on left mouse down and any class implementing IClickable will be notified if hit |


# Key Components 

Nodes 

├── Belt

│   ├── Producer

│   ├── Intersection

│   ├── Splitter

│   ├── Combiner

Widgets

- Nodes are any object you would want to place on the map that have a direction and a target
- Belts inherit from Nodes and ship widgets to one another
- Producers inherit from Belts and spawn new widgets
- Intersections inherit from Belts. They ship Widgets to different target Nodes based on which Node they recieved the Widget. 
- Splitters - to come
- Combiners - to come

# Scriptable objects

| Scriptable Objects | Function  
|----------|----------|
| PlacementSettings    | Data about placement like the map size and cell size |
| InputSettings    | Input action references and camera movement parameters |
| NodePrefabBindings    | A list of node/prefab bindings (enums and prefabs) |
| WidgetPrefabs    | A list of game objects that should be delivered via nodes / belts |
| NodeTypes    | Each type of node (e.g. belt) should have a corresponding node type that records its width, height, and whether it is draggable |

# Dependencies

Unity Input System.


For zero garbage allocation async operations, this project uses UniTask from Cysharp.


For zero garbage allocation LINQ operations, this project uses ZLinq from Cysharp. 


See https://github.com/Cysharp


Easing Functions by cjddmut.


See https://gist.github.com/cjddmut
