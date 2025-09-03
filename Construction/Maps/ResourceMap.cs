using System.Collections.Generic;
using Engine.Construction.Events;
using Engine.Construction.Placement;
using Engine.Construction.Resources;
using Engine.Construction.Utilities;
using Engine.Utilities.Events;
using UnityEngine;
using Grid = Engine.Construction.Utilities.Grid;

namespace Engine.Construction.Maps
{
    public class ResourceMap: IResourceMap
    {
        private readonly Dictionary<Vector3Int, IResourceSource> _sources = new Dictionary<Vector3Int, IResourceSource>();

        public Dictionary<Vector3Int, IResourceSource> Sources
        {
            get => _sources;
            set => throw new System.NotImplementedException();
        }

        private GridParams gridParams; 
        private readonly EventBinding<RegisterResourceEvent> _onRegisterResourceRequest; 

        public ResourceMap(PlacementSettings settings)
        {
            gridParams = new GridParams(settings.mapOrigin, settings.mapWidth, settings.mapHeight, settings.cellSize);
            
            _onRegisterResourceRequest = new EventBinding<RegisterResourceEvent>(OnRegisterResourceEvent); 
            EventBus<RegisterResourceEvent>.Register(_onRegisterResourceRequest);
        }

        public void Disable()
        {
            EventBus<RegisterResourceEvent>.Deregister(_onRegisterResourceRequest);
        }

        private void OnRegisterResourceEvent(RegisterResourceEvent ev)
        {
            Vector3Int gridCoord = Grid.WorldToGridCoordinate(ev.Position, gridParams); 
            ev.Source.SetGridCoord(gridCoord);
            Register(ev.Source, gridCoord);
        }

        public void Register(IResourceSource src, Vector3Int gridCoord)
        {
            int width = src.GridWidth;
            int height = src.GridHeight;

            if (width % 2 == 0)
                width--; 
            
            if (height % 2 == 0)
                height--;
            
            int startX = gridCoord.x -(width / 2);
            int startZ = gridCoord.z - (height / 2);
            
            for (int i = startX; i < startX + width; i++)
            {
                for (int j = startZ; j < startZ + height; j++)
                {
                    _sources[new Vector3Int(i, 0, j)] = src;
                }
            }

            src.OnDepleted += HandleDepleted; 
        }

        public void Deregister(IResourceSource src)
        {
            if(_sources.TryGetValue(src.GridCoord, out IResourceSource source) && source == src)
                _sources.Remove(src.GridCoord);
            
            src.OnDepleted -= HandleDepleted;
        }

        private void HandleDepleted(IResourceSource src) => Deregister(src);

        public bool TryGetResourceSourceAt(Vector3Int gridCoord, out IResourceSource source) => _sources.TryGetValue(gridCoord, out source);
    }
}