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

Belt path splitters and delivery combiners.

![Demo](Media/SplitAndCombine.gif)

Path elevation handling - coming

Click and drag to add more belts to an existing belt path in the scene. 

Belt path loop detection and automatic avoidance. 

Player removal of placed game objects. 

# To get started using the template

Monobehaviour scripts required in the scene: 

| Monobehavior         | Function  
|----------            |----------|
| Map                  | maintains a "base" grid, recording which cells are occupied |
| Node Map             | mainatins a "Node" grid, recordidng where Nodes have been placed on the grid | 
| Placement Manager    | coordinates game object placement on the grid, and creates Drag Sessions for click and drag operations |
| Placement Visuals    | manages visual aspects of game object placement | 
| Belt Manager         | coordinates a belt graph, triggering widget movement | 
| Removal Manager      | coordinates game object removal via right mouse click and hold |
| Player Click Manager | casts a raycast on left mouse down and any class implementing IClickable will be notified if hit |


# Key Components 

Nodes 

├── Belt

│   ├── Producer

│   ├── Consumer

│   ├── Intersection

│   ├── Parent Belt

│   ├── ├── Combiner

│   ├── ├── Splitter


classDiagram
    class Belt
    class Producer
    class Consumer
    class Intersection
    class ParentBelt
    class Combiner
    class Splitter
    
    Belt <|-- Producer
    Belt <|-- Consumer
    Belt <|-- Intersection
    Belt <|-- ParentBelt
    ParentBelt <|-- Combiner
    ParentBelt <|-- Splitter



Widgets


| Class          | Function                                                                                                                                    | Parents and Interfaces
|----------      |----------                                                                                                                                   |----------|
| Node           | Any object you would want to place on the map that have a direction and a target                                                            | Monobehaviour, IPlaceable, IRotatable, IClickable |
| Belt           | A type of Node that can ship widgets to one another - managed by Belt Manager                                                               | Node |
| Widget         | Deliveries handled by belts. They manage their own movement logic and implement a strategy pattern for handling different movement styles   | Monobehaviour |
| Producer       | A type of Belt that spawns new widgets                                                                                                      | Belt |
| Consumer       | A type of Belt that despawns existing widgets upon arrival                                                                                  | Belt |
| Intersection   | A type of Belt that ships Widgets to different target Nodes based on which Node they recieved the Widget from                               | Belt |
| ParentBelt     | A belt that manages a child belt - intended to be attached to the same game object                                                          | ParentBelt |
| Splitter       | A type of Belt that splits widgets onto two different children Belt paths                                                                   | ParentBelt |
| Combiner       | A type of Belt that combines widgets onto one child belt path from two parent Belt paths                                                    | ParentBelt |

# Scriptable objects

| Scriptable Objects   | Function  
|----------            |----------|
| PlacementSettings    | Data about placement like the map size and cell size |
| InputSettings        | Input action references and camera movement parameters |
| NodePrefabBindings   | A list of node/prefab bindings (enums and prefabs) |
| WidgetPrefabs        | A list of game objects that should be delivered via nodes / belts |
| NodeTypes            | Each type of node (e.g. belt) should have a corresponding node type that records its width, height, and whether it is draggable |

# Dependencies

Unity Input System.


For zero garbage allocation async operations, this project uses UniTask from Cysharp.


For zero garbage allocation LINQ operations, this project uses ZLinq from Cysharp. 


See https://github.com/Cysharp


Easing Functions by cjddmut.


See https://gist.github.com/cjddmut
