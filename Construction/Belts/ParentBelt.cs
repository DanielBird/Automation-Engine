using Engine.Construction.Maps;
using Engine.Construction.Nodes;
using Engine.Construction.Placement;
using Engine.Construction.Utilities;
using UnityEngine;

namespace Engine.Construction.Belts
{
    /// <summary>
    /// A type of belt that manages a child belt
    /// The child belt should live on a game object that is a child of this game object
    /// </summary>
    public class ParentBelt : Belt
    {
        [Header("Parent Belt")]
        public Belt childBelt;
        protected IMap Map; 
        private Vector2Int _leftGridPosition; 
        [Tooltip("How many steps along the grid should the splitter look for its target Nodes?")]
        public int stepSize = 1;

        public override void Initialise(NodeConfiguration config)
        {
            base.Initialise(config);
            SetupWidgetMovement();
            
            Map = config.Map;
            if (!RegisterChildBelt(config)) return;
            FinaliseChildBelt(config);
        }
        
        private bool RegisterChildBelt(NodeConfiguration config)
        {
            if (childBelt == null)
            {
                Debug.LogWarning($"Every {name} requires a child belt. The child belt is currently missing");
                
                childBelt = GetComponentInChildren<Belt>();
                if(childBelt == null) return false;
            }
            
            _leftGridPosition = PositionByDirection.GetLeftPosition(GridCoord, Direction, stepSize);
            Vector3Int childGridCoord = new (_leftGridPosition.x, 0, _leftGridPosition.y);

            Vector2Int size = childBelt.GetSize();
            if (!config.Map.RegisterOccupant(_leftGridPosition.x, _leftGridPosition.y, size.x, size.y))
            {
                childBelt.FailedPlacement(childGridCoord);
                return false;
            }
            
            childBelt.Place(childGridCoord, config.NodeMap);
            return true;
        }

        private void FinaliseChildBelt(NodeConfiguration config)
        {
            NodeConfiguration nodeConfig = GetChildNodeConfiguration(config);
            
            childBelt.SetParent(this);
            childBelt.Initialise(nodeConfig);
            childBelt.UpdateTargetNode();
        }

        protected virtual NodeConfiguration GetChildNodeConfiguration(NodeConfiguration config)
        {
            return NodeConfiguration.Create(Map, config.NodeMap, NodeType.Straight, Direction, true); 
        }
        
        public override void OnRemoval()
        {
            base.OnRemoval();
            
            // Clean up child belt
            childBelt.OnRemoval();
            Vector3Int gridCoord = childBelt.GridCoord; 
            Map.DeregisterOccupant(gridCoord.x, gridCoord.z, childBelt.GridWidth, childBelt.GridHeight);
            NodeMap.DeregisterNode(childBelt);
        }
    }
}